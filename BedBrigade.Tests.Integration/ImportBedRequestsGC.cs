using BedBrigade.Common.Logic;
using BedBrigade.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using KellermanSoftware.AddressParser;
using KellermanSoftware.NameParser;

namespace BedBrigade.Tests.Integration;

[TestFixture]
public class ImportBedRequestsGC
{
    private const string ImportFilePath = @"C:\Users\gfinz\Downloads\Bed Requests - Bed Requests.csv";
    private const string ConnectionString =
        "server=localhost\\sqlexpress;database=bedbrigade;trusted_connection=SSPI;Encrypt=False";
    private readonly NameParserLogic _nameParserLogic = LibraryFactory.CreateNameParser();
    private readonly AddressParser _addressParser = LibraryFactory.CreateAddressParser();
    private readonly Regex _phoneRegex = new Regex(Validation.PhoneRegexPattern, RegexOptions.Compiled);
    private readonly Regex _teamRegex = new Regex(@"(team\s)(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _zipRegex = new Regex(@"^\d{5}", RegexOptions.Compiled);
    private DateTime _defaultCreateDate = new DateTime(2018, 11, 11);
    private DateTime _lastDeliveryDate = DateTime.Now;

    [Test]
    //[Test, Ignore("Only run locally manually")]
    public async Task ImportBedRequestsFromGC()
    {
        if (!TestHelper.IsWindows() || !TestHelper.ThisComputerHasExcelInstalled())
        {
            Assert.Ignore("This test should only run locally.");
        }


        var context = CreateDbContext(ConnectionString);
        await DeleteExistingBedRequestsForLocation(context, Defaults.GroveCityLocationId);
       
        CsvReader csvReader = new CsvReader();

        var items = csvReader.CsvFileToDictionary(ImportFilePath);
        List<BedRequest> destItems = new List<BedRequest>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            try
            {
                BedRequest bedRequest = new BedRequest();
                FillBedRequest(bedRequest, item);
                AddOrUpdateList(bedRequest, destItems);
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Error processing item {i + 1}: {ex.Message}");
                sb.AppendLine($"Item data: {string.Join(", ", item.Select(kv => $"{kv.Key}: {kv.Value}"))}");
                throw new FormatException(sb.ToString(), ex);
            }
        }

        //Add the items to the database
        foreach (BedRequest bedRequest in destItems)
        {
            context.BedRequests.Add(bedRequest);
        }

        await context.SaveChangesAsync();
    }

    private void AddOrUpdateList(BedRequest bedRequest, List<BedRequest> destItems)
    {
        string[] keywordsToIgnore = new[] { "social", "worker", "manager", "health" };
        string[] propertiesToIgnore = new[] { "CreateDate" };

        BedRequest? existing = destItems.FirstOrDefault(o => o.Phone == bedRequest.Phone
                                                             && o.FirstName == bedRequest.FirstName
                                                             && o.LastName == bedRequest.LastName
                                                             && o.Status == BedRequestStatus.Waiting
                                                             && !keywordsToIgnore.Any(k => (o.Reference ?? string.Empty).ToLower().Contains(k))
                                                             && !keywordsToIgnore.Any(k => (o.Notes ?? string.Empty).ToLower().Contains(k))
                                                             );
        if (existing != null)
        {
            string existingString = ObjectUtil.ObjectToString(existing);
            string bedRequestString = ObjectUtil.ObjectToString(bedRequest);
            ObjectUtil.CopyProperties(bedRequest, existing, propertiesToIgnore);
        }
        else
        {
            destItems.Add(bedRequest);
        }
    }

