using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data;
using KellermanSoftware.NameParser;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Licensing;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BedBrigade.Common.Enums;
using KellermanSoftware.AddressParser;

namespace BedBrigade.Tests.Integration
{
    [TestFixture]
    public class ImportBedRequestsPolaris
    {
        private const string FilePath = @"C:\Users\gfinz\Downloads\Bed_Requests_1754656392.xlsx";
        private const string ConnectionString =
            "server=localhost\\sqlexpress;database=bedbrigade;trusted_connection=SSPI;Encrypt=False";
        private readonly NameParserLogic _nameParserLogic = LibraryFactory.CreateNameParser();
        private readonly AddressParser _addressParser = LibraryFactory.CreateAddressParser();

        private Regex aptRegex =
            new Regex(@"(APT|apt)\s[#0-9A-Za-z]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _phoneRegex = new Regex(Validation.PhoneRegexPattern, RegexOptions.Compiled);
        private readonly Regex _zipRegex = new Regex(@"\d{5}", RegexOptions.Compiled);

        private string[] groups = new[]
        {
            "New Requests",
            "Contacted (Waiting)",
            "August 9th delivery",
            "Grove City requests",
            "Additional needs",
            "Unable to contact after 3 months",
            "Out of town",
            "Wrong phone number listed",
            "Delivered"
        };

        [Test]
        public async Task Import()
        {
            if (!TestHelper.IsWindows() || !TestHelper.ThisComputerHasExcelInstalled())
            {
                Assert.Ignore("This test should only run locally.");
            }

            List<PolarisBedRequest> polarisBedRequests = ExcelToPolarisBedRequests();


            DataContext context = CreateDbContext(ConnectionString);

            List<BedRequest> existing = await context.BedRequests.ToListAsync();
            List<BedRequest> newPolaris = CombinePolaris(polarisBedRequests);
            await Reconcile(context, existing, newPolaris);
        }

        private async Task Reconcile(DataContext context, List<BedRequest> existing, List<BedRequest> polaris)
        {
            List<BedRequest> toAdd = new List<BedRequest>();
            List<BedRequest> toUpdate = new List<BedRequest>();
            foreach (BedRequest bedRequest in polaris)
            {
                BedRequest? existingBedRequest = existing.FirstOrDefault(x => x.Phone == bedRequest.Phone
                                                                              && x.FirstName == bedRequest.FirstName
                                                                              && x.LastName == bedRequest.LastName
                                                                              && x.Status == BedRequestStatus.Waiting 
                                                                                      && (bedRequest.Status == BedRequestStatus.Scheduled 
                                                                                  || bedRequest.Status == BedRequestStatus.Delivered)
                                                                              );
                if (existingBedRequest == null)
                {
                    toAdd.Add(bedRequest);
                }
                else
                {
                    string[] propertiesToIgnore = new[]
                    {
                        nameof(BedRequest.BedRequestId), 
                        nameof(BedRequest.CreateDate), 
                        nameof(BedRequest.UpdateDate),
                        nameof(BedRequest.CreateUser), 
                        nameof(BedRequest.UpdateUser), 
                        nameof(BedRequest.MachineName)
                    };

                    ObjectUtil.CopyProperties(bedRequest, existingBedRequest, propertiesToIgnore);
                    toUpdate.Add(existingBedRequest);
                }
            }
            if (toAdd.Count > 0)
            {
                context.BedRequests.AddRange(toAdd);
            }
            if (toUpdate.Count > 0)
            {
                context.BedRequests.UpdateRange(toUpdate);
            }
            await context.SaveChangesAsync();
        }

        private List<BedRequest> CombinePolaris(List<PolarisBedRequest> polarisBedRequests)
        {
            List<BedRequest> results = new List<BedRequest>();
            BedRequest current = new BedRequest();
            current.NumberOfBeds = 0;
            DateTime createDate = new DateTime(2025, 7, 5);
            DateTime deliveryDate = new DateTime(2025, 7, 12);
            foreach (PolarisBedRequest request in polarisBedRequests)
            {
                if (request.Group == "Out of town")
                    continue;

                string phone = GetFirstPhone(request.PhoneNumber);

                if (!String.IsNullOrEmpty(current.Phone) && current.Phone != phone)
                {
                    results.Add(current);
                    current = new BedRequest();
                }

                SetPhone(current, request.PhoneNumber);
                current.LocationId = Defaults.PolarisLocationId;
                current.Group = "Polaris";

                SetNotes(request, current);
                SetStatus(request, current);
                SetReference(request, current);
                SetFirstNameLastName(request, current);
                SetCreateDate(request, current, createDate);
                createDate = current.CreateDate.Value;
                SetDeliveryDate(request, current, deliveryDate);

                if (current.Status == BedBrigade.Common.Enums.BedRequestStatus.Delivered
                    && current.DeliveryDate.HasValue)
                {
                    deliveryDate = current.DeliveryDate.Value;
                }

                SetGenderAge(request, current);
                SetNames(request, current);
                SetAddress(request, current);
                SetLatLong(current);
                current.Email = request.Email;
                current.PrimaryLanguage = request.PrimaryLanguage;
                current.SpeakEnglish = request.SpeaksEnglish;
                current.Team = request.TeamLead1;
                AddBedType(current, request.BedType);
                current.NumberOfBeds++;

                current.CreateUser = "Import";
                current.UpdateUser = "Import";
                current.MachineName = Environment.MachineName;

                if (current.DeliveryDate.HasValue)
                {
                    current.UpdateDate = current.DeliveryDate.Value;
                }
                else
                {
                    current.UpdateDate = current.CreateDate.Value;
                }
            }

            results.Add(current);

            return results;
        }

        private void SetPhone(BedRequest bedRequest, string phoneInput)
        {
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
                //We already added the other phone numbers to the notes
                if (bedRequest.Notes.Contains(phoneMatches[1].Value.FormatPhoneNumber()))
                {
                    return;
                }

                if (bedRequest.Notes.Length > 0)
                {
                    bedRequest.Notes += " ";
                }

                bedRequest.Notes += "Other Phone Numbers: ";
                bedRequest.Notes += string.Join(", ", phoneMatches.Cast<Match>().Skip(1).Select(m => m.Value.FormatPhoneNumber()));
            }
        }

