# ProjectGuide — MyAdmin 后端

> 面向 Agent 的项目理解指南。保留所有业务信息，重组结构以便快速定位。

---

## 1. 项目简介

### 1.1 项目名称

MyAdmin — 基于 RBAC 的后台管理系统后端。

### 1.2 技术栈

| 层级 | 技术 |
|------|------|
| 运行时 | .NET 8.0 |
| Web 框架 | ASP.NET Core (Web API) |
| 认证 | JWT Bearer（`Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.11） |
| ORM | Entity Framework Core 8.0.11（SQL Server） |
| API 文档 | Swashbuckle / Swagger 6.6.2 |
| 密码哈希 | BCrypt |

### 1.3 项目结构

```
backend/
├── MyAdmin.Core/            # 公共实体、DTO、统一响应类型、JWT 配置
├── MyAdmin.Infrastructure/  # EF Core 数据访问层（DbContext、迁移、种子数据）
├── MyAdmin.Service/         # 业务逻辑层（接口 + 实现）
└── MyAdmin.WebApi/          # Web API 层（Controller、Attributes、Program.cs）
```

### 1.4 统一响应格式

所有接口 HTTP 状态码均为 `200`，业务状态通过 `code` 字段区分：

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {}
}
```

| code | 含义 |
|------|------|
| 200  | 成功 |
| 401  | 未认证 |
| 403  | 无权限 |
| 500  | 业务失败 |

### 1.5 权限模型

- **RBAC**：`SysUser` → `SysUserRole` → `SysRole` → `SysRoleMenu` → `SysMenu`
- 菜单分三级：`MenuType = 0`（目录）→ `MenuType = 1`（菜单）→ `MenuType = 2`（按钮）
- 按钮级权限通过 `PermCode` 标识（如 `system:user:create`）
- 接口级鉴权通过 `[HasPermission("permCode")]` 特性实现，从当前用户关联角色的菜单中校验权限码是否存在
- 认证：除登录接口外，其余接口需在请求头携带 `Authorization: Bearer <token>`

---

## 2. 模块说明

### 2.1 认证模块（Auth）

**Controller**: `MyAdmin.WebApi/Controllers/AuthController.cs`
**Service**: `MyAdmin.Service/Implementations/AuthService.cs`

| 接口 | 说明 | 认证 |
|------|------|------|
| `POST /api/auth/login` | 用户登录，返回 JWT token | 不需要 |
| `GET /api/auth/info` | 获取当前用户信息（含角色、权限码） | 需要 |
| `GET /api/auth/menus` | 获取当前用户可访问的菜单树（仅目录+菜单，不含按钮） | 需要 |
| `POST /api/auth/logout` | 退出登录（前端清理 token，后端无状态） | 需要 |

**核心逻辑**：
- `LoginAsync`：验证用户名密码（BCrypt），生成 JWT，包含 NameIdentifier、Name、nickname、Role 声明
- `GetCurrentUserInfoAsync`：通过 `SysUserRole → SysRoleMenu → SysMenu` 汇总权限码，返回 `UserInfoDto`（含 `roles` 与 `permissions`）
- `GetCurrentUserMenusAsync`：查询 `MenuType = 0` 或 `1` 且 `Status = 1` 的菜单，返回扁平列表，由 Controller 递归组装为树

**相关 DTO**：
- `LoginDto`：`username`, `password`
- `LoginResponseDto`：`token`
- `UserInfoDto`：`userId`, `username`, `nickname`, `roles[]`, `permissions[]`
- `MenuTreeDto`：`id`, `parentId`, `title`, `path`, `component`, `permCode`, `menuType`, `icon`, `sort`, `children[]`

---

### 2.2 用户管理模块（User）

**Controller**: `MyAdmin.WebApi/Controllers/UserController.cs`
**Service**: `MyAdmin.Service/Implementations/UserService.cs`

