using MyAdmin.Core.Dtos;

namespace MyAdmin.Service.Helpers;

/// <summary>
/// 仅需 JWT 认证的基础自助模块（文献大厅、个人文献中心）：
/// 不参与角色权限配置树勾选，但动态菜单接口须向所有合法用户放行。
/// </summary>
public static class JwtBaselineMenuHelper
{
    public static readonly string[] BaselineRootTitles = { "文献大厅", "个人文献中心" };

    /// <summary>
    /// 收集指定根菜单及其全部子孙节点 Id（含按钮 MenuType = 2）。
    /// </summary>
    public static HashSet<int> CollectSubtreeIds(IReadOnlyList<MenuTreeDto> flatMenus, IEnumerable<string> rootTitles)
    {
        var rootTitleSet = rootTitles.ToHashSet(StringComparer.Ordinal);
        var rootIds = flatMenus
            .Where(m => rootTitleSet.Contains(m.Title))
            .Select(m => m.Id)
            .ToHashSet();

        var subtreeIds = new HashSet<int>(rootIds);
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var menu in flatMenus)
            {
                if (menu.ParentId.HasValue
                    && subtreeIds.Contains(menu.ParentId.Value)
                    && subtreeIds.Add(menu.Id))
                {
                    changed = true;
                }
            }
        }

        return subtreeIds;
    }

    public static List<MenuTreeDto> ExcludeFromPermissionTree(IReadOnlyList<MenuTreeDto> flatMenus)
    {
        var excludedIds = CollectSubtreeIds(flatMenus, BaselineRootTitles);
        return flatMenus.Where(m => !excludedIds.Contains(m.Id)).ToList();
    }

    public static List<int> FilterCheckedMenuIds(IReadOnlyList<MenuTreeDto> flatMenus, IEnumerable<int> checkedMenuIds)
    {
        var excludedIds = CollectSubtreeIds(flatMenus, BaselineRootTitles);
        return checkedMenuIds.Where(id => !excludedIds.Contains(id)).ToList();
    }
}
