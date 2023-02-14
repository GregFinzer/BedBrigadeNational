using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Models;

namespace BedBrigade.Data.Seeding
{
    public class Seed
    {


        // Table User
        private const string _seedUserPassword = "Password";
        private const string _seedUserLocation = "Location";
        private const string _seedUserLocation1 = "Location1";
        private const string _seedUserLocation2 = "Location2";

        private static List<Role> Roles = new()
        {
            new Role {Name = "National Admin"},
            new Role {Name = "National Editor" },
            new Role {Name = "Location Admin"},
            new Role {Name = "Location Editor"},
            new Role {Name = "Location Scheduler"},
            new Role {Name = "Location Contributor"},
            new Role {Name = "Location Treasurer"},
            new Role {Name = "Location Communications"},
            new Role {Name = "Location Author"}
        };

        private static List<Location> locations = new()
        {
            new Location {Name="Bed Brigade Columbus", Route="/", Address1="", Address2="", City="Columbus", State="Ohio", PostalCode=""},
            new Location {Name="Bed Brigade Living Hope Church", Route="/newark", Address1="", Address2="", City="Newark", State="Ohio", PostalCode=""},
            new Location {Name="Bed Brigade Rock City Polaris", Route="/rock", Address1="", Address2="", City="Rock City", State="Ohio", PostalCode=""},
            new Location {Name="Bed Brigade Peace Lutheran", Route="/linden", Address1="", Address2="", City="Linden", State="Ohio", PostalCode=""},
            new Location {Name="Bed Brigade Vinyard Church Circleville", Route="/Circleville", Address1="", Address2="", City="Circleville", State="Ohio", PostalCode=""},
            new Location {Name="Bed Brigade Hardbarger Impact", Route="/lancaster", Address1="", Address2="", City="Lancaster", State="Ohio", PostalCode=""},
            new Location {Name="Bed Brigade Upper Arlington Lutheran Church", Route="/Arlington", Address1="", Address2="", City="Arlington", State="Ohio", PostalCode=""},
            new Location {Name="Bed Brigade Greensburg United Methodist Church", Route="/canton", Address1="", Address2="", City="Canton", State="Ohio", PostalCode=""}

        };

        private static readonly List<User> users = new()
        {
            new User { FirstName = _seedUserLocation, LastName = "Contributor", Role = "Location Contributor" },
            new User {FirstName = _seedUserLocation, LastName = "Author", Role = "Location Author" },
            new User {FirstName = _seedUserLocation, LastName = "Editor", Role = "Location Editor" },
            new User {FirstName = _seedUserLocation, LastName = "Scheduler", Role = "Location Scheduler" },
            new User {FirstName = _seedUserLocation, LastName = "Treasurer", Role = "Location Treasurer"},
            new User {FirstName = _seedUserLocation, LastName = "Communications", Role = "Location Communications"},
            new User {FirstName = _seedUserLocation, LastName = "Admin", Role = "Location Admin"},
            new User {FirstName =  SeedConstants.SeedLocationNational, LastName = "Editor", Role = "National Editor"},
            new User {FirstName =  SeedConstants.SeedLocationNational, LastName = "Admin", Role = "National Admin"},
            new User { FirstName = _seedUserLocation1, LastName = "Contributor", Role = "Location Contributor" },
            new User {FirstName = _seedUserLocation1, LastName = "Author", Role = "Location Author" },
            new User {FirstName = _seedUserLocation1, LastName = "Editor", Role = "Location Editor" },
            new User {FirstName = _seedUserLocation1, LastName = "Scheduler", Role = "Location Scheduler" },
            new User {FirstName = _seedUserLocation1, LastName = "Treasurer", Role = "Location Treasurer"},
            new User {FirstName = _seedUserLocation1, LastName = "Communications", Role = "Location Communications"},
            new User {FirstName = _seedUserLocation1, LastName = "Admin", Role = "Location Admin"},
            new User { FirstName = _seedUserLocation2, LastName = "Contributor", Role = "Location Contributor" },
            new User {FirstName = _seedUserLocation2, LastName = "Author", Role = "Location Author" },
            new User {FirstName = _seedUserLocation2, LastName = "Editor", Role = "Location Editor" },
            new User {FirstName = _seedUserLocation2, LastName = "Scheduler", Role = "Location Scheduler" },
            new User {FirstName = _seedUserLocation2, LastName = "Treasurer", Role = "Location Treasurer"},
            new User {FirstName = _seedUserLocation2, LastName = "Communications", Role = "Location Communications"},
            new User {FirstName = _seedUserLocation2, LastName = "Admin", Role = "Location Admin"},
        };
        static readonly List<User> Users = users;

