namespace MyAdmin.Core.Dtos;

public class KnowledgeBookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public byte Status { get; set; }
    public string CreateTime { get; set; } = string.Empty;
}

public class KnowledgeBorrowRequest
{
    public int UserId { get; set; }
    public List<int> BookIds { get; set; } = new();
    public int BorrowDays { get; set; }
}

public class KnowledgeBookSaveRequest
{
    public string Title { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public byte Status { get; set; } = 1;
}

public class KnowledgeBorrowLogDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string BorrowTime { get; set; } = string.Empty;
    public string ReturnTime { get; set; } = string.Empty;
    public string? ActualReturnTime { get; set; }
    public byte LogStatus { get; set; }
}
