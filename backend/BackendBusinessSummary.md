# 后端业务与数据库结构总结

## 1. 后端架构概览

后端采用典型的三层架构：
- `MyAdmin.WebApi`：ASP.NET Core Web API，暴露 HTTP 接口，负责请求路由和认证授权。
- `MyAdmin.Service`：业务逻辑层，封装用户、角色、认证等操作。
- `MyAdmin.Infrastructure`：数据访问层，使用 Entity Framework Core 访问 SQL Server。
- `MyAdmin.Core`：公共 DTO、实体、统一响应类型和 JWT 配置类。

## 2. 身份认证与权限模型

### 2.1 JWT 认证

- `MyAdmin.WebApi/Program.cs` 中配置了 JWT Bearer 认证。
- `JwtSettings` 从配置文件加载，包含 `Issuer`、`Audience`、`SecretKey`、`ExpirationMinutes`。
- 所有非匿名接口都使用 `[Authorize]` 强制鉴权。

### 2.2 当前登录用户信息接口

- 接口：`GET /api/auth/info`
- 由 `AuthController.GetInfo()` 实现。
- 需携带 `Authorization: Bearer <token>`。
- 返回数据类型：`ApiResponse<UserInfoDto>`。

`UserInfoDto` 字段：
- `userId`：用户 ID
- `username`：用户名
- `nickname`：昵称
- `roles`：字符串数组，角色编码列表
- `permissions`：字符串数组，当前用户通过所有关联角色拥有的全部有效权限码列表

### 2.3 菜单与权限接口

- `GET /api/auth/menus`：返回当前登录用户可访问的菜单树（仅目录和菜单类型，不含按钮），由后端递归组装为树形结构，使用 `MenuTreeDto` 返回。
- `GET /api/auth/info`：返回当前用户基本信息、角色数组和权限码数组。
- `[HasPermission("permCode")]`：自定义权限属性，可用于接口级别细粒度鉴权，通过 `SysUserRole -> SysRoleMenu -> SysMenu` 链路校验权限码。

## 3. 业务模块与接口

### 3.1 认证模块（Auth）

`MyAdmin.WebApi/Controllers/AuthController.cs`
- `POST /api/auth/login`
  - 输入：`LoginDto`（`username`, `password`）
  - 输出：`LoginResponseDto`，包含 JWT `token`
- `GET /api/auth/info`
  - 输出：`UserInfoDto`，包含用户信息、角色列表和权限码列表
- `GET /api/auth/menus`
  - 输出：`List<MenuTreeDto>`，当前用户可访问的菜单树
- `POST /api/auth/logout`
  - 仅做前端本地清理，后端无状态 JWT 不实际注销 token。

`MyAdmin.Service/Implementations/AuthService.cs`
- `LoginAsync(LoginDto dto)`
  - 从 `SysUsers` 表查询用户
  - 验证用户存在、状态为 1，并校验 bcrypt 密码
  - 生成 JWT，包含 NameIdentifier、Name、nickname、Role 声明
- `GetCurrentUserInfoAsync(int userId)`
  - 查询用户及其角色
  - 通过 `SysUserRole -> SysRoleMenu -> SysMenu` 汇总权限码
  - 返回 `UserInfoDto`，包含 `roles` 与 `permissions`
- `GetCurrentUserMenusAsync(int userId)`
  - 查询当前用户通过角色关联可访问的菜单（`MenuType = 0` 或 `1`，且 `Status = 1`）
  - 返回扁平列表，由 Controller 层递归组装为树

### 3.2 用户管理模块（User）

`MyAdmin.WebApi/Controllers/UserController.cs`
- `GET /api/user/list`
  - 分页查询用户列表，可按关键词搜索 `username`、`nickname`、`email`
- `POST /api/user`
  - 新增用户
- `PUT /api/user/{id}`
  - 修改用户
- `DELETE /api/user/{id}`
  - 删除单个用户
- `POST /api/user/batch-delete`
  - 批量删除用户

`MyAdmin.Service/Implementations/UserService.cs`
- `GetListAsync(pageIndex, pageSize, keyword)`
  - 支持分页、排序为 `Id ASC`
  - 每条记录返回 `UserDto`，包含角色编码数组
