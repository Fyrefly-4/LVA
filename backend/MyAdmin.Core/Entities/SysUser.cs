namespace MyAdmin.Core.Entities;

public class SysUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? Email { get; set; }
    public byte Status { get; set; } = 1;
    public DateTime CreateTime { get; set; }

    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
    public ICollection<SysBorrowLog> BorrowLogs { get; set; } = new List<SysBorrowLog>();
}
