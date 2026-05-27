namespace MyAdmin.Core.Dtos;

public class UserSaveDto
{
    public string? Username { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Email { get; set; }
    public byte Status { get; set; } = 1;
    public List<int> RoleIds { get; set; } = new();
}