        List<Volunteer> volunteers = new List<Volunteer>();

        public static async Task SeedData(DataContext context)
        {
            await SeedVolunteers(context);
            await SeedConfigurations(context);
            await SeedLocations(context);
            await SeedContentsLogic.SeedContents(context);
            await SeedMedia(context);
            await SeedRoles(context);
            await SeedUser(context);
            await SeedUserRoles(context);

        }

        private static async Task SeedVolunteers(DataContext context)
        {
            if (context.Volunteers.Any()) return;
            try
            {

                List<string>? volunteeringFor = new List<string> { "Bed Delivery", "Bed Building", "Event Planning", "New Option", "Other" };
                List<bool> YesOrNo = new List<bool> { true, false };
                List<string> FirstName = new List<string> { "Bill", "Bob", "Mike", "Val", "Sally", "Sue", "Sandy", "William", "John", "Toby", "Betty", "Steven", "Chris", "Richard", "Ron", "Elliot" };
                List<string> LastName = new List<string> { "Smith", "Williamson", "McNally", "Fender", "Hauser", "Thompson", "Noble", "William", "Johnson", "Toble", "Bole", "Sith", "Stringer", "Richardson", "Johanson", "Swatrz" };
                List<string> EmailAgents = new List<string> { "hotmail.com", "gmail.com", "yahoo.com", "outlook.com" };

                List<Volunteer> Volunteers = new List<Volunteer>();
                for (int i = 0; i < 100; i++)
                {
                    var emailAgent = EmailAgents[new Random().Next(EmailAgents.Count - 1)];
                    var firstName = FirstName[new Random().Next(FirstName.Count - 1)];
                    var lastName = LastName[new Random().Next(LastName.Count - 1)];

                    var volunteer = new Volunteer
                    {
                        Location = locations[new Random().Next(locations.Count - 1)],
                        VolunteeringFor = volunteeringFor[new Random().Next(volunteeringFor.Count - 1)],
                        VolunteeringForDate = DateTime.Now.AddDays(new Random().Next(30)),
                        IHaveVolunteeredBefore = YesOrNo[new Random().Next(2)],
                        FirstName = firstName,
                        LastName = lastName,
                        Email = $"{firstName}.{lastName}@{emailAgent}",
                        Phone = "(999) 999-9999",
                        OrganizationOrGroup = string.Empty,
                        IHaveAMinivan = YesOrNo[new Random().Next(2)],
                        IHaveAnSUV = YesOrNo[new Random().Next(2)],
                        IHaveAPickupTruck = YesOrNo[new Random().Next(2)]
                    };
                    Volunteers.Add(volunteer);
                    await context.Volunteers.AddAsync(volunteer);
                    var count = await context.SaveChangesAsync();
                }
                }
                catch (DbException ex)
                {
                    Console.WriteLine($"Error processing volunteer seed - {ex.Message} ");
                    throw;

                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error processing volunteer seed - {ex.Message} ");
                    throw;
                }



        }

        private static async Task SeedConfigurations(DataContext context)
        {
            if (await context.Configurations.AnyAsync()) return;

            var configurations = new List<Configuration>
            {
                new()
                {
                    ConfigurationKey = "FromEmailAddress",
                    ConfigurationValue = "webmaster@bedbrigade.org",
                },
                new()
                {
                    ConfigurationKey = "HostName",
                    ConfigurationValue = "mail.bedbrigade.org",
                },
                new()
                {ConfigurationKey = "Port",
                ConfigurationValue = "8889"},
                new()
                {
                    ConfigurationKey = "TokenExpiration",
                    ConfigurationValue = "24"
                }
            };

            await context.Configurations.AddRangeAsync(configurations);
            await context.SaveChangesAsync();
        }
        private static async Task SeedLocations(DataContext context)
        {
            if(await context.Locations.AnyAsync()) return;    

            try
            {
                await context.Locations.AddRangeAsync(locations);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration seed error: {ex.Message}");
            }
        }

