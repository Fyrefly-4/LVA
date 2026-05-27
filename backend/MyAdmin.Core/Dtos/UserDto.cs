namespace MyAdmin.Core.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? Email { get; set; }
    public byte Status { get; set; }
    public List<string> Roles { get; set; } = new();
    public string CreateTime { get; set; } = string.Empty;
}