- `CreateAsync(UserSaveDto dto)`
  - 必填：用户名、密码
  - 校验用户名唯一
  - 校验 `RoleIds` 是否都存在
  - 密码使用 bcrypt 哈希
  - 保存用户后，写入用户角色关联表
- `UpdateAsync(int id, UserSaveDto dto)`
  - 修改昵称、邮箱、状态
  - 可选修改密码（若传入新密码则更新哈希）
  - 同步用户角色关联
- `DeleteAsync(int id)`
  - 删除用户记录
- `BatchDeleteAsync(IEnumerable<int> ids)`
  - 批量删除用户记录

`UserDto` 字段：
- `id`, `username`, `nickname`, `email`, `status`
- `roles`：角色编码列表
- `createTime`：字符串格式创建时间

`UserSaveDto` 字段：
- `username`, `nickname`, `password`, `email`, `status`, `roleIds`

### 3.3 角色管理模块（Role）

`MyAdmin.WebApi/Controllers/RoleController.cs`
- `GET /api/role/list`
  - 支持分页查询，若不传 `pageIndex`/`pageSize` 则返回全部角色
- `GET /api/role/all`
  - 返回全部角色列表
- `POST /api/role`
  - 新增角色
- `PUT /api/role/{id}`
  - 修改角色
- `DELETE /api/role/{id}`
  - 删除角色
- `GET /api/role/{id}/permissions`
  - 获取全量菜单树 + 当前角色已勾选 MenuId（`RolePermissionDto`）
- `POST /api/role/{id}/permissions`
  - 保存角色权限，请求体为平铺 `List<int>` menuIds（不包 dto 壳）

`MyAdmin.Service/Implementations/RoleService.cs`
- `GetListAsync(int? pageIndex, int? pageSize)`
  - 分页查询或全量查询角色
- `GetAllAsync()`
  - 返回所有角色
- `CreateAsync(RoleSaveDto dto)`
  - 校验 `RoleCode` 全局唯一
- `UpdateAsync(int id, RoleSaveDto dto)`
  - 校验修改后的 `RoleCode` 不冲突
- `DeleteAsync(int id)`
  - 删除角色
- `GetRolePermissionsAsync(int roleId)`
  - 查询 `Status = 1` 的全量菜单并递归为 `AllMenus`
  - 查询 `SysRoleMenu` 得到 `CheckedMenuIds`
- `SaveRolePermissionsAsync(int roleId, List<int> menuIds)`
  - 事务内先删后插同步 `SysRoleMenu`，支持空数组清空

`RolePermissionDto` 字段：
- `allMenus`：`List<MenuTreeDto>`
- `checkedMenuIds`：`List<int>`

`RoleDto` 字段：
- `id`, `roleName`, `roleCode`, `description`

`RoleSaveDto` 字段：
- `roleName`, `roleCode`, `description`

## 4. 数据库表结构

### 4.1 SysUser

字段：
- `Id` int PK 自增
- `Username` varchar(50) 必填，唯一
- `PasswordHash` varchar(256) 必填
- `Nickname` varchar(50) 必填
- `Email` varchar(100) 可空
- `Status` tinyint，默认 1
- `CreateTime` datetime，默认 `GETDATE()`

关系：
- 与 `SysUserRole` 一对多

### 4.2 SysRole

字段：
- `Id` int PK 自增
- `RoleName` varchar(50) 必填
- `RoleCode` varchar(50) 必填，唯一
- `Description` varchar(200) 可空
- `CreateTime` datetime，默认 `GETDATE()`

关系：
- 与 `SysUserRole` 一对多

### 4.3 SysUserRole

字段：
- `UserId` int
- `RoleId` int

主键：
- 复合主键 `(UserId, RoleId)`

外键：
- `UserId` -> `SysUser.Id`
- `RoleId` -> `SysRole.Id`
- 删除用户或角色时级联删除关联记录

## 5. 数据初始化

`DbInitializer.SeedAsync()` 会执行：
- 物理删除旧库并重新建库（`EnsureDeletedAsync()` + `EnsureCreatedAsync()`，首次注入成功后已注释）
- 注入完整 RBAC 种子数据：

