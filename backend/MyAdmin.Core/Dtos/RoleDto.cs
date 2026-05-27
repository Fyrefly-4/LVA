namespace MyAdmin.Core.Dtos;

public class RoleDto
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string? Description { get; set; }
}
