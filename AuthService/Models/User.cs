namespace AuthService.Models;


public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } = Roles.Receptionist;
}


public static class Roles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Housekeeping = "Housekeeping";
    public const string FAndB = "F&B";
    public const string Maintenance = "Maintenance";
}
