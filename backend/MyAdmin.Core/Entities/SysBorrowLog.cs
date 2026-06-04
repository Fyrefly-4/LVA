namespace MyAdmin.Core.Entities;

public class SysBorrowLog
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public DateTime BorrowTime { get; set; }
    public DateTime ReturnTime { get; set; }
    public DateTime? ActualReturnTime { get; set; }
    public byte LogStatus { get; set; }

    public SysBook Book { get; set; } = null!;
    public SysUser User { get; set; } = null!;
}
