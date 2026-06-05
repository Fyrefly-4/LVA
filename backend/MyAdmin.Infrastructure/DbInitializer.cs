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

        // A8. 业务中台目录（MenuType = 0）
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

        // A9. 文献大厅（MenuType = 1，无 PermCode，仅需 JWT 认证）
        var hallMenu = new SysMenu
        {
            Title = "文献大厅",
            Path = "hall",
            Component = "views/knowledge/hall.vue",
            ParentId = businessMenu.Id,
            MenuType = 1,
            Icon = "Collection",
            PermCode = "",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(hallMenu);
        await context.SaveChangesAsync();

        var browseBtn = new SysMenu
        {
            Title = "浏览文献",
            Path = "",
            Component = "",
            ParentId = hallMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var hallBorrowBtn = new SysMenu
        {
            Title = "借阅文献",
            Path = "",
            Component = "",
            ParentId = hallMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(browseBtn, hallBorrowBtn);
        await context.SaveChangesAsync();

        // A10. 个人文献中心（MenuType = 1，无 PermCode，仅需 JWT 认证）
        var personalCenter = new SysMenu
        {
            Title = "个人文献中心",
            Path = "personal",
            Component = "views/knowledge/personal.vue",
            ParentId = businessMenu.Id,
            MenuType = 1,
            Icon = "User",
            PermCode = "",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(personalCenter);
        await context.SaveChangesAsync();

        var myBorrowBtn = new SysMenu
        {
            Title = "我的借阅",
            Path = "",
            Component = "",
            ParentId = personalCenter.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var myHistoryBtn = new SysMenu
        {
            Title = "我的历史",
            Path = "",
            Component = "",
            ParentId = personalCenter.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var selfReturnBtn = new SysMenu
        {
            Title = "自助归还",
            Path = "",
            Component = "",
            ParentId = personalCenter.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "",
            Sort = 3,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(myBorrowBtn, myHistoryBtn, selfReturnBtn);
        await context.SaveChangesAsync();

        // A11. 文献资产管理（MenuType = 1，管理员专属）+ 3 个按钮（MenuType = 2）
        var bookMenu = new SysMenu
        {
            Title = "文献资产管理",
            Path = "knowledge",
            Component = "views/knowledge/book.vue",
            ParentId = businessMenu.Id,
            MenuType = 1,
            Icon = "Reading",
            PermCode = "system:knowledge:bookList",
            Sort = 3,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(bookMenu);
        await context.SaveChangesAsync();

        var bookCreateBtn = new SysMenu
        {
            Title = "新增文献",
            Path = "",
            Component = "",
            ParentId = bookMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:create",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var bookEditBtn = new SysMenu
        {
            Title = "编辑文献",
            Path = "",
            Component = "",
            ParentId = bookMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:edit",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var bookDeleteBtn = new SysMenu
        {
            Title = "删除文献",
            Path = "",
            Component = "",
            ParentId = bookMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:delete",
            Sort = 3,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(bookCreateBtn, bookEditBtn, bookDeleteBtn);
        await context.SaveChangesAsync();

        // A12. 流转审计日志（MenuType = 1，管理员专属）- 挂于业务中台下
        var auditLogMenu = new SysMenu
        {
            Title = "流转审计日志",
            Path = "audit-log",
            Component = "views/knowledge/audit-log.vue",
            ParentId = businessMenu.Id,
            MenuType = 1,
            Icon = "Notebook",
            PermCode = "system:knowledge:adminLog",
            Sort = 4,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.Add(auditLogMenu);
        await context.SaveChangesAsync();

        var auditBorrowBtn = new SysMenu
        {
            Title = "批量指派借阅",
            Path = "",
            Component = "",
            ParentId = auditLogMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:borrow",
            Sort = 1,
            Status = 1,
            CreateTime = DateTime.Now
        };

        var auditReturnBtn = new SysMenu
        {
            Title = "管理员归还入库",
            Path = "",
            Component = "",
            ParentId = auditLogMenu.Id,
            MenuType = 2,
            Icon = "",
            PermCode = "system:knowledge:return",
            Sort = 2,
            Status = 1,
            CreateTime = DateTime.Now
        };
        context.SysMenus.AddRange(auditBorrowBtn, auditReturnBtn);
        await context.SaveChangesAsync();

        // 收集全部 24 个菜单/按钮的 Id，用于后续角色绑定
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
            hallMenu.Id,
            browseBtn.Id,
            hallBorrowBtn.Id,
            personalCenter.Id,
            myBorrowBtn.Id,
            myHistoryBtn.Id,
            selfReturnBtn.Id,
            bookMenu.Id,
            bookCreateBtn.Id,
            bookEditBtn.Id,
            bookDeleteBtn.Id,
            auditLogMenu.Id,
            auditBorrowBtn.Id,
            auditReturnBtn.Id
        };

        // 普通用户 JWT 可见菜单（文献大厅 + 个人文献中心）
        var userMenuIds = new List<int>
        {
            businessMenu.Id,
            hallMenu.Id,
            browseBtn.Id,
            hallBorrowBtn.Id,
            personalCenter.Id,
            myBorrowBtn.Id,
            myHistoryBtn.Id,
            selfReturnBtn.Id
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

        // D2. 角色菜单关联 (SysRoleMenu) - 将 admin 角色与全部 24 个菜单/按钮批量绑定
        // 确保管理员拥有全栈动态菜单渲染权与细粒度接口操作权（[HasPermission] 校验）
        var adminRoleMenus = allMenuIds.Select(menuId => new SysRoleMenu
        {
            RoleId = adminRole.Id,
            MenuId = menuId
        }).ToList();
        context.SysRoleMenus.AddRange(adminRoleMenus);

        // D3. 角色菜单关联 (SysRoleMenu) - 将 user 角色与 JWT 可见菜单绑定
        var userRoleMenus = userMenuIds.Select(menuId => new SysRoleMenu
        {
            RoleId = userRole.Id,
            MenuId = menuId
        }).ToList();
        context.SysRoleMenus.AddRange(userRoleMenus);
        await context.SaveChangesAsync();

        // ==================== 日志输出 ====================
        Console.WriteLine("✅ Database seeded successfully.");
        Console.WriteLine($"   - Admin User: admin / password123");
        Console.WriteLine($"   - Admin Role: {adminRole.RoleCode} (Id={adminRole.Id})");
        Console.WriteLine($"   - User Role: {userRole.RoleCode} (Id={userRole.Id})");
        Console.WriteLine($"   - Menus: {allMenuIds.Count} items injected");
        Console.WriteLine($"   - Admin Role-Menu Bindings: {adminRoleMenus.Count} records created");
        Console.WriteLine($"   - User Role-Menu Bindings: {userRoleMenus.Count} records created");
    }

    /// <summary>
    /// 增量更新逻辑：安全地追加或修改菜单/权限数据，不覆盖已有记录。
    /// 
    /// 使用场景：
    /// - 权限树结构重构（父子关系迁移）
    /// - 新增菜单/按钮节点
    /// - 自动将新菜单绑定到 admin 角色
    /// 
    /// 幂等性：多次执行不会产生重复数据，基于 PermCode 唯一性判断。
    /// </summary>
    private static async Task ApplyIncrementalUpdatesAsync(MyAdminDbContext context)
    {
        var updated = false;

        var businessMenu = await context.SysMenus
            .FirstOrDefaultAsync(m => m.PermCode == null && m.MenuType == 0 && m.Path == "/business");

        // 1. 更新文献资产管理菜单的 Title / Sort（如果还是旧值）
        var bookMenu = await context.SysMenus
            .FirstOrDefaultAsync(m => m.PermCode == "system:knowledge:bookList");
        if (bookMenu != null)
        {
            if (bookMenu.Title != "文献资产管理")
            {
                bookMenu.Title = "文献资产管理";
                updated = true;
                Console.WriteLine("   - Updated: 文献资产管理 menu Title");
            }

            if (bookMenu.Sort != 3)
            {
                bookMenu.Sort = 3;
                updated = true;
                Console.WriteLine("   - Updated: 文献资产管理 menu Sort → 3");
            }
        }

        // 2. 清理旧的借阅日志菜单（system:borrow:list）
        var oldBorrowLog = await context.SysMenus
            .FirstOrDefaultAsync(m => m.PermCode == "system:borrow:list");
        if (oldBorrowLog != null)
        {
            // 清理关联的 SysRoleMenu 绑定
            await context.SysRoleMenus
                .Where(rm => rm.MenuId == oldBorrowLog.Id)
                .ExecuteDeleteAsync();
            context.SysMenus.Remove(oldBorrowLog);
            updated = true;
            Console.WriteLine("   - Removed: 旧借阅日志 menu (system:borrow:list)");
        }

        // 3. 查找或创建个人文献中心菜单
        var personalCenter = await context.SysMenus
            .FirstOrDefaultAsync(m => m.Title == "个人文献中心" && m.MenuType == 1);
        if (personalCenter == null && businessMenu != null)
        {
            personalCenter = new SysMenu
            {
                Title = "个人文献中心",
                Path = "personal",
                Component = "views/knowledge/personal.vue",
                ParentId = businessMenu.Id,
                MenuType = 1,
                Icon = "User",
                PermCode = "",
                Sort = 2,
                Status = 1,
                CreateTime = DateTime.Now
            };
            context.SysMenus.Add(personalCenter);
            await context.SaveChangesAsync();
            updated = true;
            Console.WriteLine("   - Inserted: 个人文献中心 menu");
        }

        // 4. 查找或创建流转审计日志菜单（system:knowledge:adminLog）
        var auditLogMenu = await context.SysMenus
            .FirstOrDefaultAsync(m => m.PermCode == "system:knowledge:adminLog");
        if (auditLogMenu == null && businessMenu != null)
        {
            auditLogMenu = new SysMenu
            {
                Title = "流转审计日志",
                Path = "audit-log",
                Component = "views/knowledge/audit-log.vue",
                ParentId = businessMenu.Id,
                MenuType = 1,
                Icon = "Notebook",
                PermCode = "system:knowledge:adminLog",
                Sort = 4,
                Status = 1,
                CreateTime = DateTime.Now
            };
            context.SysMenus.Add(auditLogMenu);
            await context.SaveChangesAsync();
            updated = true;
            Console.WriteLine("   - Inserted: 流转审计日志 menu (system:knowledge:adminLog)");
        }

        // 5. 迁移批量指派和归还入库按钮到流转审计日志下
        if (auditLogMenu != null)
        {
            var borrowBtn = await context.SysMenus
                .FirstOrDefaultAsync(m => m.PermCode == "system:knowledge:borrow");
            if (borrowBtn != null)
            {
                if (borrowBtn.Title != "批量指派借阅" || borrowBtn.ParentId != auditLogMenu.Id)
                {
                    borrowBtn.Title = "批量指派借阅";
                    borrowBtn.ParentId = auditLogMenu.Id;
                    borrowBtn.Sort = 1;
                    updated = true;
                    Console.WriteLine("   - Migrated: 批量指派借阅 → 流转审计日志");
                }
            }

            var returnBtn = await context.SysMenus
                .FirstOrDefaultAsync(m => m.PermCode == "system:knowledge:return");
            if (returnBtn != null)
            {
                if (returnBtn.Title != "管理员归还入库" || returnBtn.ParentId != auditLogMenu.Id)
                {
                    returnBtn.Title = "管理员归还入库";
                    returnBtn.ParentId = auditLogMenu.Id;
                    returnBtn.Sort = 2;
                    updated = true;
                    Console.WriteLine("   - Migrated: 管理员归还入库 → 流转审计日志");
                }
            }
        }

        // 6. 个人文献中心子按钮：重命名旧节点或插入新节点
        if (personalCenter != null)
        {
            var myBorrowBtn = await context.SysMenus
                .FirstOrDefaultAsync(m => m.ParentId == personalCenter.Id
                    && (m.Title == "我的借阅" || m.Title == "自助借阅申请"));
            if (myBorrowBtn != null)
            {
                if (myBorrowBtn.Title != "我的借阅" || myBorrowBtn.Sort != 1)
                {
                    myBorrowBtn.Title = "我的借阅";
                    myBorrowBtn.Sort = 1;
                    updated = true;
                    Console.WriteLine("   - Renamed: 自助借阅申请 → 我的借阅");
                }
            }
            else
            {
                context.SysMenus.Add(new SysMenu
                {
                    Title = "我的借阅",
                    Path = "",
                    Component = "",
                    ParentId = personalCenter.Id,
                    MenuType = 2,
                    Icon = "",
                    PermCode = "",
                    Sort = 1,
                    Status = 1,
                    CreateTime = DateTime.Now
                });
                updated = true;
                Console.WriteLine("   - Inserted: 我的借阅 button");
            }

            var myHistoryExists = await context.SysMenus
                .AnyAsync(m => m.Title == "我的历史" && m.ParentId == personalCenter.Id);
            if (!myHistoryExists)
            {
                context.SysMenus.Add(new SysMenu
                {
                    Title = "我的历史",
                    Path = "",
                    Component = "",
                    ParentId = personalCenter.Id,
                    MenuType = 2,
                    Icon = "",
                    PermCode = "",
                    Sort = 2,
                    Status = 1,
                    CreateTime = DateTime.Now
                });
                updated = true;
                Console.WriteLine("   - Inserted: 我的历史 button");
            }

            var selfReturnBtn = await context.SysMenus
                .FirstOrDefaultAsync(m => m.ParentId == personalCenter.Id
                    && (m.Title == "自助归还" || m.Title == "自助归还核销"));
            if (selfReturnBtn != null)
            {
                if (selfReturnBtn.Title != "自助归还" || selfReturnBtn.Sort != 3)
                {
                    selfReturnBtn.Title = "自助归还";
                    selfReturnBtn.Sort = 3;
                    updated = true;
                    Console.WriteLine("   - Renamed: 自助归还核销 → 自助归还");
                }
            }
            else
            {
                context.SysMenus.Add(new SysMenu
                {
                    Title = "自助归还",
                    Path = "",
                    Component = "",
                    ParentId = personalCenter.Id,
                    MenuType = 2,
                    Icon = "",
                    PermCode = "",
                    Sort = 3,
                    Status = 1,
                    CreateTime = DateTime.Now
                });
                updated = true;
                Console.WriteLine("   - Inserted: 自助归还 button");
            }
        }

        // 7. Knowledge 菜单树重组：文献大厅 + 流转审计日志提升层级
        SysMenu? hallMenu = null;
        if (businessMenu != null)
        {
            hallMenu = await context.SysMenus
                .FirstOrDefaultAsync(m => m.Title == "文献大厅" && m.MenuType == 1 && m.ParentId == businessMenu.Id);
            if (hallMenu == null)
            {
                hallMenu = new SysMenu
                {
                    Title = "文献大厅",
                    Path = "hall",
                    Component = "views/knowledge/hall.vue",
                    ParentId = businessMenu.Id,
                    MenuType = 1,
                    Icon = "Collection",
                    PermCode = "",
                    Sort = 1,
                    Status = 1,
                    CreateTime = DateTime.Now
                };
                context.SysMenus.Add(hallMenu);
                await context.SaveChangesAsync();
                updated = true;
                Console.WriteLine("   - Inserted: 文献大厅 menu");
            }

            var browseExists = await context.SysMenus
                .AnyAsync(m => m.Title == "浏览文献" && m.ParentId == hallMenu.Id);
            if (!browseExists)
            {
                context.SysMenus.Add(new SysMenu
                {
                    Title = "浏览文献",
                    Path = "",
                    Component = "",
                    ParentId = hallMenu.Id,
                    MenuType = 2,
                    Icon = "",
                    PermCode = "",
                    Sort = 1,
                    Status = 1,
                    CreateTime = DateTime.Now
                });
                updated = true;
                Console.WriteLine("   - Inserted: 浏览文献 button");
            }

            var hallBorrowExists = await context.SysMenus
                .AnyAsync(m => m.Title == "借阅文献" && m.ParentId == hallMenu.Id);
            if (!hallBorrowExists)
            {
                context.SysMenus.Add(new SysMenu
                {
                    Title = "借阅文献",
                    Path = "",
                    Component = "",
                    ParentId = hallMenu.Id,
                    MenuType = 2,
                    Icon = "",
                    PermCode = "",
                    Sort = 2,
                    Status = 1,
                    CreateTime = DateTime.Now
                });
                updated = true;
                Console.WriteLine("   - Inserted: 借阅文献 button");
            }
        }

        if (auditLogMenu != null && businessMenu != null)
        {
            if (auditLogMenu.ParentId != businessMenu.Id || auditLogMenu.Sort != 4)
            {
                auditLogMenu.ParentId = businessMenu.Id;
                auditLogMenu.Sort = 4;
                updated = true;
                Console.WriteLine("   - Migrated: 流转审计日志 → 业务中台");
            }
        }

        if (personalCenter != null && personalCenter.Sort != 2)
        {
            personalCenter.Sort = 2;
            updated = true;
            Console.WriteLine("   - Updated: 个人文献中心 menu Sort → 2");
        }

        // 8. 角色菜单绑定：admin 增量 + user 角色 JWT 可见菜单
        // 先落库菜单变更，确保后续按 ParentId 查询能拿到新节点 Id
        if (updated)
        {
            await context.SaveChangesAsync();
        }

        var adminRole = await context.SysRoles
            .FirstOrDefaultAsync(r => r.RoleCode == "admin");
        var userRole = await context.SysRoles
            .FirstOrDefaultAsync(r => r.RoleCode == "user");

        if (adminRole != null)
        {
            var adminMenuIds = new List<int>();
            if (hallMenu != null)
            {
                adminMenuIds.Add(hallMenu.Id);
                var hallChildIds = await context.SysMenus
                    .Where(m => m.ParentId == hallMenu.Id)
                    .Select(m => m.Id)
                    .ToListAsync();
                adminMenuIds.AddRange(hallChildIds);
            }

            if (personalCenter != null)
            {
                adminMenuIds.Add(personalCenter.Id);
                var personalChildIds = await context.SysMenus
                    .Where(m => m.ParentId == personalCenter.Id)
                    .Select(m => m.Id)
                    .ToListAsync();
                adminMenuIds.AddRange(personalChildIds);
            }

            if (auditLogMenu != null)
                adminMenuIds.Add(auditLogMenu.Id);

            foreach (var menuId in adminMenuIds.Distinct())
            {
                var alreadyBound = await context.SysRoleMenus
                    .AnyAsync(rm => rm.RoleId == adminRole.Id && rm.MenuId == menuId);
                if (!alreadyBound)
                {
                    context.SysRoleMenus.Add(new SysRoleMenu
                    {
                        RoleId = adminRole.Id,
                        MenuId = menuId
                    });
                    updated = true;
                    Console.WriteLine($"   - Bound to admin role: menuId={menuId}");
                }
            }
        }

        if (userRole != null && businessMenu != null && hallMenu != null && personalCenter != null)
        {
            var userMenuIds = new List<int> { businessMenu.Id, hallMenu.Id, personalCenter.Id };

            var hallChildIds = await context.SysMenus
                .Where(m => m.ParentId == hallMenu.Id)
                .Select(m => m.Id)
                .ToListAsync();
            userMenuIds.AddRange(hallChildIds);

            var personalChildIds = await context.SysMenus
                .Where(m => m.ParentId == personalCenter.Id)
                .Select(m => m.Id)
                .ToListAsync();
            userMenuIds.AddRange(personalChildIds);

            foreach (var menuId in userMenuIds.Distinct())
            {
                var alreadyBound = await context.SysRoleMenus
                    .AnyAsync(rm => rm.RoleId == userRole.Id && rm.MenuId == menuId);
                if (!alreadyBound)
                {
                    context.SysRoleMenus.Add(new SysRoleMenu
                    {
                        RoleId = userRole.Id,
                        MenuId = menuId
                    });
                    updated = true;
                    Console.WriteLine($"   - Bound to user role: menuId={menuId}");
                }
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
