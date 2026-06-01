namespace MyAdmin.Core.Entities;

public class SysMenu
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
    public byte Status { get; set; } = 1;
    public DateTime CreateTime { get; set; }

    public SysMenu? Parent { get; set; }
    public ICollection<SysMenu> Children { get; set; } = new List<SysMenu>();
    public ICollection<SysRoleMenu> RoleMenus { get; set; } = new List<SysRoleMenu>();
}