    private void FillBedRequest(BedRequest bedRequest, Dictionary<string, string> item)
    {

        bedRequest.Notes = string.Empty;
        SetFirstNameLastName(item, bedRequest);
        SetPhone(item, bedRequest);
        SetAddress(item, bedRequest);
        SetPostalCode(item, bedRequest);
        bedRequest.CreateDate = ParseDate(item);
        bedRequest.CreateUser = "Import";
        bedRequest.UpdateUser = "Import";
        bedRequest.MachineName = Environment.MachineName;
        _defaultCreateDate = bedRequest.CreateDate.Value;
        bedRequest.Group = item["OrgDeliver"];
        bedRequest.BedType = Defaults.DefaultBedType;
        if (string.IsNullOrWhiteSpace(bedRequest.Group))
        {
            bedRequest.Group = "GC";
        }

        if (bedRequest.Group.ToLower() == "polaris")
        {
            bedRequest.LocationId = Defaults.PolarisLocationId;
        }
        else
        {
            bedRequest.LocationId = Defaults.GroveCityLocationId;
        }

        bedRequest.NumberOfBeds = int.TryParse(item["#BedsRequested"], out int beds) ? beds : 1;
        bedRequest.Status = Enum.Parse<BedRequestStatus>(item["Status"], true);
        bedRequest.GenderAge = item["Gender/Age"];
        bedRequest.DeliveryDate = ParseDeliveryDate(item["DeliveryDate"]);
        bedRequest.Reference = item["Reference"];

        SetPrimaryLanguage(item, bedRequest);
        SetLatLong(bedRequest);
        SetTeam(item, bedRequest);

        if (bedRequest.Notes.Length > 0)
        {
            bedRequest.Notes += " ";
        }

        bedRequest.Notes += item["Notes"];

        if (bedRequest.Notes.ToLower().Contains("pick") && String.IsNullOrEmpty(bedRequest.Reference))
        {
            bedRequest.Reference = "Pick Up";
        }
        else if (string.IsNullOrWhiteSpace(bedRequest.Reference) || bedRequest.Reference == "Website")
        {
            bedRequest.Reference = "GC Website";
        }

        if (bedRequest.Status == BedRequestStatus.Given && !bedRequest.DeliveryDate.HasValue)
        {
            bedRequest.DeliveryDate = bedRequest.CreateDate;
        }
        else if (bedRequest.Status == BedRequestStatus.Delivered && !bedRequest.DeliveryDate.HasValue)
        {
            bedRequest.DeliveryDate = GetDateFromNote(item["Notes"]);

            if (!bedRequest.DeliveryDate.HasValue)
            {
                bedRequest.DeliveryDate = _lastDeliveryDate;
            }
        }

        if (bedRequest.DeliveryDate.HasValue)
        {
            bedRequest.UpdateDate = bedRequest.DeliveryDate.Value;
            _lastDeliveryDate = bedRequest.DeliveryDate.Value;
        }
        else
        {
            bedRequest.UpdateDate = bedRequest.CreateDate.Value;
        }

        string[] calledWords = new[]
        {
            "lm", "left m", "called", "vm", "voicemail", "talked",
            "contacted", "spoke", "scheduled",
            "delivered", "delivery",
            "/", "will call", "call after", "call back",
            "voice mail"
        };

        if (bedRequest.Status == BedRequestStatus.Delivered
            || bedRequest.Status == BedRequestStatus.Scheduled
            || bedRequest.Status == BedRequestStatus.Given
            || calledWords.Any(o => bedRequest.Notes.ToLower().Contains(o)))
        {
            bedRequest.Contacted = true;
        }

        List<string> errors = Validation.ValidateWithDataAnnotations(bedRequest);

        if (errors.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Validation errors:");
            foreach (string error in errors)
            {
                sb.AppendLine(error);
            }
            throw new FormatException(sb.ToString());
        }
    }

    private void SetPostalCode(Dictionary<string, string> item, BedRequest bedRequest)
    {
        if (string.IsNullOrWhiteSpace(item["Zip"]))
        {
            return;
        }
        Match zipMatch = _zipRegex.Match(item["Zip"]);
        if (zipMatch.Success)
        {
            bedRequest.PostalCode = zipMatch.Value;
        }
        else
        {
            throw new FormatException($"Invalid postal code format: {item["Zip"]}. Expected format: 12345.");
        }
    }

    private void SetAddress(Dictionary<string, string> item, BedRequest bedRequest)
    {
        bedRequest.Street = item["Address"];

        if (bedRequest.Street.Contains("13769 us hwy 62 lot D"))
        {
            bedRequest.Street = "13769 US Hwy 62 Lot D";
        }
        else if (bedRequest.Street.Contains("165 S Prinston Ave"))
        {
            bedRequest.Street = "165 S Princeton Ave";
        }
        else if (bedRequest.Street.Contains("1077 Irongate Ln.  Apt. A"))
        {
            bedRequest.Street = "1077 Irongate Ln. Apt. A";
        }
    }

    private void SetPrimaryLanguage(Dictionary<string, string> item, BedRequest bedRequest)
    {
        string notes = item["Notes"].ToLower();

        if (notes.Contains("spanish"))
        {
            bedRequest.PrimaryLanguage = "Spanish";
        }
        else if (notes.Contains("creole"))
        {
            bedRequest.PrimaryLanguage = "Haitian Creole";
        }
        else if (notes.Contains("portuguese"))
        {
            bedRequest.PrimaryLanguage = "Portuguese";
        }
        else
        {
            bedRequest.PrimaryLanguage = "English";
        }
    }