        private string GetFirstPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }
            Match match = _phoneRegex.Match(phone);
            if (match.Success)
            {
                return match.Value.FormatPhoneNumber();
            }
            return string.Empty;
        }

        private void SetNames(PolarisBedRequest request, BedRequest current)
        {
            if (request.BedForAdultOrChild == "Adult")
            {
                AddName(current, request.AdultName);
            }
            else
            {
                AddName(current, request.ChildName);
            }
        }


        private void SetLatLong(BedRequest bedRequest)
        {
            if (string.IsNullOrWhiteSpace(bedRequest.PostalCode))
            {
                return;
            }

            try
            {
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
            catch (Exception)
            {
                // Ignore invalid zip codes
            }
        }

        private void SetAddress(PolarisBedRequest request, BedRequest current)
        {
            //Get apartment number if it exists
            string deliveryAddress = request.DeliveryAddress;
            deliveryAddress = deliveryAddress.Replace(", EE. UU.", string.Empty);
            deliveryAddress = StringUtil.TakeOffEnd(deliveryAddress, ", USA A");
            deliveryAddress = deliveryAddress.Replace(", USA", string.Empty);
            deliveryAddress = deliveryAddress.Replace(", MS, USA", string.Empty);
            deliveryAddress = deliveryAddress.Replace(", États-Unis", string.Empty);
            deliveryAddress = deliveryAddress.Replace("4681 Ohio River Circle South. Columbus Ohio 43228. Apt 106", "4681 Ohio River Circle South Apt 106, Columbus, Ohio 43228");
            string aptNumber = aptRegex.Match(deliveryAddress).Value.Trim();

            if (!string.IsNullOrWhiteSpace(aptNumber))
            {
                deliveryAddress = deliveryAddress.Replace(aptNumber, string.Empty).Trim();
            }

            string street = deliveryAddress.Split(',')[0];
            string cityStateZip = deliveryAddress.Replace(street + ",", string.Empty).Trim();
            deliveryAddress = current.FirstName + " " + current.LastName + "\r\n" 
                              + street + "\r\n"
                              + cityStateZip;

            AddressResult? addressResult = _addressParser.ParseAddress(deliveryAddress);

            if (addressResult == null)
            {
                current.Street = deliveryAddress;
                current.City = string.Empty;
                current.State = string.Empty;
                current.PostalCode = string.Empty;
            }
            else
            {
                current.Street = addressResult.AddressLine1;

                if (!String.IsNullOrWhiteSpace(addressResult.AddressLine2))
                {
                    current.Street += " " + addressResult.AddressLine2;
                }
                current.City = addressResult.City;
                current.State = addressResult.Region;
                current.PostalCode = _zipRegex.Match(addressResult.PostalCode ?? string.Empty).Value ?? string.Empty;

                if (!String.IsNullOrEmpty(current.PostalCode) && current.PostalCode.Length > 5)
                {
                    throw new FormatException("Postal code too long: " + current.PostalCode);
                }

                if (!string.IsNullOrWhiteSpace(aptNumber))
                {
                    current.Street += " " + aptNumber;
                }
            }

        }

        private void SetGenderAge(PolarisBedRequest request, BedRequest current)
        {
            string genderLetter = string.Empty;
            string ageLetter = string.Empty;

            if (request.BedForAdultOrChild == "Adult")
            {
                ageLetter = "A";
                genderLetter = request.AdultGender == "Male" ? "M" : "F";
            }
            else
            {
                ageLetter = request.ChildAge;

                genderLetter = request.ChildGender == "Boy" ? "B" : "G";
            }

            if (!string.IsNullOrWhiteSpace(current.GenderAge))
            {
                current.GenderAge += " ";
            }

            current.GenderAge += $"{genderLetter}/{ageLetter}";
        }

        private void SetDeliveryDate(PolarisBedRequest request, BedRequest current, DateTime deliveryDate)
        {
            if (current.Status == BedBrigade.Common.Enums.BedRequestStatus.Delivered)
            {
                if (current.DeliveryDate == null || current.DeliveryDate == DateTime.MinValue)
                {
                    if (request.DeliveryDate != DateTime.MinValue)
                    {
                        current.DeliveryDate = request.DeliveryDate;
                    }
                    else
                    {
                        current.DeliveryDate = deliveryDate;
                    }
                }
            }
        }

        private void SetCreateDate(PolarisBedRequest request, BedRequest current, DateTime createDate)
        {
            if (current.CreateDate == null || current.CreateDate == DateTime.MinValue)
            {
                if (request.DateOfRequest != DateTime.MinValue)
                {
                    current.CreateDate = request.DateOfRequest;
                }
                else
                {
                    current.CreateDate = createDate;
                }
            }
        }

        private void SetFirstNameLastName(PolarisBedRequest request, BedRequest current)
        {
            string[] splitWords = new[] { "-", "(", ",", " or "};
            request.RequestersName = request.RequestersName.Replace(")", "").Trim();

            foreach (string splitWord in splitWords)
            {
                if (request.RequestersName.Contains(splitWord))
                {
                    string[] parts = request.RequestersName.Split(splitWord);
                    request.RequestersName =parts[0].Trim();

                    if (parts.Length > 1)
                    {
                        AddNote(current,  "Additional Name: " + String.Join(", ", parts.Skip(1).Select(p => p.Trim())));
                    }
                }
            }

            if (request.RequestersName.ToLower().Contains("red de vida unlimited"))
            {
                request.RequestersName = request.RequestersName.Replace("Red de Vida Unlimited", string.Empty,StringComparison.OrdinalIgnoreCase).Trim();
                AddNote(current, "Red de Vida Unlimited");
            }

            var nameParts = _nameParserLogic.ParseName(request.RequestersName, NameOrder.FirstLast);
            current.FirstName = nameParts.FirstName;
            current.LastName = nameParts.LastName;

            if (string.IsNullOrWhiteSpace(current.FirstName) && string.IsNullOrWhiteSpace(current.LastName))
            {
                current.FirstName = string.Empty;
                current.LastName = request.RequestersName;
            }

            if (string.IsNullOrWhiteSpace(current.FirstName))
            {
                current.FirstName = "Unknown";
            }

            if (string.IsNullOrWhiteSpace(current.LastName))
            {
                current.LastName = string.Empty;
            }
        }

        private void SetReference(PolarisBedRequest request, BedRequest current)
        {
            if (String.IsNullOrWhiteSpace(request.Name))
                current.Reference = "Polaris Website";
            else if (request.Name.ToLower().Contains("incoming") || request.Name.ToLower().Contains("new"))
                current.Reference = "Polaris Website";
            else if (request.Name.ToLower().Contains("grove") || request.Name.ToLower().Contains("gc"))
                current.Reference = "Grove City Website";
            else if (request.Name.ToLower().Contains("phone"))
                current.Reference = "Phone Call";
            else if (request.Name.ToLower().Contains("email"))
                current.Reference = "Email";
            else if (request.Name.ToLower().Contains("text to katie"))
                current.Reference = "Text to Katie";
            else if (request.Name.ToLower().Contains("facebook") || request.Name.ToLower().Contains("fb"))
                current.Reference = "Facebook";
            else if (request.Name.ToLower().Contains("import"))
                current.Reference = "Import from List";
            else if (request.Name.ToLower().Contains("spreadsheet"))
                current.Reference = "Spreadsheet";
            else if (request.Name.ToLower().Contains("tierra"))
                current.Reference = "tierra";
            else if (request.Name.ToLower().Contains("nyap"))
                current.Reference = "NYAP";
            else if (request.Name.ToLower().Contains("walk"))
                current.Reference = "Walk-in";
            else
                current.Reference = request.Name;
        }

        private void SetStatus(PolarisBedRequest request, BedRequest current)
        {
            switch (request.Group)
            {
                case "New Requests":
                case "Grove City requests":
                case "Additional needs":
                    current.Status = BedBrigade.Common.Enums.BedRequestStatus.Waiting;
                    current.Contacted =false;
                    break;
                case "Contacted (Waiting)":
                    current.Status = BedBrigade.Common.Enums.BedRequestStatus.Waiting;
                    current.Contacted = true;
                    break;
                case "August 9th delivery":
                    current.Status = BedBrigade.Common.Enums.BedRequestStatus.Scheduled;
                    current.Contacted = true;
                    break;
                case "Unable to contact after 3 months":
                    current.Status = BedBrigade.Common.Enums.BedRequestStatus.Cancelled;
                    current.Contacted = false;
                    AddNote(current, "Unable to contact after 3 months");
                    break;
                case "Wrong phone number listed":
                    current.Status = BedBrigade.Common.Enums.BedRequestStatus.Cancelled;
                    current.Contacted = false;
                    AddNote(current, "Wrong phone number listed");
                    break;
                case "Delivered":
                    current.Status = BedBrigade.Common.Enums.BedRequestStatus.Delivered;
                    current.Contacted = true;
                    break;
                default:
                    Assert.Fail($"Unknown group {request.Group}");
                    break;
            }
        }

        private void SetNotes(PolarisBedRequest request, BedRequest current)
        {
            AddNote(current, request.Reference);
            AddNote(current, request.DeliveryNotes);
        }

        private void AddNote(BedRequest current, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return;

            if (string.IsNullOrEmpty(current.Notes))
            {
                current.Notes = note;
            }
            else if (!current.Notes.Contains(note))
            {
                current.Notes += " " + note;
            }
        }

        private void AddBedType(BedRequest current, string bedType)
        {
            if (string.IsNullOrWhiteSpace(bedType))
                return;
            if (string.IsNullOrEmpty(current.BedType))
            {
                current.BedType = bedType;
            }
            else if (!current.BedType.Contains(bedType))
            {
                current.BedType += ", " + bedType;
            }
        }

        private void AddName(BedRequest current, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (string.IsNullOrEmpty(current.Names))
            {
                current.Names = name;
            }
            else if (!current.Names.Contains(name))
            {
                current.Names += ", " + name;
            }
        }


        public DataContext CreateDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new DataContext(optionsBuilder.Options);
        }

        private List<PolarisBedRequest> ExcelToPolarisBedRequests()
        {
            List<PolarisBedRequest> results = new List<PolarisBedRequest>();
            SyncfusionLicenseProvider.RegisterLicense(LicenseLogic.SyncfusionLicenseKey);
            ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;
           
            IWorkbook workbook = application.Workbooks.Open(FilePath);
            IWorksheet sheet = workbook.Worksheets.First();
            IRange usedRange = sheet.UsedRange;
            int lastRow = usedRange.LastRow;
            string group = "New Requests";

            for (int i = 1; i <= lastRow; i++)
            {
                string name = usedRange[i, 1].Value?.ToString();

                // Skip empty and header rows
                if (string.IsNullOrEmpty(name) || name == "Name" || name == "Bed Requests")
                {
                    continue; 
                }
                // Check if the group has changed
                if (groups.Contains(name))
                {
                    group = name;
                    continue; // Skip the group header row
                }
                PolarisBedRequest bedRequest = new PolarisBedRequest
                {
                    Group = group,
                    Name = usedRange[i, 1].Value?.ToString(),
                    RequestersName = usedRange[i, 2].Value?.ToString(),
                    DateOfRequest = usedRange[i, 3].DateTime,
                    BedForAdultOrChild = usedRange[i, 4].Value?.ToString(),
                    BedType = usedRange[i, 5].Value?.ToString(),
                    AdultName = usedRange[i, 6].Value?.ToString(),
                    AdultGender = usedRange[i, 7].Value?.ToString(),
                    ChildAge = usedRange[i, 8].Value?.ToString(),
                    ChildGender = usedRange[i, 9].Value?.ToString(),
                    ChildName = usedRange[i, 10].Value?.ToString(),
                    DeliveryAddress = usedRange[i, 11].Value?.ToString(),
                    Email = usedRange[i, 12].Value?.ToString(),
                    PrimaryLanguage = usedRange[i, 13].Value?.ToString(),
                    PhoneNumber = usedRange[i, 14].Value?.ToString(),
                    SpeaksEnglish = usedRange[i, 15].Value?.ToString(),
                    Status = usedRange[i, 16].Value?.ToString(),
                    DeliveryDate = usedRange[i, 17].DateTime,
                    TeamLead1 = usedRange[i, 18].Value?.ToString(),
                    Reference = usedRange[i, 23].Value?.ToString(),
                    DeliveryNotes = usedRange[i, 24].Value?.ToString()
                };
                results.Add(bedRequest);
            }

            return results;
        }
    }
}