        private static async Task SeedMedia(DataContext context)
        {
            try
            {
                if (!await context.Media.AnyAsync(m => m.FileName == "Logo")) // table Media does not have site logo
                {
                    // var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNational);
                    // add the first reciord in Media table with National Logo
                    context.Media.Add(new Media
                    {
                        LocationId = 1,
                        FileName = "logo",
                        MediaType = "png",
                        FilePath = "media/national",
                        FileSize = 9827,
                        AltText = "Bed Brigade National Logo",
                        FileStatus = "seed",
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        CreateUser = SeedConstants.SeedUserName,
                        UpdateUser = SeedConstants.SeedUserName,
                        MachineName = Environment.MachineName
                    });

                    await context.SaveChangesAsync();
                } // add the first media row
                if (!await context.Media.AnyAsync(m => m.FileStatus == "test"))
                {
                    // add additional test files - should be removed in production version - VS 2/9/2023
                    // media table content & physical files synchronization is part of Media Manager
                    await context.Media.AddRangeAsync(TestMedia);
                    await context.SaveChangesAsync();
                }
         
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seed media: {ex.Message}");
            }
        } // Seed Media

        private static async Task SeedRoles(DataContext context)
        {
            if (await context.Roles.AnyAsync()) return;
            try
            {
                await context.Roles.AddRangeAsync(Roles);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroor adding roles {ex.Message}");
            }
        }
        private static async Task SeedUser(DataContext context)
        {
            foreach (var user in Users)
            {
                if (!await context.Users.AnyAsync(u => u.UserName == $"{user.FirstName}{user.LastName}"))
                {
                    try
                    {
                        SeedRoutines.CreatePasswordHash(_seedUserPassword, out byte[] passwordHash, out byte[] passwordSalt);

                        var location = locations[new Random().Next(locations.Count)];

                        // Create the user
                        var newUser = new User
                        {
                            UserName = $"{user.FirstName}{user.LastName}",
                            Location = location,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = $"{user.FirstName}.{user.LastName}@bedBrigade.org".ToLower(),
                            Phone = "(999) 999-9999",
                            Role = user.Role,
                            PasswordHash = passwordHash,
                            PasswordSalt = passwordSalt,
                        };
                        context.Users.Add(newUser);
                        await context.SaveChangesAsync();
                        //}
                        //catch (Exception ex) { Console.WriteLine(ex.Message); }
                        //try
                        //{ 
                        // Now store the user and a role

                        //var userrole = new UserRole
                        //{
                        //    Location = location,
                        //    Role = context.Roles.FirstOrDefault(r => r.Name == user.Role),
                        //    User = newUser
                        //};
                        //context.UserRoles.Add(userrole);
                        //context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SaveChanges Error {ex.Message}");
                    }
                }
            }
        }
        private static async Task SeedUserRoles(DataContext context)
        {
            try
            {
                var users = context.Users.ToList();
                foreach (var user in users)
                {
                    var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == user.Role);
                    UserRole newUserRole = new()
                    {
                        Location = user.Location,
                        Role = await context.Roles.FirstOrDefaultAsync(r => r.Name == user.Role),
                        User = user
                    };
                    await context.AddAsync(newUserRole);
                    await context.SaveChangesAsync();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error SeedUserRoles {ex.Message}");
            }
        }



