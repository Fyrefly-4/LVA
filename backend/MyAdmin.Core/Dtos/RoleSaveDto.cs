namespace MyAdmin.Core.Dtos;

public class RoleSaveDto
{
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string? Description { get; set; }
}
