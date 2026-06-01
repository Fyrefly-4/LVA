namespace MyAdmin.Core.Dtos;

public class MenuTreeDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Component { get; set; }
    public string? PermCode { get; set; }
    public byte MenuType { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public List<MenuTreeDto> Children { get; set; } = new();
}
