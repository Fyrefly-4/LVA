namespace MyAdmin.Core.Entities;

public class SysRoleMenu
{
    public int RoleId { get; set; }
    public int MenuId { get; set; }

    public SysRole Role { get; set; } = null!;
    public SysMenu Menu { get; set; } = null!;
}
