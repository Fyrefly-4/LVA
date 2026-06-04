using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Entities;

namespace MyAdmin.Infrastructure;

public static class DbInitializer
{
    /// <summary>
    /// 数据库种子初始化方法
    /// 功能：物理删除旧库、重新建库、注入完整的 RBAC 权限数据（角色、菜单、按钮、用户及多对多关联）
    /// 
    /// ⚠️⚠️⚠️ 重要警告 ⚠️⚠️⚠️
    /// 以下两行代码会【物理删除】当前 SQL Server 中的整个数据库，然后重新创建空库。
    /// 仅在开发环境首次启动时使用！
    /// 
    /// 【操作流程】
    /// 1. 保持下方两行代码取消注释状态
    /// 2. 启动项目，等待控制台输出 "Database seeded successfully." 确认数据注入完成
    /// 3. 立即手动注释掉 EnsureDeletedAsync() 和 EnsureCreatedAsync() 这两行
    /// 4. 后续重启项目时，这两行必须保持注释状态，否则每次启动都会清空全部数据！
    /// </summary>
    public static async Task SeedAsync(MyAdminDbContext context)
    {
        // ==================== 【危险操作区 - 已注释，如需重新洗牌请临时取消注释】 ====================
        // 物理抹去旧库并重新建库，使所有 IDENTITY 自增主键从 1 开始，彻底清除脏数据和碎片
        // await context.Database.EnsureDeletedAsync();
        // await context.Database.EnsureCreatedAsync();
        // ==================== 【危险操作区结束】 ====================

        // ==================== 数据存在性检查 ====================
        // 如果数据库中已有角色数据，说明种子数据已注入过，执行增量更新逻辑
        if (await context.SysRoles.AnyAsync())
        {
            Console.WriteLine("ℹ️  Database already seeded. Running incremental update...");
            await ApplyIncrementalUpdatesAsync(context);
            return;
        }

        // ==================== A. 构建菜单权限树 (SysMenu) ====================
        // 利用 EF Core 对象上下文生命周期，先插入父级并 SaveChanges 拿到真实 Id，再动态赋值给子级的 ParentId
        // 严禁硬编码 Id，确保自关联外键 100% 畅通

        // A1. 目录级菜单（MenuType = 0）- 系统管理
        // 根节点 ParentId 必须为 null（不可为 0），因为 SysMenu 表存在自关联外键约束
        // ParentId = 0 会触发 FK_SysMenu_SysMenu_ParentId 冲突（无 Id=0 的行）
        // ParentId = null 在 FK 约束中被允许（NULL 不参与外键校验）
        var systemMenu = new SysMenu
        {
            Title = "系统管理",
            Path = "/system",
            Component = "Layout",
            ParentId = null,
            MenuType = 0,
            Icon = "Setting",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(systemMenu);
        await context.SaveChangesAsync(); // 获取 systemMenu.Id（数据库生成的真实自增 Id）

        // A2. 菜单级（MenuType = 1）- 用户管理（父级为系统管理）
        var userManageMenu = new SysMenu
        {
            Title = "用户管理",
            Path = "user",
            Component = "views/user/index.vue",
            ParentId = systemMenu.Id, // 动态引用父级真实 Id
            MenuType = 1,
            Icon = "User",
            PermCode = "system:user:list",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(userManageMenu);
        await context.SaveChangesAsync(); // 获取 userManageMenu.Id

        // A3. 菜单级（MenuType = 1）- 角色管理（父级为系统管理）
        var roleManageMenu = new SysMenu
        {
            Title = "角色管理",
            Path = "role",
            Component = "views/role/index.vue",
            ParentId = systemMenu.Id, // 动态引用父级真实 Id
            MenuType = 1,
            Icon = "UserFilled",
            PermCode = "system:role:list",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(roleManageMenu);
        await context.SaveChangesAsync(); // 获取 roleManageMenu.Id

        // A4. 按钮级细粒度权限（MenuType = 2）- 用户管理子级
        var userCreateBtn = new SysMenu
        {
            Title = "用户新增",
            Path = "",
            Component = "",
            ParentId = userManageMenu.Id, // 动态引用父级真实 Id
            MenuType = 2,
            Icon = "",
            PermCode = "system:user:create",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var userDeleteBtn = new SysMenu
        {
            Title = "用户删除",
            Path = "",
            Component = "",
            ParentId = userManageMenu.Id, // 动态引用父级真实 Id
            MenuType = 2,
            Icon = "",
            PermCode = "system:user:delete",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(userCreateBtn, userDeleteBtn);
        await context.SaveChangesAsync(); // 获取两个按钮的 Id

        // A5. 按钮级细粒度权限（MenuType = 2）- 角色管理子级
        var roleCreateBtn = new SysMenu
        {
            Title = "角色新增",
            Path = "",
            Component = "",
            ParentId = roleManageMenu.Id, // 动态引用父级真实 Id
            MenuType = 2,
            Icon = "",
            PermCode = "system:role:create",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var roleDeleteBtn = new SysMenu
        {
            Title = "角色删除",
            Path = "",
            Component = "",
            ParentId = roleManageMenu.Id, // 动态引用父级真实 Id
            MenuType = 2,
            Icon = "",
            PermCode = "system:role:delete",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(roleCreateBtn, roleDeleteBtn);
        await context.SaveChangesAsync(); // 获取两个按钮的 Id

        // A6. 按钮级细粒度权限（MenuType = 2）- 用户管理 → 分配角色
        var assignRoleBtn = new SysMenu
        {
            Title = "分配角色",
            Path = "",
            Component = "",
            ParentId = userManageMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:user:assignRole",
            Sort = 3,
            Status = 1,
            CreateTime = DateTime.Now
        };

        // A7. 按钮级细粒度权限（MenuType = 2）- 角色管理 → 分配权限
        var assignPermBtn = new SysMenu
        {
            Title = "分配权限",
            Path = "",
            Component = "",
            ParentId = roleManageMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:role:assignPerm",
            Sort = 3,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(assignRoleBtn, assignPermBtn);
        await context.SaveChangesAsync(); // 获取两个按钮的 Id

        // A8. 业务中台目录与文献管理/借阅日志菜单及按钮权限
        var businessMenu = new SysMenu
        {
            Title = "业务中台",
            Path = "/business",
            Component = "Layout",
            ParentId = null,
            MenuType = 0,
            Icon = "Management",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(businessMenu);
        await context.SaveChangesAsync();

        var knowledgeMenu = new SysMenu
        {
            Title = "文献管理",
            Path = "knowledge",
            Component = "views/knowledge/book.vue",
            ParentId = businessMenu.Id,
            MenuType = 1,
            Icon = "Reading",
            PermCode = "system:knowledge:bookList",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(knowledgeMenu);
        await context.SaveChangesAsync();

        var knowledgeBorrowBtn = new SysMenu
        {
            Title = "批量指派",
            Path = "",
            Component = "",
            ParentId = knowledgeMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:borrow",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var knowledgeReturnBtn = new SysMenu
        {
            Title = "归还入库",
            Path = "",
            Component = "",
            ParentId = knowledgeMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:return",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(knowledgeBorrowBtn, knowledgeReturnBtn);
        await context.SaveChangesAsync();

        var knowledgeCreateBtn = new SysMenu
        {
            Title = "新增文献",
            Path = "",
            Component = "",
            ParentId = knowledgeMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:create",
            Sort = 3,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var knowledgeEditBtn = new SysMenu
        {
            Title = "编辑文献",
            Path = "",
            Component = "",
            ParentId = knowledgeMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:edit",
            Sort = 4,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var knowledgeDeleteBtn = new SysMenu
        {
            Title = "删除文献",
            Path = "",
            Component = "",
            ParentId = knowledgeMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:delete",
            Sort = 5,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(knowledgeCreateBtn, knowledgeEditBtn, knowledgeDeleteBtn);
        await context.SaveChangesAsync();

        // A9. 菜单级（MenuType = 1）- 借阅日志（父级为业务中台）
        var borrowLogMenu = new SysMenu
        {
            Title = "借阅日志",
            Path = "borrow-log",
            Component = "views/knowledge/borrow-log.vue",
            ParentId = businessMenu.Id,
            MenuType = 1,
            Icon = "Notebook",
            PermCode = "system:borrow:list",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(borrowLogMenu);
        await context.SaveChangesAsync();

        // 收集全部 17 个菜单/按钮的 Id，用于后续角色绑定
        var allMenuIds = new List<int>
        {
            systemMenu.Id,
            userManageMenu.Id,
            roleManageMenu.Id,
            userCreateBtn.Id,
            userDeleteBtn.Id,
            assignRoleBtn.Id,
            roleCreateBtn.Id,
            roleDeleteBtn.Id,
            assignPermBtn.Id,
            businessMenu.Id,
            knowledgeMenu.Id,
            knowledgeBorrowBtn.Id,
            knowledgeReturnBtn.Id,
            knowledgeCreateBtn.Id,
            knowledgeEditBtn.Id,
            knowledgeDeleteBtn.Id,
            borrowLogMenu.Id
        };

        // ==================== B. 初始角色 (SysRole) ====================
        var adminRole = new SysRole
        {
            RoleName = "管理员",
            RoleCode = "admin",
            Description = "超级管理员",
            CreateTime = DateTime.Now
        };

        var userRole = new SysRole
        {
            RoleName = "普通用户",
            RoleCode = "user",
            Description = "普通用户",
            CreateTime = DateTime.Now
        };
        context.SysRoles.AddRange(adminRole, userRole);
        await context.SaveChangesAsync(); // 获取角色的真实自增 Id

        // ==================== C. 初始超级管理员用户 (SysUser) ====================
        var adminUser = new SysUser
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), // BCrypt 哈希加密
            Nickname = "超级管理员",
            Email = "admin@example.com",
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysUsers.Add(adminUser);
        await context.SaveChangesAsync(); // 获取用户的真实自增 Id

        // ==================== D. 全链路多对多绑定 ====================

        // D1. 用户角色关联 (SysUserRole) - 将 admin 用户绑定到 admin 角色
        context.SysUserRoles.Add(new SysUserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });
        await context.SaveChangesAsync();

        // D2. 角色菜单关联 (SysRoleMenu) - 将 admin 角色与全部 13 个菜单/按钮批量绑定
        // 确保管理员拥有全栈动态菜单渲染权与细粒度接口操作权（[HasPermission] 校验）
        var adminRoleMenus = allMenuIds.Select(menuId => new SysRoleMenu
        {
            RoleId = adminRole.Id,
            MenuId = menuId
        }).ToList();
        context.SysRoleMenus.AddRange(adminRoleMenus);
        await context.SaveChangesAsync();

        // ==================== 日志输出 ====================
        Console.WriteLine("✅ Database seeded successfully.");
        Console.WriteLine($"   - Admin User: admin / password123");
        Console.WriteLine($"   - Admin Role: {adminRole.RoleCode} (Id={adminRole.Id})");
        Console.WriteLine($"   - User Role: {userRole.RoleCode} (Id={userRole.Id})");
        Console.WriteLine($"   - Menus: {allMenuIds.Count} items injected");
        Console.WriteLine($"   - Role-Menu Bindings: {adminRoleMenus.Count} records created");
    }

    /// <summary>
    /// 增量更新逻辑：安全地追加或修改菜单/权限数据，不覆盖已有记录。
    /// 
    /// 使用场景：
    /// - 前端重构后菜单路径变更（如 Component 路径调整）
    /// - 新增菜单/按钮节点
    /// - 自动将新菜单绑定到 admin 角色
    /// 
    /// 幂等性：多次执行不会产生重复数据，基于 PermCode 唯一性判断。
    /// </summary>
    private static async Task ApplyIncrementalUpdatesAsync(MyAdminDbContext context)
    {
        var updated = false;

        // 1. 更新文献管理菜单的 Title 和 Component（如果还是旧值）
        var knowledgeMenu = await context.SysMenus
            .FirstOrDefaultAsync(m => m.PermCode == "system:knowledge:bookList");
        if (knowledgeMenu != null)
        {
            if (knowledgeMenu.Title == "知识库流转" || knowledgeMenu.Component == "views/knowledge/index.vue")
            {
                knowledgeMenu.Title = "文献管理";
                knowledgeMenu.Component = "views/knowledge/book.vue";
                updated = true;
                Console.WriteLine("   - Updated: 文献管理 menu (Title/Component)");
            }
        }

        // 2. 检查并插入借阅日志菜单节点（基于 PermCode 唯一性）
        var borrowLogExists = await context.SysMenus
            .AnyAsync(m => m.PermCode == "system:borrow:list");
        if (!borrowLogExists)
        {
            // 查找父级"业务中台"目录
            var businessMenu = await context.SysMenus
                .FirstOrDefaultAsync(m => m.PermCode == null && m.MenuType == 0 && m.Path == "/business");
            if (businessMenu == null)
            {
                Console.WriteLine("   ⚠️  Skipped: 业务中台 (business menu) not found");
            }
            else
            {
                var borrowLogMenu = new SysMenu
                {
                    Title = "借阅日志",
                    Path = "borrow-log",
                    Component = "views/knowledge/borrow-log.vue",
                    ParentId = businessMenu.Id,
                    MenuType = 1,
                    Icon = "Notebook",
                    PermCode = "system:borrow:list",
                    Sort = 2,
                    Status = 1,
                    CreateTime = DateTime.Now
                };
                context.SysMenus.Add(borrowLogMenu);
                await context.SaveChangesAsync();

                // 3. 将新菜单绑定到 admin 角色
                var adminRole = await context.SysRoles
                    .FirstOrDefaultAsync(r => r.RoleCode == "admin");
                if (adminRole != null)
                {
                    var alreadyBound = await context.SysRoleMenus
                        .AnyAsync(rm => rm.RoleId == adminRole.Id && rm.MenuId == borrowLogMenu.Id);
                    if (!alreadyBound)
                    {
                        context.SysRoleMenus.Add(new SysRoleMenu
                        {
                            RoleId = adminRole.Id,
                            MenuId = borrowLogMenu.Id
                        });
                    }
                }

                updated = true;
                Console.WriteLine("   - Inserted: 借阅日志 menu (system:borrow:list)");
                Console.WriteLine("   - Bound to: admin role");
            }
        }

        if (updated)
        {
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Incremental update completed.");
        }
        else
        {
            Console.WriteLine("   - All data up-to-date, no changes needed.");
        }
    }
}