| 接口 | 说明 | 权限码 |
|------|------|--------|
| `GET /api/user/list` | 条件分页获取用户列表 | `system:user:list` |
| `POST /api/user` | 新增用户 | `system:user:create` |
| `PUT /api/user/{id}` | 修改用户（含角色分配） | `system:user:assignRole` |
| `DELETE /api/user/{id}` | 删除单个用户 | `system:user:delete` |
| `POST /api/user/batch-delete` | 批量删除用户 | `system:user:delete` |

**核心逻辑**：
- `GetListAsync`：分页查询，按 `Id ASC` 排序，支持 `keyword` 模糊搜索 `username`/`nickname`/`email`
- `CreateAsync`：校验用户名唯一、RoleIds 有效性，BCrypt 哈希密码，写入用户角色关联
- `UpdateAsync`：修改昵称、邮箱、状态；可选修改密码；同步用户角色关联（先删后插）
- `DeleteAsync` / `BatchDeleteAsync`：删除用户记录

**相关 DTO**：
- `UserDto`：`id`, `username`, `nickname`, `email`, `status`, `roles[]`, `createTime`
- `UserSaveDto`：`username`, `nickname`, `password`, `email`, `status`, `roleIds[]`

---

### 2.3 角色管理模块（Role）

**Controller**: `MyAdmin.WebApi/Controllers/RoleController.cs`
**Service**: `MyAdmin.Service/Implementations/RoleService.cs`

| 接口 | 说明 | 权限码 |
|------|------|--------|
| `GET /api/role/list` | 分页查询角色列表 | `system:role:list` |
| `GET /api/role/all` | 获取全部角色（不分页） | `system:role:list` |
| `POST /api/role` | 新增角色 | `system:role:create` |
| `PUT /api/role/{id}` | 修改角色 | `system:role:list` |
| `DELETE /api/role/{id}` | 删除角色 | `system:role:delete` |
| `GET /api/role/{id}/permissions` | 获取全量菜单树 + 当前角色已勾选 MenuId | `system:role:list` |
| `POST /api/role/{id}/permissions` | 保存角色权限（平铺 `List<int>` menuIds，不包 dto 壳） | `system:role:assignPerm` |

**核心逻辑**：
- `GetListAsync`：分页或全量查询角色
- `CreateAsync` / `UpdateAsync`：校验 `RoleCode` 全局唯一
- `GetRolePermissionsAsync`：查询 `Status = 1` 的全量菜单并递归为 `AllMenus`，查询 `SysRoleMenu` 得到 `CheckedMenuIds`
- `SaveRolePermissionsAsync`：事务内先删后插同步 `SysRoleMenu`，支持空数组清空

**相关 DTO**：
- `RoleDto`：`id`, `roleName`, `roleCode`, `description`
- `RoleSaveDto`：`roleName`, `roleCode`, `description`
- `RolePermissionDto`：`allMenus`（`List<MenuTreeDto>`）, `checkedMenuIds`（`List<int>`）

---

### 2.4 菜单权限模块（Menu）

**实体**: `SysMenu`（`MyAdmin.Core/Entities/`）
**关联表**: `SysRoleMenu`

菜单类型（`MenuType`）：

| 值 | 类型 | 说明 |
|----|------|------|
| 0 | 目录 | 顶级导航，`ParentId = null`，`Component = "Layout"` |
| 1 | 菜单 | 页面路由，含 `Path`、`Component`、`PermCode` |
| 2 | 按钮 | 操作权限，无 `Path`/`Component`，仅含 `PermCode` |

**权限码命名规范**：`system:{module}:{action}`，如 `system:user:create`、`system:knowledge:bookList`

**当前全部 16 条权限树**：

