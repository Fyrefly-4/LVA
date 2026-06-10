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

所有接口 HTTP 状态码均为 `200`（部分错误场景如权限、验证失败例外），业务状态通过 `code` 字段区分：

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {}
}
```

| code | HTTP 状态码 | 含义 |
|------|-------------|------|
| 200  | 200         | 成功 |
| 400  | 400         | 请求参数无效/模型绑定失败（自动由 `ApiBehaviorOptions` 统一格式，不再返回 `ProblemDetails`） |
| 401  | 401         | 未认证（`[HasPermission]` 鉴权失败或 JWT 无效） |
| 403  | 403         | 无权限（`[HasPermission]` 权限码不匹配） |
| 500  | 200 或 500  | 业务失败/服务端异常（Controller `try/catch` 捕获或全局异常中间件捕获，均返回 `ApiResponse` 格式） |

**错误处理机制（代码实现级）**：

- **Controller 层**：各接口方法内部 `try/catch`，业务校验失败（角色不存在、文献被借阅中、ISBN 重复等）通过 `throw InvalidOperationException` 抛出，`catch` 块返回 `ApiResponse.Fail(ex.Message)`
- **权限过滤器**：`[HasPermission]` 内部查询权限码时发生 DB 异常，自动返回 `ApiResponse.Fail(ex.GetBaseException().Message, 500)`，不再冒泡到 ASP.NET 默认错误页
- **模型验证**：请求体 JSON 无法绑定到 DTO（字段类型不匹配、必填缺失）时，通过 `ApiBehaviorOptions.InvalidModelStateResponseFactory` 改写为 `ApiResponse` 格式（`code: 400`），不再返回默认的 `ProblemDetails`
- **全局异常中间件**：所有从中间件管线/过滤器/Controller 漏出的未捕获异常，统一包装为 `ApiResponse` JSON（`code: 500`，`message` 取 `ex.GetBaseException().Message` 以穿透 `DbUpdateException` 等包装异常的内层真实错误）
- **异常消息穿透**：统一使用 `ex.GetBaseException().Message` 读取最内层异常消息，避免 `DbUpdateException`、`DbUpdateConcurrencyException` 等 EF Core 包装异常仅显示 "An error occurred while updating the entries" 这类无意义信息

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
- `GetCurrentUserMenusAsync`：查询角色绑定的 `MenuType = 0/1` 菜单，再 **无条件合并** `文献大厅`、`个人文献中心` 及 `业务中台` 目录，返回扁平列表，由 Controller 递归组装为树

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
- `GetRolePermissionsAsync`：查询全量启用菜单，经 `JwtBaselineMenuHelper` **排除** JWT 基础自助模块后递归为 `AllMenus`；`CheckedMenuIds` 同步排除不可编辑节点
- `SaveRolePermissionsAsync`：事务内先删后插同步可编辑权限；**保留**角色上已有的 JWT 基础模块 `SysRoleMenu` 绑定，忽略请求体中的对应 MenuId

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

**当前全部 24 条权限树**：

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
| 菜单 | 文献大厅 | - | 1 | 业务中台 |
| 按钮 | 浏览文献 | - | 2 | 文献大厅 |
| 按钮 | 借阅文献 | - | 2 | 文献大厅 |
| 菜单 | 个人文献中心 | - | 1 | 业务中台 |
| 按钮 | 我的借阅 | - | 2 | 个人文献中心 |
| 按钮 | 我的历史 | - | 2 | 个人文献中心 |
| 按钮 | 自助归还 | - | 2 | 个人文献中心 |
| 菜单 | 文献资产管理 | `system:knowledge:bookList` | 1 | 业务中台 |
| 按钮 | 新增文献 | `system:knowledge:create` | 2 | 文献资产管理 |
| 按钮 | 编辑文献 | `system:knowledge:edit` | 2 | 文献资产管理 |
| 按钮 | 删除文献 | `system:knowledge:delete` | 2 | 文献资产管理 |
| 菜单 | 流转审计日志 | `system:knowledge:adminLog` | 1 | 业务中台 |
| 按钮 | 批量指派借阅 | `system:knowledge:borrow` | 2 | 流转审计日志 |
| 按钮 | 管理员归还入库 | `system:knowledge:return` | 2 | 流转审计日志 |

**角色菜单可见性**：

| 角色 | Knowledge 侧边栏（`GET /api/auth/menus`） | 权限配置树（`GET /api/role/{id}/permissions`） |
|------|------------------------------------------|---------------------------------------------|
| 任意已登录用户 | 文献大厅、个人文献中心 **始终可见**（JWT 基础模块） | — |
| `admin` | 另含文献资产管理、流转审计日志（按 `SysRoleMenu`） | 仅可勾选文献资产管理、流转审计日志及其按钮 |
| `user` | 同上（大厅 + 个人中心固化；无管理员菜单） | 仅可勾选文献资产管理、流转审计日志及其按钮（若分配） |

**辅助类**：`MyAdmin.Service/Helpers/JwtBaselineMenuHelper.cs` — 识别并过滤 `文献大厅`、`个人文献中心` 子树。

> **注意**：`ParentId = null`（非 `0`），因为 `SysMenu` 表存在自关联外键约束 `FK_SysMenu_SysMenu_ParentId`。

---

### 2.5 知识库/文献模块（Knowledge）

**Controller**: `MyAdmin.WebApi/Controllers/KnowledgeController.cs`
**Service**: `MyAdmin.Service/Implementations/KnowledgeService.cs`

| 接口 | 说明 | 权限码 |
|------|------|--------|
| `GET /api/knowledge/book/list` | 条件分页获取文献资产列表（自动过滤 `Status = 2` 已下架文献） | 仅需 JWT 认证 |
| `POST /api/knowledge/book` | 新增文献 | `system:knowledge:create` |
| `PUT /api/knowledge/book/{id}` | 修改文献 | `system:knowledge:edit` |
| `DELETE /api/knowledge/book/{id}` | 删除文献 | `system:knowledge:delete` |
| `POST /api/knowledge/borrow` | 批量流转指派借阅 | `system:knowledge:borrow` |
| `POST /api/knowledge/return/{logId}` | 办理资产归还入库 | `system:knowledge:return` |
| `GET /api/knowledge/log/list` | 流转审计日志（管理员全量/普通用户仅自己） | 动态隔离（见下方） |
| `POST /api/knowledge/borrow/self` | 普通用户自助借阅 | 仅需 JWT 认证 |
| `GET /api/knowledge/log/my-list` | 获取当前用户个人借阅历史 | 仅需 JWT 认证 |
| `POST /api/knowledge/return/self/{logId}` | 普通用户自助归还核销 | 仅需 JWT 认证 |

**核心逻辑**：
- `GetBookListAsync`：分页查询，按 `CreateTime DESC` 排序，支持 `keyword` 模糊搜索 `Title`/`Isbn`、`category` 精确筛选，使用 `AsNoTracking` 投影，**自动过滤 `Status = 2`（已下架）的文献**
- `CreateBookAsync`：校验 `Title`/`Isbn` 非空、ISBN 全局唯一后写入 `SysBook`
- `UpdateBookAsync`：校验文献存在、ISBN 唯一（排除自身）后更新
- `DeleteBookAsync`：先校验是否有 `LogStatus = 0`（流转中）的记录，如有则提示"当前仍有文献流转在读者手中，无法强行销毁"；若无则**逻辑删除**（更新 `Status = 2`），保留文献档案和所有历史借阅日志
- `BorrowAsync`：`Serializable` 事务，校验用户状态、逐本校验库存和状态，扣减库存并批量写入 `SysBorrowLog`，任一步失败即回滚
- `ReturnAsync`：事务内更新 `ActualReturnTime`、`LogStatus = 1`，回滚 `SysBook.Stock += 1`，已归还不可重复入库
- `GetLogListAsync`：**动态隔离** — 通过 `SysUserRole → SysRoleMenu → SysMenu` 检查当前用户是否拥有 `system:knowledge:adminLog` 权限：管理员查看全量大盘，普通用户强制 `.Where(x => x.UserId == currentUserId)` 仅返回自身数据。查询前批量标记逾期
- `SelfBorrowAsync`：从 JWT 提取 `currentUserId`，校验单人额度上限（未还数 + 本次 ≤ 5 本）、禁止重复借阅同一文献，`Serializable` 事务内批量校验库存并扣减
- `GetMyLogListAsync`：强制锁死 `currentUserId` 实现数据隔离，查询前批量标记逾期，返回个人借阅历史
- `SelfReturnAsync`：所有权卡点校验 `logId + userId` 双条件匹配，防止横向越权，事务内回补库存

**相关 DTO**：
- `KnowledgeBookDto`：`id`, `title`, `isbn`, `category`, `price`, `stock`, `status`, `createTime`
- `KnowledgeBookSaveRequest`（新增/修改共用）：`title`（必填，100 字符）, `isbn`（必填，30 字符，唯一）, `category`（可空，50 字符）, `price`（decimal(10,2)）, `stock`（默认 0）, `status`（`1` 正常流转 / `0` 盘点维护中 / `2` 已下架（逻辑删除），默认 `1`）
- `KnowledgeBorrowRequest`：`userId`, `bookIds[]`, `borrowDays`
- `KnowledgeSelfBorrowRequest`：`bookIds[]`, `borrowDays`（无 `userId`，由 JWT 自动提取）
- `KnowledgeBorrowLogDto`：`id`, `bookId`, `bookTitle`, `isbn`, `userId`, `username`, `nickname`, `borrowTime`, `returnTime`, `actualReturnTime`（可空）, `logStatus`（`0` 流转中 / `1` 已归还 / `2` 逾期未还）

---

### 2.6 Dashboard 模块（首页聚合）

**Controller**: `MyAdmin.WebApi/Controllers/DashboardController.cs`
**Service**: `MyAdmin.Service/Implementations/DashboardService.cs`
**Helper**: `MyAdmin.Service/Helpers/UserPermissionHelper.cs`、`DashboardBuildContext.cs`

| 接口 | 说明 | 权限 |
|------|------|------|
| `GET /api/dashboard` | 首页聚合数据（指标卡、图表、动态、最近借阅） | 仅需 JWT 认证 |

**设计原则**：
- 统一 Dashboard 页面，不按角色写死 `admin` / `user`；由当前用户权限码决定各 Widget 是否填充
- 用户 ID 从 JWT `ClaimTypes.NameIdentifier` 提取，禁止前端传参
- 单次请求返回全部数据；无权限的指标为 `null`，列表型 Widget 无权限时返回 `[]`

**Widget 权限映射**：

| Widget | 权限码 |
|--------|--------|
| `BookTotal`、`StockTotal` | `system:knowledge:bookList` |
| `BorrowedTotal`、`OverdueTotal`、`BorrowTrend`、`HotCategories`、`LogStatusDistribution`、`RecentActivities` | `system:knowledge:adminLog` |
| `UserTotal`、`UserGrowthTrend` | `system:user:list` |
| `RoleTotal` | `system:role:list` |
| `CurrentBorrowTotal`、`DueSoonTotal`、`OverdueBorrowTotal`、`HistoryTotal`、`PreferenceCategories`、`RecentBorrows` | 仅需登录 |

**核心逻辑**：
- `GetDashboardAsync`：一次查询用户权限码 → 批量标记逾期（与 Knowledge 一致）→ 顺序构建 Summary / Charts / RecentActivities / RecentBorrows（避免 DbContext 并发冲突）
- `DueSoonTotal`：`LogStatus = 0` 且应还时间在当前起 3 天内
- `BorrowTrend` / `UserGrowthTrend`：最近 30 天按日统计，缺失日期补 `0`
- `HotCategories`：全库借阅按分类 Top 5；`PreferenceCategories`：当前用户借阅按分类统计
- `RecentActivities`：最近借阅 + 最近归还各取 10 条合并排序后取 Top 10（需 `system:knowledge:adminLog`）
- `RecentBorrows`：当前用户最近 5 条借阅记录

**相关 DTO**（`MyAdmin.Core/Dtos/DashboardDtos.cs`）：
- `DashboardDto`：`summary`, `charts`, `recentActivities`, `recentBorrows`
- `DashboardSummaryDto`：管理员指标（可空）+ 个人指标（始终有值）
- `DashboardChartsDto`：`borrowTrend`, `hotCategories`, `logStatusDistribution`, `userGrowthTrend`, `preferenceCategories`
- `DashboardActivityDto`：`activityType`（`borrow` / `return`）, `time`, `username`, `nickname`, `bookTitle`
- `DashboardRecentBorrowDto`：`id`, `bookTitle`, `borrowTime`, `returnTime`, `logStatus`

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
| Status | tinyint | 默认 1（1=正常流转, 0=盘点维护中, 2=已下架（逻辑删除）） |
| CreateTime | datetime | 默认 `GETDATE()` |

**SysBorrowLog**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int PK | 自增 |
| BookId | int | FK → SysBook.Id，**级联删除已禁用**（`DeleteBehavior.Restrict`） |
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
- **菜单**：24 条（系统管理 9 条 + 业务中台 15 条）
- **绑定**：admin 用户 → admin 角色 → 全部 24 条菜单/按钮；增量更新时会确保 admin 角色正确绑定"个人文献中心"菜单节点

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

**Dashboard 模块特殊命名**：
- 单接口聚合：`GET /api/dashboard`，Service 方法 `GetDashboardAsync`
- 不在 Controller 挂载 `[HasPermission]`，Widget 可见性在 Service 内按权限码裁剪

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