        // added by VS for testing time
        private static readonly List<Media> TestMedia = new()
        {
        new Media     {
            // MediaId= 2,
            LocationId= 1,
            FilePath= "media/national",
            FileName= "usamap",
            MediaType= "jpg",
            AltText= "USA Map",
            FileSize= 146502,
            CreateDate= DateTime.Now,
            CreateUser= "vskordin@gmail.com",
            UpdateDate= DateTime.Now,
            UpdateUser= "vskordin@gmail.com",
            MachineName= "DELLXPS-8930",
            FileStatus="test"
        },
        new Media  {
            //MediaId= 3,
            LocationId= 1,
            FilePath= "media/national",
            FileName= "CODEFocus",
            MediaType= "pdf",
            AltText= "Magazine (PDF Example)",
            FileSize= 6267940,
            CreateDate= DateTime.Now,
            CreateUser= "vskordin@gmail.com",
            UpdateDate= DateTime.Now,
            UpdateUser= "vskordin@gmail.com",
            MachineName= "DELLXPS-8930",
            FileStatus="test"
        },
        new Media  {
            //MediaId= 4,
            LocationId= 2,
            FilePath= "media/ohio",
            FileName= "ohioflag",
            MediaType= "png",
            AltText= "Ohio State Flag",
            FileSize= 33419,
            CreateDate= DateTime.Now,
            CreateUser= "vskordin@gmail.com",
            UpdateDate= DateTime.Now,
            UpdateUser= "vskordin@gmail.com",
            MachineName= "DELLXPS-8930",
            FileStatus="test"
        },
        new Media{
                //MediaId= 5,
                LocationId= 2,
                FilePath= "media/ohio",
                FileName= "ohiomap",
                MediaType= "jpg",
                AltText= "Ohio State Map",
                FileSize= 468934,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
        new Media{
                //MediaId= 6,
                LocationId= 2,
                FilePath= "media/ohio",
                FileName= "CODE2023_1",
                MediaType= "pdf",
                AltText= "PDF File Example",
                FileSize= 7773635,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
            new Media{
                //MediaId= 7,
                LocationId= 2,
                FilePath= "media/ohio",
                FileName= "OhioStateSeal",
                MediaType= "jpg",
                AltText= "Ohio State Seal",
                FileSize= 70694,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
           new Media {
                //MediaId= 8,
                LocationId= 2,
                FilePath= "media/ohio",
                FileName= "FordExplorer2006",
                MediaType= "jpg",
                AltText= "Ford Explorer 2006",
                FileSize= 51567,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 9,
                LocationId= 3,
                FilePath= "media/arizona",
                FileName= "arizonamap",
                MediaType= "jpg",
                AltText= "Arizona State Map",
                FileSize= 72696,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
           new Media {
                //MediaId= 10,
                LocationId= 3,
                FilePath= "media/arizona",
                FileName= "arizonaflag",
                MediaType= "png",
                AltText= "Arizona State Flag",
                FileSize= 24150,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 11,
                LocationId= 3,
                FilePath= "media/arizona",
                FileName= "DNCMag49",
                MediaType= "pdf",
                AltText= "PDF File Example",
                FileSize= 19564686,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 12,
                LocationId= 3,
                FilePath= "media/arizona",
                FileName= "arizonastateseal",
                MediaType= "webp",
                AltText= "Arizona State Seal",
                FileSize= 44802,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 13,
                LocationId= 3,
                FilePath= "media/arizona",
                FileName= "FordT_1925",
                MediaType= "jpg",
                AltText= "Ford Model T 1925",
                FileSize= 39665,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 14,
                LocationId= 2,
                FilePath= "media/ohio",
                FileName= "Mercury1950",
                MediaType= "jpg",
                AltText= "Mercury 1950",
                FileSize= 35468,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 15,
                LocationId= 3,
                FilePath= "media/arizona",
                FileName= "Lincoln2006",
                MediaType= "jpg",
                AltText= "Lincoln Concept 2006",
                FileSize= 35890,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 16,
                LocationId= 1,
                FilePath= "media/national",
                FileName= "usaseal",
                MediaType= "jpg",
                AltText= "Great Seal of USA",
                FileSize= 103948,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            },
          new Media  {
                //MediaId= 17,
                LocationId= 1,
                FilePath= "media/national",
                FileName= "Edsel1958",
                MediaType= "jpg",
                AltText= "Ford Edsel 1958",
                FileSize= 53399,
                CreateDate= DateTime.Now,
                CreateUser= "vskordin@gmail.com",
                UpdateDate= DateTime.Now,
                UpdateUser= "vskordin@gmail.com",
                MachineName= "DELLXPS-8930",
                FileStatus="test"
            }
        };



    }



    }

