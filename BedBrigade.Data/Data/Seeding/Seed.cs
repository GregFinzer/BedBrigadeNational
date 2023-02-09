using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using BedBrigade.Data.Models;

namespace BedBrigade.Data.Seeding
{
    public class Seed
    {
        private const string _seedUserName = "Seed";
        private const string _seedLocationNational = "National";
        private const string _seedLocationNationalName = "Bed Brigade Columbus";



        // Table User
        private const string _seedUserPassword = "Password";
        private const string _seedUserLocation = "Location";

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
            new User {FirstName =  _seedLocationNational, LastName = "Editor", Role = "National Editor"},
            new User {FirstName =  _seedLocationNational, LastName = "Admin", Role = "National Admin"}

        };
        static readonly List<User> Users = users;

        public static async Task SeedData(DataContext context)
        {
            await SeedConfigurations(context);
            await SeedLocations(context);
            await SeedContents(context);
            await SeedMedia(context);
            await SeedRoles(context);
            await SeedUser(context);
            await SeedUserRoles(context);
        }


        private static async Task SeedConfigurations(DataContext context)
        {
            if (context.Configurations.Any()) return;

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
                ConfigurationValue = "8889"}
            };

            await context.Configurations.AddRangeAsync(configurations);
            await context.SaveChangesAsync();
        }
        private static async Task SeedLocations(DataContext context)
        {
            if(context.Configurations.Any()) return;    
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
        private static async Task SeedContents(DataContext context)
        {
            var header = "Header";
            if (!context.Content.Any(c => c.ContentType == header))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNationalName);
                var seedHtml = GetHtml("header.html");
                context.Content.Add(new Content
                {
                    Location = location!,
                    ContentType = header,
                    Name = header,
                    ContentHtml = seedHtml
                }); ;
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }
        private static async Task SeedMedia(DataContext context)
        {
            try
            {
                if (!context.Media.Any(m => m.FileName == "Logo"))
                {
                    var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNational);
                    context.Media.Add(new Media
                    {
                        Location = location!,
                        FileName = "logo",
                        MediaType = "png",
                        Path = "media/national",
                        AltText = "Bed Brigade National Logo",
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        CreateUser = _seedUserName,
                        UpdateUser = _seedUserName,
                        MachineName = Environment.MachineName,
                    });

                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seed media: {ex.Message}");
            }
        }
        private static async Task SeedRoles(DataContext context)
        {
            if (context.Roles.Any()) return;
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
                if (!context.Users.Any(u => u.UserName == $"{user.FirstName}{user.LastName}"))
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

        private static string GetHtml(string fileName)
        {
            var html = File.ReadAllText($"../BedBrigade.Data/Data/Seeding/SeedHtml/{fileName}");
            return html;
        }

    }



    }

