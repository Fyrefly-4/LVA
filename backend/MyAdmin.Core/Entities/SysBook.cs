namespace MyAdmin.Core.Entities;

public class SysBook
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public byte Status { get; set; } = 1;
    public DateTime CreateTime { get; set; }

    public ICollection<SysBorrowLog> BorrowLogs { get; set; } = new List<SysBorrowLog>();
}