### 菜单权限树（9 条，利用 EF 上下文生命周期避免硬编码 Id）

| 层级 | Title | PermCode | MenuType | 父级 |
|------|-------|----------|----------|------|
| 目录 | 系统管理 | - | 0 | `null`（根节点） |
| 菜单 | 用户管理 | `system:user:list` | 1 | 系统管理 |
| 菜单 | 角色管理 | `system:role:list` | 1 | 系统管理 |
| 按钮 | 用户新增 | `system:user:create` | 2 | 用户管理 |
| 按钮 | 用户删除 | `system:user:delete` | 2 | 用户管理 |
| 按钮 | 分配角色 | `system:user:assignRole` | 2 | 用户管理 |
| 按钮 | 角色新增 | `system:role:create` | 2 | 角色管理 |
| 按钮 | 角色删除 | `system:role:delete` | 2 | 角色管理 |
| 按钮 | 分配权限 | `system:role:assignPerm` | 2 | 角色管理 |

> 根节点 `ParentId = null`，因为 `SysMenu` 表存在自关联外键约束（`FK_SysMenu_SysMenu_ParentId`），`ParentId = 0` 会触发 FK 冲突。

### 角色（2 条）

- 管理员：`RoleCode = admin`，Description = "超级管理员"
- 普通用户：`RoleCode = user`，Description = "普通用户"

### 用户（1 条）

- `username`: `admin`，`password`: `password123`（BCrypt 哈希），`nickname`: `超级管理员`

### 关联绑定

- **用户角色**：admin 用户 ↔ admin 角色
- **角色菜单**：admin 角色 ↔ 全部 9 条菜单/按钮（管理员拥有全栈动态菜单渲染权与细粒度接口操作权）

## 6. 重要结论

- 当前后端已有用户、角色、认证、动态菜单四大功能模块。
- 业务核心是：用户管理、角色管理、JWT 登录鉴权、当前用户信息查询、菜单树获取、接口级权限控制。
- 已实现菜单权限树和权限码列表接口：`GET /api/auth/menus` 和 `GET /api/auth/info`（含 `permissions` 字段）。
- 权限模型通过 `SysUserRole -> SysRoleMenu -> SysMenu` 链路实现，支持角色关联菜单、按钮级权限标识（`PermCode`）。
- `[HasPermission]` 自定义属性可用于任意接口进行细粒度权限校验。

---

文档生成于 `backend/BackendBusinessSummary.md`，可直接用于业务功能设计参考。

## 7. 本次 RBAC 扩展说明

本次在现有 `SysUser` / `SysRole` / `SysUserRole` 基础上，补充了"动态菜单"和"细粒度权限"相关能力。

### 7.1 新增数据表

- `SysMenu`
  - 字段：`Id`、`ParentId`、`Title`、`Path`、`Component`、`PermCode`、`MenuType`、`Icon`、`Sort`、`Status`、`CreateTime`
  - 用途：维护目录、菜单、按钮三类资源
  - 说明：
    - `MenuType = 0` 表示目录
    - `MenuType = 1` 表示菜单
    - `MenuType = 2` 表示按钮
    - `PermCode` 用于按钮级或接口级权限标识

- `SysRoleMenu`
  - 字段：`RoleId`、`MenuId`
  - 用途：角色与菜单的关联表

### 7.2 用户信息返回值扩展

`UserInfoDto` 新增字段：

- `permissions`
  - 类型：`List<string>`
  - 来源：当前用户关联角色所拥有的全部有效菜单权限码
  - 去重规则：按权限码唯一汇总，只保留启用菜单的 `PermCode`

### 7.3 认证服务扩展

`AuthService.GetCurrentUserInfoAsync(int userId)` 现在会：

- 查询当前用户基础信息
- 通过 `SysUserRole -> SysRoleMenu -> SysMenu` 关联链路汇总权限码
- 返回 `roles` 与 `permissions`

### 7.4 菜单接口扩展

新增接口：

- `GET /api/auth/menus`

说明：

- 返回当前登录用户可访问的菜单树
- 仅包含 `MenuType = 0` 或 `MenuType = 1` 的启用菜单
- 由后端递归组装为树形结构
- 返回结构使用 `MenuTreeDto`