    private void SetTeam(Dictionary<string, string> item, BedRequest bedRequest)
    {
        string notes = item["Notes"];
        if (string.IsNullOrWhiteSpace(notes))
        {
            return;
        }

        Match teamMatch = _teamRegex.Match(notes);

        if (!teamMatch.Success)
        {
            return;
        }

        bedRequest.Team = teamMatch.Groups[2].Value;
    }

    private void SetLatLong(BedRequest bedRequest)
    {
        if (string.IsNullOrWhiteSpace(bedRequest.PostalCode))
        {
            return;
        }

        ZipCodeInfo? zipInfo = _addressParser.GetInfoForZipCode(bedRequest.PostalCode);

        if (zipInfo == null)
        {
            return;
        }

        bedRequest.Latitude = zipInfo.Latitude;
        bedRequest.Longitude = zipInfo.Longitude;
        bedRequest.City = zipInfo.PrimaryCity;
        bedRequest.State = zipInfo.State;
    }

    private DateTime? ParseDeliveryDate(string dateInput)
    {
        if (string.IsNullOrWhiteSpace(dateInput))
        {
            return null;
        }

        if (dateInput.Contains("2/8/2023"))
        {
            return new DateTime(2023, 2, 8);
        }
        if (dateInput.Contains("125/25"))
        {
            return new DateTime(2025, 1, 25);
        }
        if (dateInput.Contains("7/0/25"))
        {
            return DateUtil.GetLastSaturdayOfMonthAndYear(7, 2025);
        }

            // Try full date first
            string[] fullFormats = {
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "M/dd/yyyy",
            "MM/d/yyyy",
            "MM/dd/yy",
            "M/d/yy",
            "M/dd/yy",
            "MM/d/yy"
        };
        if (DateTime.TryParseExact(dateInput, fullFormats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
        {
            return parsedDate;
        }

        // Try M/d (no year), use year from _defaultCreateDate
        string[] monthDayFormats = { "M/d", "MM/dd", "M/dd", "MM/d" };
        if (DateTime.TryParseExact(dateInput, monthDayFormats, null, System.Globalization.DateTimeStyles.None, out DateTime monthDay))
        {
            // Use year from _defaultCreateDate
            DateTime parsedDeliveryDate = new DateTime(_defaultCreateDate.Year, monthDay.Month, monthDay.Day);

            if (parsedDeliveryDate < _defaultCreateDate)
            {
                // If the parsed date is before the default create date, assume it's for next year
                parsedDeliveryDate = parsedDeliveryDate.AddYears(1);
            }

            return parsedDeliveryDate;
        }

        // Try M/d (no year), use year from _defaultCreateDate
        string[] monthYearFormats = { "M/yyyy", "MM/yyyy", "M/yy", "MM/yy" };
        if (DateTime.TryParseExact(dateInput, monthYearFormats, null, System.Globalization.DateTimeStyles.None, out DateTime monthYear))
        {
            return DateUtil.GetLastSaturdayOfMonthAndYear(monthYear.Month, monthYear.Year);
        }

        throw new FormatException($"Invalid date format: {dateInput}. Expected formats: MM/dd/yyyy, M/d/yyyy, MM/dd, M/d, etc.");
    }

    private DateTime ParseDate(Dictionary<string, string> item)
    {
        string dateInput = item["AddedtoList"];
        string address = item["Address"];
        if (address.Contains("899 Geers Ave Apt D"))
        {
            dateInput = "2/8/2023"; // Special case for this address
        }
        else if (address.Contains("757 S. Warren Ave"))
        {
            dateInput = "7/9/2025"; // Special case for this address
        }
        else if (address.Contains("1420 Franklin Ave Apt B"))
        {
            dateInput = "11/3/2024"; // Special case for this address
        }

        if (string.IsNullOrWhiteSpace(dateInput))
        {
            return _defaultCreateDate;
        }

        string[] fullFormats = { "MM/dd/yyyy", "M/d/yyyy", "M/dd/yyyy", "MM/d/yyyy", "MM/dd/yy", "M/d/yy", "M/dd/yy", "MM/d/yy" };
        if (DateTime.TryParseExact(dateInput, fullFormats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
        {
            return parsedDate;
        }

        return _defaultCreateDate;
    }

    public static DateTime? GetDateFromNote(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;

        // Pattern matches dates like 9/20/2025, 09/20/25, etc.
        var pattern = @"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{2,4})\b";
        var match = Regex.Match(note, pattern);

        if (!match.Success)
            return null;

        string dateStr = match.Value;

        // All supported formats
        string[] formats = new[]
        {
            "MM/dd/yyyy", "M/d/yyyy", "M/dd/yyyy", "MM/d/yyyy",
            "MM/dd/yy", "M/d/yy", "M/dd/yy", "MM/d/yy"
        };

        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }

        return null;
    }

    private void SetPhone(Dictionary<string, string> item, BedRequest bedRequest)
    {
        string phoneInput = item["Phone"];
        if (string.IsNullOrWhiteSpace(phoneInput))
        {
            bedRequest.Phone = string.Empty;
            return;
        }

        //Get phone matches
        MatchCollection phoneMatches = _phoneRegex.Matches(phoneInput);

        if (phoneMatches.Count == 0)
        {
            bedRequest.Phone = string.Empty;
            return;
        }

        //If there are multiple matches, use the first one
        bedRequest.Phone = phoneMatches[0].Value.FormatPhoneNumber();

        if (phoneMatches.Count > 1)
        {
            if (bedRequest.Notes.Length > 0)
            {
                bedRequest.Notes += " ";
            }

            bedRequest.Notes += "Other Phone Numbers: ";
            bedRequest.Notes += string.Join(", ", phoneMatches.Cast<Match>().Skip(1).Select(m => m.Value.FormatPhoneNumber()));
        }
    }

    private void SetFirstNameLastName(Dictionary<string, string> item, BedRequest bedRequest)
    {
        if (item["Name"] == "Bebe - The Columbus Dream Center")
        {
            bedRequest.FirstName = "Bebe";
            bedRequest.LastName = "The Columbus Dream Center";
            return;
        }

        if (item["Name"] == "Lanija Sales")
        {
            bedRequest.FirstName = "Lanija";
            bedRequest.LastName = "Sales";
            return;
        }

        if (item["Name"] == "Steven/Libby Cooper")
        {
            bedRequest.FirstName = "Steven";
            bedRequest.LastName = "Cooper";
            return;
        }

        if (item["Name"] == "Daniel/Leona McKenzie")
        {
            bedRequest.FirstName = "Daniel";
            bedRequest.LastName = "McKenzie";
            return;
        }

        if (item["Name"] == "Jessica/Christopher Smith")
        {
            bedRequest.FirstName = "Christopher";
            bedRequest.LastName = "Smith";
            return;
        }

        if (item["Name"] == "Jessica/Lamar")
        {
            bedRequest.FirstName = "Lamar";
            bedRequest.LastName = string.Empty;
            return;
        }

        List<NameParts> namePartsList = ParseNames(item["Name"]);
        if (namePartsList.Count == 0)
        {
            bedRequest.FirstName = string.Empty;
            bedRequest.LastName = string.Empty;
        }
        else if (namePartsList.Count == 1)
        {
            if (string.IsNullOrEmpty(namePartsList[0].FirstName))
            {
                bedRequest.FirstName = namePartsList[0].LastName;
            }
            else
            {
                bedRequest.FirstName = namePartsList[0].FirstName;
                bedRequest.LastName = namePartsList[0].LastName;
            }
        }
        else
        {
            bedRequest.FirstName = namePartsList[0].FirstName;
            bedRequest.LastName = namePartsList[0].LastName;
            bedRequest.Notes = "Other Names: " + string.Join(", ", namePartsList.Skip(1).Select(n => $"{n.FirstName} {n.LastName}"));
        }

        if (string.IsNullOrWhiteSpace(bedRequest.FirstName) && string.IsNullOrWhiteSpace(bedRequest.LastName))
        {
            bedRequest.FirstName = string.Empty;
            bedRequest.LastName = item["Name"];
        }

        if (string.IsNullOrWhiteSpace(bedRequest.FirstName))
        {
            bedRequest.FirstName = "Unknown";
        }

        if (string.IsNullOrWhiteSpace(bedRequest.LastName))
        {
            bedRequest.LastName = string.Empty;
        }
    }

    public List<NameParts> ParseNames(string nameInput)
    {
        string[] names = nameInput.Split(new []{'/','('}, StringSplitOptions.RemoveEmptyEntries);

        List<NameParts> namePartsList = new List<NameParts>();

        foreach (string name in names.ToList())
        {
            string trimmedName = name.Replace(")", string.Empty).Trim();
            NameParts nameParts = _nameParserLogic.ParseName(trimmedName, NameOrder.FirstLast);
            if (nameParts != null)
            {
                namePartsList.Add(nameParts);
            }
        }

        return namePartsList;
    }

    public DataContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new DataContext(optionsBuilder.Options);
    }

    public async Task DeleteExistingBedRequestsForLocation(DataContext context, int locationId)
    {
        var bedRequests = await context.BedRequests.Where(o => o.LocationId == locationId).ToListAsync();
        context.BedRequests.RemoveRange(bedRequests);
        await context.SaveChangesAsync();
    }
}