| 层级 | Title | PermCode | MenuType | 父级 |
|------|-------|----------|----------|------|
| 目录 | 系统管理 | - | 0 | `null` |
| 菜单 | 用户管理 | `system:user:list` | 1 | 系统管理 |
| 菜单 | 角色管理 | `system:role:list` | 1 | 系统管理 |
| 按钮 | 用户新增 | `system:user:create` | 2 | 用户管理 |
| 按钮 | 用户删除 | `system:user:delete` | 2 | 用户管理 |
| 按钮 | 分配角色 | `system:user:assignRole` | 2 | 用户管理 |
| 按钮 | 角色新增 | `system:role:create` | 2 | 角色管理 |
| 按钮 | 角色删除 | `system:role:delete` | 2 | 角色管理 |
| 按钮 | 分配权限 | `system:role:assignPerm` | 2 | 角色管理 |
| 目录 | 业务中台 | - | 0 | `null` |
| 菜单 | 知识库流转 | `system:knowledge:bookList` | 1 | 业务中台 |
| 按钮 | 批量指派 | `system:knowledge:borrow` | 2 | 知识库流转 |
| 按钮 | 归还入库 | `system:knowledge:return` | 2 | 知识库流转 |
| 按钮 | 新增文献 | `system:knowledge:create` | 2 | 知识库流转 |
| 按钮 | 编辑文献 | `system:knowledge:edit` | 2 | 知识库流转 |
| 按钮 | 删除文献 | `system:knowledge:delete` | 2 | 知识库流转 |

> **注意**：`ParentId = null`（非 `0`），因为 `SysMenu` 表存在自关联外键约束 `FK_SysMenu_SysMenu_ParentId`。

---

### 2.5 知识库/文献模块（Knowledge）

**Controller**: `MyAdmin.WebApi/Controllers/KnowledgeController.cs`
**Service**: `MyAdmin.Service/Implementations/KnowledgeService.cs`

| 接口 | 说明 | 权限码 |
|------|------|--------|
| `GET /api/knowledge/book/list` | 条件分页获取文献资产列表 | `system:knowledge:bookList` |
| `POST /api/knowledge/book` | 新增文献 | `system:knowledge:create` |
| `PUT /api/knowledge/book/{id}` | 修改文献 | `system:knowledge:edit` |
| `DELETE /api/knowledge/book/{id}` | 删除文献 | `system:knowledge:delete` |
| `POST /api/knowledge/borrow` | 批量流转指派借阅 | `system:knowledge:borrow` |
| `POST /api/knowledge/return/{logId}` | 办理资产归还入库 | `system:knowledge:return` |
| `GET /api/knowledge/log/list` | 获取流转审计历史日志 | 仅需认证 |

**核心逻辑**：
- `GetBookListAsync`：分页查询，按 `CreateTime DESC` 排序，支持 `keyword` 模糊搜索 `Title`/`Isbn`、`category` 精确筛选，使用 `AsNoTracking` 投影
- `CreateBookAsync`：校验 `Title`/`Isbn` 非空、ISBN 全局唯一后写入 `SysBook`
- `UpdateBookAsync`：校验文献存在、ISBN 唯一（排除自身）后更新
- `DeleteBookAsync`：校验文献存在后物理删除
- `BorrowAsync`：`Serializable` 事务，校验用户状态、逐本校验库存和状态，扣减库存并批量写入 `SysBorrowLog`，任一步失败即回滚
- `ReturnAsync`：事务内更新 `ActualReturnTime`、`LogStatus = 1`，回滚 `SysBook.Stock += 1`，已归还不可重复入库
- `GetLogListAsync`：查询前将超期未还记录批量标记为 `LogStatus = 2`（逾期），支持按 `logStatus` 筛选

**相关 DTO**：
- `KnowledgeBookDto`：`id`, `title`, `isbn`, `category`, `price`, `stock`, `status`, `createTime`
- `KnowledgeBookSaveRequest`（新增/修改共用）：`title`（必填，100 字符）, `isbn`（必填，30 字符，唯一）, `category`（可空，50 字符）, `price`（decimal(10,2)）, `stock`（默认 0）, `status`（`1` 正常流转 / `0` 盘点维护中，默认 `1`）
- `KnowledgeBorrowRequest`：`userId`, `bookIds[]`, `borrowDays`
- `KnowledgeBorrowLogDto`：`id`, `bookId`, `bookTitle`, `userId`, `username`, `nickname`, `borrowTime`, `returnTime`, `actualReturnTime`（可空）, `logStatus`（`0` 流转中 / `1` 已归还 / `2` 逾期未还）

---

## 3. 数据库核心关系

### 3.1 实体关系图（简化）

