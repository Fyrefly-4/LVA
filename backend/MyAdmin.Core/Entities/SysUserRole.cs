namespace MyAdmin.Core.Entities;

public class SysUserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public SysUser User { get; set; } = null!;
    public SysRole Role { get; set; } = null!;
}
