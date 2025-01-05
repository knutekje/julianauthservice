using AuthService.Models;

namespace AuthService.Data;

public static class DbInitializer
{
    public static void SeedUsers(AuthDbContext context)
    {
        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = Roles.Admin,
                    Email = "admin@example.com"
                },
                new User
                {
                    Username = "receptionist",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Receptionist@123"),
                    Role = Roles.Receptionist,
                    Email = "receptionist@example.com"
                },
                new User
                {
                    Username = "housekeeper",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Housekeeping@123"),
                    Role = Roles.Housekeeping,
                    Email = "housekeeping@example.com"
                },
                new User
                {
                    Username = "fb",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("FB@123"),
                    Role = Roles.FAndB,
                    Email = "fb@example.com"
                },
                new User
                {
                    Username = "maintenance",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Maintenance@123"),
                    Role = Roles.Maintenance,
                    Email = "maintenance@example.com"
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }
}