```
SysUser ──< SysUserRole >── SysRole ──< SysRoleMenu >── SysMenu
  │                                                         │
  │                          (自关联 ParentId)              │
  │                                                         │
  └──< SysBorrowLog >── SysBook
```

### 3.2 核心实体

**SysUser**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int PK | 自增 |
| Username | varchar(50) | 必填，唯一 |
| PasswordHash | varchar(256) | 必填，BCrypt |
| Nickname | varchar(50) | 必填 |
| Email | varchar(100) | 可空 |
| Status | tinyint | 默认 1（正常） |
| CreateTime | datetime | 默认 `GETDATE()` |

**SysRole**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int PK | 自增 |
| RoleName | varchar(50) | 必填 |
| RoleCode | varchar(50) | 必填，唯一 |
| Description | varchar(200) | 可空 |
| CreateTime | datetime | 默认 `GETDATE()` |

**SysUserRole**

| 字段 | 类型 | 说明 |
|------|------|------|
| UserId | int | 复合主键 |
| RoleId | int | 复合主键 |

> 级联删除：删除用户/角色时级联删除关联记录。

**SysMenu**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int PK | 自增 |
| ParentId | int? | 自关联外键，`null` 为根节点 |
| Title | nvarchar(50) | 必填 |
| Path | nvarchar(200) | 必填，按钮类型可为空字符串 |
| Component | nvarchar(200) | 可空，目录为 `"Layout"` |
| PermCode | nvarchar(100) | 可空，权限码 |
| MenuType | tinyint | 0=目录, 1=菜单, 2=按钮 |
| Icon | nvarchar(100) | 可空 |
| Sort | int | 默认 0 |
| Status | tinyint | 默认 1（启用） |
| CreateTime | datetime | 默认 `GETDATE()` |

**SysRoleMenu**

| 字段 | 类型 | 说明 |
|------|------|------|
| RoleId | int | 复合主键 |
| MenuId | int | 复合主键 |

**SysBook**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int PK | 自增 |
| Title | nvarchar(100) | 必填 |
| Isbn | varchar(30) | 必填，唯一索引 |
| Category | nvarchar(50) | 可空 |
| Price | decimal(10,2) | |
| Stock | int | 默认 0 |
| Status | tinyint | 默认 1（1=正常流转, 0=盘点维护中） |
| CreateTime | datetime | 默认 `GETDATE()` |

**SysBorrowLog**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int PK | 自增 |
| BookId | int | FK → SysBook.Id，级联删除 |
| BookTitle | nvarchar(100) | 文献标题快照 |
| UserId | int | FK → SysUser.Id，级联删除 |
| Username | varchar(50) | 借阅人账号快照 |
| Nickname | nvarchar(50) | 借阅人姓名快照 |
| BorrowTime | datetime | 默认 `GETDATE()` |
| ReturnTime | datetime | 应还时间 |
| ActualReturnTime | datetime? | 可空，实际归还时间 |
| LogStatus | tinyint | 默认 0（0=流转中, 1=已归还, 2=逾期未还） |

### 3.3 种子数据

- **用户**：`admin` / `password123`（BCrypt 哈希），昵称 `超级管理员`
- **角色**：`admin`（超级管理员）、`user`（普通用户）
- **菜单**：16 条（系统管理 9 条 + 业务中台 7 条）
- **绑定**：admin 用户 → admin 角色 → 全部 16 条菜单/按钮