`MenuTreeDto` 主要字段：

- `id`
- `parentId`
- `title`
- `path`
- `component`
- `permCode`
- `menuType`
- `icon`
- `sort`
- `children`

### 7.5 权限控制扩展

新增自定义权限属性：

- `HasPermissionAttribute`

使用方式：

```csharp
[HasPermission("system:user:delete")]
```

行为说明：

- 请求未认证时返回 `401`
- 当前用户无对应权限时返回 `403`
- 通过 `SysUserRole -> SysRoleMenu -> SysMenu` 判断权限是否存在

### 7.6 数据库配置说明

`MyAdminDbContext` 已补充：

- `SysMenu` 实体配置
- `SysRoleMenu` 实体配置
- `SysMenu.ParentId` 自关联关系
- `SysRoleMenu` 与 `SysRole`、`SysMenu` 的多对多关联
- `ParentId`、`PermCode`、`MenuId` 索引

### 7.7 迁移说明

已补充菜单与权限表对应的 EF Core 迁移，以配合启动时的 `MigrateAsync()` 自动建表流程。

## 8. 使用 Swagger 调试接口

项目已集成 Swagger（Swashbuckle），可在开发环境下通过浏览器直接调试所有 API 接口。

### 8.1 启动与访问

1. 确保运行环境为 `Development`（`launchSettings.json` 中默认配置）。
2. 启动项目后，浏览器会自动打开 `http://localhost:5014/swagger`（以 `launchSettings.json` 中 `launchUrl` 配置为准）。
3. 也可手动访问：`http://localhost:5014/swagger`。

### 8.2 Swagger 页面结构

- 接口按 Controller 分组展示（`Auth`、`User`、`Role`）。
- 每个接口可点击展开，查看请求方法、URL、参数类型和响应结构。
- 点击 **Try it out** 按钮可直接在页面内发起请求。

### 8.3 认证调试流程

由于大部分接口需要 JWT 认证，调试流程如下：

**步骤一：调用登录接口获取 Token**

1. 在 Swagger 页面找到 `POST /api/auth/login` 接口。
2. 点击 **Try it out**。
3. 在 Request body 中填入登录信息：

```json
{
  "username": "admin",
  "password": "password123"
}
```

4. 点击 **Execute**。
5. 在响应体中复制返回的 `data.token` 值。

**步骤二：在 Swagger 中配置 Token**

1. 点击 Swagger 页面右上角的 **Authorize** 按钮。
2. 在弹出的对话框中，输入 Token 值（直接粘贴 Token 字符串，无需加 `Bearer` 前缀，Swagger 会自动拼接）。
3. 点击 **Authorize** 确认，再点击 **Close** 关闭对话框。

**步骤三：调试需要认证的接口**

配置 Token 后，所有后续请求会自动携带 `Authorization: Bearer <token>` 请求头。此时可以：

- 调用 `GET /api/auth/info` 查看当前登录用户信息（含角色和权限码）。
- 调用 `GET /api/auth/menus` 获取当前用户可访问的菜单树。
- 调用 `GET /api/user/list` 查看用户列表。
- 调用其他 CRUD 接口进行增删改查操作。

### 8.4 Swagger 配置说明

项目 `Program.cs` 中已配置：

- **API 文档信息**：标题 `MyAdmin API`，版本 `v1`。
- **JWT Bearer 认证定义**：Swagger 页面支持通过 Authorize 按钮统一设置 Token。
- **全局安全要求**：所有接口默认需要 Bearer Token 认证（标注 `[AllowAnonymous]` 的接口除外）。

### 8.5 常见问题

- **Token 过期**：JWT 过期后会收到 `401` 响应，需重新登录获取新 Token 并再次 Authorize。
- **Swagger 页面无法访问**：检查是否运行在 `Development` 环境，Swagger 仅在开发环境下启用（`app.Environment.IsDevelopment()`）。
- **端口不一致**：以 `launchSettings.json` 中的 `applicationUrl` 为准，默认为 `http://localhost:5014`。
- **请求返回 403**：当前用户缺少接口所需的权限码，检查 `SysRoleMenu` 和 `SysMenu` 表中是否正确配置了对应的权限关联。
