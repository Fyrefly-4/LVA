namespace MyAdmin.Core.Entities;

public class SysRole
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreateTime { get; set; }

    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
    public ICollection<SysRoleMenu> RoleMenus { get; set; } = new List<SysRoleMenu>();
}