种子数据入口：[`MyAdmin.Infrastructure/DbInitializer.cs`](file:///c:/Users/RISEY/Desktop/Codes/VS Code/Front-End/LVA/backend/MyAdmin.Infrastructure/DbInitializer.cs) 的 `SeedAsync` 方法。

---

## 4. Agent 使用指南

### 4.1 优先阅读文件

| 优先级 | 文件 | 用途 |
|--------|------|------|
| 1 | `ProjectGuide.md`（本文档） | 项目全景理解 |
| 2 | `API.md` | 完整接口文档（含请求/响应示例） |
| 3 | `MyAdmin.Infrastructure/MyAdminDbContext.cs` | 所有实体映射和数据库配置 |
| 4 | `MyAdmin.Infrastructure/DbInitializer.cs` | 种子数据，理解权限树结构 |
| 5 | `MyAdmin.WebApi/Program.cs` | 启动配置、DI 注册、中间件管线 |
| 6 | `MyAdmin.WebApi/Attributes/HasPermissionAttribute.cs` | 权限校验机制 |

### 4.2 模块边界

| 层 | 职责 | 禁止 |
|----|------|------|
| `MyAdmin.Core` | 实体定义、DTO、统一响应 `ApiResponse<T>`、JWT 配置类 | 不引用其他项目 |
| `MyAdmin.Infrastructure` | EF Core DbContext、实体映射配置、迁移、种子数据 | 不包含业务逻辑 |
| `MyAdmin.Service` | 业务逻辑实现（`Service` 实现 `IService` 接口） | 不直接返回 IActionResult |
| `MyAdmin.WebApi` | Controller、`[HasPermission]` 挂载、中间件配置 | 不包含业务逻辑，仅调用 Service |

### 4.3 CRUD 命名规范

项目遵循统一的 CRUD 命名模式：

| 操作 | Service 方法 | Controller 接口 | 权限码 |
|------|-------------|-----------------|--------|
| 分页列表 | `GetListAsync` | `GET /api/{module}/list` | `system:{module}:list` |
| 全部列表 | `GetAllAsync` | `GET /api/{module}/all` | `system:{module}:list` |
| 新增 | `CreateAsync` | `POST /api/{module}` | `system:{module}:create` |
| 修改 | `UpdateAsync` | `PUT /api/{module}/{id}` | `system:{module}:edit` 或具体权限码 |
| 删除 | `DeleteAsync` | `DELETE /api/{module}/{id}` | `system:{module}:delete` |
| 批量删除 | `BatchDeleteAsync` | `POST /api/{module}/batch-delete` | `system:{module}:delete` |

**知识库/文献模块特殊命名**：
- 实体为 `SysBook`，但 Controller 路由为 `/api/knowledge/...`
- Service 方法名前缀为 `Book`（如 `CreateBookAsync`、`GetBookListAsync`）
- 权限码前缀为 `system:knowledge:...`

### 4.4 新增模块 Checklist

当需要新增业务模块时，按以下步骤操作：

1. **Core**：在 `MyAdmin.Core/Entities/` 添加实体类，在 `MyAdmin.Core/Dtos/` 添加 DTO
2. **Infrastructure**：在 `MyAdminDbContext` 中添加 `DbSet<T>` 和 `modelBuilder` 配置；在 `DbInitializer` 种子数据中追加菜单权限
3. **Service**：添加 `IService` 接口 → 添加 `Service` 实现，在 `Program.cs` 注册 `AddScoped`
4. **WebApi**：添加 `Controller`，在接口上挂载 `[HasPermission]`
5. **迁移**：`dotnet ef migrations add <Name> -p MyAdmin.Infrastructure -s MyAdmin.WebApi`，种子注入时取消注释 `EnsureDeletedAsync`/`EnsureCreatedAsync`
6. **文档**：同步更新 `API.md` 和 `ProjectGuide.md`

### 4.5 常见陷阱

- **`ParentId` 不能为 `0`**：`SysMenu` 自关联外键约束只接受 `null` 或有效 Id，`0` 会触发 FK 冲突
- **种子数据**：`EnsureDeletedAsync()` + `EnsureCreatedAsync()` 默认注释，仅在需要重建数据库时取消注释，完成后必须恢复注释
- **权限校验**：`[HasPermission]` 通过 `SysUserRole → SysRoleMenu → SysMenu` 链路校验，不会因为父级菜单勾选而自动继承子级按钮权限码
- **ISBN 唯一**：`SysBook.Isbn` 有唯一索引，新增/修改时需校验；`KnowledgeBookSaveRequest` 新增和修改共用
- **借阅事务**：`BorrowAsync` 使用 `IsolationLevel.Serializable` 防止并发超卖，归还时校验 `ActualReturnTime` 防止重复入库