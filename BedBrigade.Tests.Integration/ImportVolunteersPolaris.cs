using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data;
using Bogus.DataSets;
using KellermanSoftware.NameParser;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Licensing;
using Syncfusion.XlsIO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BedBrigade.Tests.Integration
{
    [TestFixture]
    public class ImportVolunteersPolaris
    {
        private const string FilePath = @"C:\Users\gfinz\Downloads\Volunteer_Management_1760555072.xlsx";
        private const string ConnectionString =
            "server=localhost\\sqlexpress;database=bedbrigade;trusted_connection=SSPI;Encrypt=False";
        private readonly NameParserLogic _nameParserLogic = LibraryFactory.CreateNameParser();
        private readonly Regex _phoneRegex = new Regex(Validation.PhoneRegexPattern, RegexOptions.Compiled);
        private string[] groups = new[]
        {
            "Volunteer Management",
            "New Volunteers",
            "Spanish Speakers",
            "Build Volunteers",
            "Delivery Volunteers"
        };

        [Test, Ignore("Only run locally manually")]
        //[Test]
        public async Task Import()
        {
            if (!TestHelper.IsWindows() || !TestHelper.ThisComputerHasExcelInstalled())
            {
                Assert.Ignore("This test should only run locally.");
            }

            DataContext context = CreateDbContext(ConnectionString);
            await DeleteExistingVolunteers(context);
            List<PolarisVolunteer> polarisVolunteers = ExcelToVolunteers();
            List<Volunteer> volunteers = CombinePolaris(polarisVolunteers);
            InsertVolunteers(context, volunteers);
        }

        private void InsertVolunteers(DataContext context, List<Volunteer> volunteers)
        {
            context.Volunteers.AddRange(volunteers);
            context.SaveChanges();
        }

        private List<Volunteer> CombinePolaris(List<PolarisVolunteer> polarisVolunteers)
        {
            List<Volunteer> volunteers = new List<Volunteer>();

            foreach (var polarisVolunteer in polarisVolunteers)
            {
                Volunteer volunteer = new Volunteer();
                volunteer.Message = string.Empty;
                volunteer.LocationId = Defaults.PolarisLocationId;
                volunteer.Group = polarisVolunteer.Group;
                volunteer.IHaveVolunteeredBefore = volunteer.Group != "New Volunteers";
                SetName(polarisVolunteer, volunteer);
                volunteer.CreateDate = ParseCreationLog(polarisVolunteer.CreationLog);
                volunteer.CreateUser = "Import";
                volunteer.UpdateDate = volunteer.CreateDate;
                volunteer.UpdateUser = volunteer.CreateUser;
                volunteer.MachineName = Environment.MachineName;
                volunteer.Email = polarisVolunteer.EmailAddress;
                SetPhone(volunteer, polarisVolunteer.PhoneNumber);
                volunteer.AttendChurch = (polarisVolunteer.DoYouAttendChurch ?? string.Empty).ToLower().Contains("yes");
                volunteer.ChurchName = polarisVolunteer.ChurchName;
                SetArea(polarisVolunteer, volunteer);

                if ((polarisVolunteer.OtherArea ?? string.Empty).Length > 100)
                {
                    volunteer.Message = polarisVolunteer.OtherArea;
                }
                else
                {
                    volunteer.OtherArea = polarisVolunteer.OtherArea ?? string.Empty;
                }
                SetVehicle(polarisVolunteer, volunteer);
                SetLanguage(polarisVolunteer, volunteer);

                if (!String.IsNullOrEmpty(volunteer.Phone) && volunteers.Any(o => o.Phone == volunteer.Phone))
                {
                    continue;
                }

                if (!String.IsNullOrEmpty(volunteer.Email) 
                    && volunteer.Email != "mssalas1990@gmail.com" //They share an email address
                    && volunteers.Any(o => o.Email == volunteer.Email))
                {
                    continue;
                }

                volunteers.Add(volunteer);
            }

            return volunteers;
        }

        private void SetLanguage(PolarisVolunteer polarisVolunteer, Volunteer volunteer)
        {
            try
            {
                if ((polarisVolunteer.DoYouSpeakSpanish ?? string.Empty).ToLower().Contains("yes"))
                {
                    volunteer.OtherLanguagesSpoken = "Spanish";
                    polarisVolunteer.CanYouTranslate = (polarisVolunteer.CanYouTranslate ?? string.Empty).Replace(" ", string.Empty);

                    if (String.IsNullOrWhiteSpace(polarisVolunteer.CanYouTranslate))
                        polarisVolunteer.CanYouTranslate = "No";

                    volunteer.CanYouTranslate =
                        (CanYouTranslate)Enum.Parse<CanYouTranslate>(polarisVolunteer.CanYouTranslate, true);
                }
                else
                {
                    volunteer.OtherLanguagesSpoken = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

        }

        private void SetVehicle(PolarisVolunteer polarisVolunteer, Volunteer volunteer)
        {
            if ((polarisVolunteer.DoYouHaveAVehicle ?? string.Empty).ToLower().Contains("yes"))
            {
                polarisVolunteer.VehicleType = (polarisVolunteer.VehicleType ?? string.Empty).ToLower();

                if (polarisVolunteer.VehicleType.Contains("mini"))
                {
                    volunteer.VehicleType = VehicleType.Minivan;
                }
                else if (polarisVolunteer.VehicleType.Contains("van"))
                {
                    volunteer.VehicleType = VehicleType.FullSizeVan;
                }
                else if (polarisVolunteer.VehicleType.Contains("truck"))
                {
                    volunteer.VehicleType = VehicleType.Truck;
                }
                else if (polarisVolunteer.VehicleType.Contains("small"))
                {
                    volunteer.VehicleType = VehicleType.SmallSUV;
                }
                else if (polarisVolunteer.VehicleType.Contains("medium"))
                {
                    volunteer.VehicleType = VehicleType.MediumSUV;
                }
                else if (polarisVolunteer.VehicleType.Contains("large"))
                {
                    volunteer.VehicleType = VehicleType.SmallSUV;
                }
                else
                {
                    volunteer.VehicleType = VehicleType.Other;
                    volunteer.VehicleDescription = polarisVolunteer.VehicleDescription ?? string.Empty;
                }
            }
            else
            {
                volunteer.VehicleType = VehicleType.None;
            }
        }

        private void SetArea(PolarisVolunteer polarisVolunteer, Volunteer volunteer)
        {
            polarisVolunteer.VolunteerArea = polarisVolunteer.VolunteerArea ?? string.Empty;
            volunteer.VolunteerArea = string.Empty;

            if (polarisVolunteer.VolunteerArea.ToLower().Contains("building"))
            {
                volunteer.VolunteerArea = StringUtil.AddToCommaDelimitedList(volunteer.VolunteerArea, "Bed Building");
            }

            if (polarisVolunteer.VolunteerArea.ToLower().Contains("delivery"))
            {
                volunteer.VolunteerArea = StringUtil.AddToCommaDelimitedList(volunteer.VolunteerArea, "Bed Delivery");
            }

            if (polarisVolunteer.VolunteerArea.ToLower().Contains("planning"))
            {
                volunteer.VolunteerArea = StringUtil.AddToCommaDelimitedList(volunteer.VolunteerArea, "Event Planning");
            }
        }

        private void SetPhone(Volunteer volunteer, string phoneInput)
        {
            if (string.IsNullOrWhiteSpace(phoneInput))
            {
                volunteer.Phone = string.Empty;
                return;
            }

            //Get phone matches
            MatchCollection phoneMatches = _phoneRegex.Matches(phoneInput);

            if (phoneMatches.Count == 0)
            {
                volunteer.Phone = string.Empty;
                return;
            }

            //If there are multiple matches, use the first one
            volunteer.Phone = phoneMatches[0].Value.FormatPhoneNumber();

            if (phoneMatches.Count > 1)
            {
                //We already added the other phone numbers to the notes
                if (volunteer.Message.Contains(phoneMatches[1].Value.FormatPhoneNumber()))
                {
                    return;
                }

                if (volunteer.Message.Length > 0)
                {
                    volunteer.Message += " ";
                }

                volunteer.Message += "Other Phone Numbers: ";
                volunteer.Message+= string.Join(", ", phoneMatches.Cast<Match>().Skip(1).Select(m => m.Value.FormatPhoneNumber()));
            }
        }

        private void SetName(PolarisVolunteer polarisVolunteer, Volunteer volunteer)
        {
            if (!String.IsNullOrEmpty(polarisVolunteer.Apellido))
            {
                volunteer.FirstName = polarisVolunteer.Name;
                volunteer.LastName = polarisVolunteer.Apellido;
            }
            else
            {
                var nameParts = _nameParserLogic.ParseName(polarisVolunteer.Name, NameOrder.FirstLast);

                if (nameParts != null && !String.IsNullOrEmpty(nameParts.FirstName))
                {
                    volunteer.FirstName = nameParts.FirstName;
                    volunteer.LastName = nameParts.LastName;
                }
                else
                {
                    volunteer.FirstName = polarisVolunteer.Name;
                    volunteer.LastName = string.Empty; 
                }
            }
        }

        private List<PolarisVolunteer> ExcelToVolunteers()
        {
            List<PolarisVolunteer> results = new List<PolarisVolunteer>();
            SyncfusionLicenseProvider.RegisterLicense(LicenseLogic.SyncfusionLicenseKey);
            ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Open(FilePath);
            IWorksheet sheet = workbook.Worksheets.First();
            IRange usedRange = sheet.UsedRange;
            int lastRow = usedRange.LastRow;
            string group = "New Volunteers";

            for (int i = 1; i <= lastRow; i++)
            {
                string name = usedRange[i, 1].Value?.ToString();

                // Skip empty and header rows
                if (string.IsNullOrEmpty(name) || name == "Name" || name == "Volunteer Management")
                {
                    continue;
                }
                // Check if the group has changed
                if (groups.Contains(name))
                {
                    group = name;
                    continue; // Skip the group header row
                }
                PolarisVolunteer volunteer = new PolarisVolunteer
                {
                    Group = group,
                    Name = usedRange[i, 1].Value?.ToString(),
                    Apellido = usedRange[i, 2].Value?.ToString(),
                    CreationLog = usedRange[i, 3].Value.ToString(),
                    EmailAddress = usedRange[i, 4].Value?.ToString(),
                    PhoneNumber = usedRange[i, 5].Value?.ToString(),
                    DoYouAttendChurch = usedRange[i, 6].Value?.ToString(),
                    ChurchName = usedRange[i, 7].Value?.ToString(),
                    VolunteerArea = usedRange[i, 8].Value?.ToString(),
                    OtherArea = usedRange[i, 9].Value?.ToString(),
                    DoYouHaveAVehicle = usedRange[i, 10].Value?.ToString(),
                    VehicleType = usedRange[i, 11].Value?.ToString(),
                    VehicleDescription = usedRange[i, 12].Value?.ToString(),
                    DoYouSpeakSpanish = usedRange[i, 14].Value?.ToString(),
                    CanYouTranslate = usedRange[i, 15].Value?.ToString(),
                };
                results.Add(volunteer);
            }

            return results;
        }

        public DataContext CreateDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new DataContext(optionsBuilder.Options);
        }

        public async Task DeleteExistingVolunteers(DataContext context)
        {
            var volunteers = await context.Volunteers.ToListAsync();
            context.Volunteers.RemoveRange(volunteers);
            await context.SaveChangesAsync();
        }


        /// <summary>
        /// Generated by Chat GPT 5
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private DateTime ParseCreationLog(string input)
        {
            // Example: "Katie McDaniel Dec 15, 2022 9:07 AM"
            // Step 1: Extract just the date/time portion
            var parts = input.Split(' ', 3);
            if (parts.Length < 3)
                throw new FormatException("Input does not contain enough parts to parse.");

            string dateTimePart = input.Substring(input.IndexOf(parts[2]));

            // Step 2: Parse to DateTime with no time zone
            if (!DateTime.TryParseExact(
                    dateTimePart,
                    "MMM d, yyyy h:mm tt",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var easternTime))
            {
                throw new FormatException("Could not parse date/time portion.");
            }

            // Step 3: Interpret as Eastern Time and convert to UTC
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(easternTime, easternZone);

            return utcTime;
        }
        

}
}
