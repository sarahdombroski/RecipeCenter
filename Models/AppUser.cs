public class AppUser
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? HashedPassword { get; set; }  // store hashed password
    public List<string> Roles { get; set; } = new();
    public string? ProfilePicturePath { get; set; }
}
