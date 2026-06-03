using MyAdmin.Core.Dtos;

namespace MyAdmin.Service.Helpers;

/// <summary>
/// 将扁平菜单列表递归组装为目录 -> 菜单 -> 按钮树形结构。
/// </summary>
public static class MenuTreeBuilder
{
    public static List<MenuTreeDto> Build(IEnumerable<MenuTreeDto> flatMenus, int? parentId = null)
    {
        return flatMenus
            .Where(menu => menu.ParentId == parentId)
            .OrderBy(menu => menu.Sort)
            .ThenBy(menu => menu.Id)
            .Select(menu => new MenuTreeDto
            {
                Id = menu.Id,
                ParentId = menu.ParentId,
                Title = menu.Title,
                Path = menu.Path,
                Component = menu.Component,
                PermCode = menu.PermCode,
                MenuType = menu.MenuType,
                Icon = menu.Icon,
                Sort = menu.Sort,
                Children = Build(flatMenus, menu.Id)
            })
            .ToList();
    }
}
