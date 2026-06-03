namespace MyAdmin.Core.Dtos;

/// <summary>
/// 角色权限分配页数据：全量菜单树 + 当前角色已勾选的 MenuId 集合。
/// </summary>
public class RolePermissionDto
{
    /// <summary>
    /// 系统中 Status = 1 的目录、菜单、按钮完整树（按 Sort 升序递归组装）。
    /// </summary>
    public List<MenuTreeDto> AllMenus { get; set; } = new();

    /// <summary>
    /// 当前角色已绑定的 MenuId，供前端 el-tree setCheckedKeys 回显。
    /// </summary>
    public List<int> CheckedMenuIds { get; set; } = new();
}
