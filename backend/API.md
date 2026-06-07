# MyAdmin 后端 API 接口文档

> Base URL: `http://localhost:5014`（以 `launchSettings.json` 中端口为准）

## 通用说明

### 统一响应格式

所有接口 HTTP 状态码均为 `200`（框架层面），业务状态通过 `code` 字段区分：

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {}
}
```

| code | 含义     |
|------|----------|
| 200  | 成功     |
| 401  | 未认证   |
| 403  | 无权限   |
| 500  | 业务失败 |

- 具体接口在发生异常时，会返回 `code` 和 `message`，`data` 可能为 `null`。
- 例如，角色编码重复、资源不存在、权限不足等错误会通过业务 `code` 反馈。

### 认证方式

除登录接口外，其余接口需在请求头携带 JWT：

```
Authorization: Bearer <token>
```

---

## 1. 认证模块 (Auth)

### 1.1 用户登录

- **URL**: `POST /api/auth/login`
- **认证**: 不需要
- **请求体**:

```json
{
  "username": "admin",
  "password": "password123"
}
```

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

- **失败响应** (`code: 401`):

```json
{
  "code": 401,
  "message": "用户名或密码错误",
  "data": null
}
```

---

### 1.2 获取当前登录用户信息

- **URL**: `GET /api/auth/info`
- **认证**: 需要 Bearer Token
- **成功响应**:

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "userId": 1,
    "username": "admin",
    "nickname": "超级管理员",
    "roles": ["admin"],
    "permissions": ["system:role:create", "system:role:delete", "system:role:list", "system:user:create", "system:user:delete", "system:user:list"]
  }
}
```

> - `roles`：当前用户的角色编码列表（去重、按字母排序）
> - `permissions`：当前用户通过所有关联角色拥有的全部有效权限码列表（去重、按字母排序）。权限码来源于 `SysMenu` 表中 `PermCode` 非空且启用的菜单项，通过 `SysUserRole -> SysRoleMenu -> SysMenu` 关联链路汇总。

---

### 1.3 获取当前用户菜单树

- **URL**: `GET /api/auth/menus`
- **认证**: 需要 Bearer Token
- **说明**:
  - 返回当前登录用户可访问的菜单树形结构
  - 仅包含 `MenuType = 0`（目录）和 `MenuType = 1`（菜单）的启用菜单，不包含按钮类型（`MenuType = 2`）
  - 后端递归组装为树形结构，同一层按 `sort` 升序、`id` 升序排列
  - 菜单项通过 `SysUserRole -> SysRoleMenu -> SysMenu` 关联链路获取，并与 **JWT 基础自助模块** 合并后自动去重
  - **无条件放行**：`文献大厅`、`个人文献中心` 及 `业务中台` 父目录对所有已登录用户始终返回（不依赖角色勾选），确保 Admin 与普通用户侧边栏均可见
- **成功响应**:

```json
{
  "code": 200,
  "message": "操作成功",
  "data": [
    {
      "id": 1,
      "parentId": null,
      "title": "系统管理",
      "path": "/system",
      "component": "Layout",
      "permCode": null,
      "menuType": 0,
      "icon": "setting",
      "sort": 1,
      "children": [
        {
          "id": 2,
          "parentId": 1,
          "title": "用户管理",
          "path": "user",
          "component": "views/user/index.vue",
          "permCode": "system:user:list",
          "menuType": 1,
          "icon": "user",
          "sort": 1,
          "children": []
        },
        {
          "id": 3,
          "parentId": 1,
          "title": "角色管理",
          "path": "role",
          "component": "views/role/index.vue",
          "permCode": "system:role:list",
          "menuType": 1,
          "icon": "peoples",
          "sort": 2,
          "children": []
        }
      ]
    }
  ]
}
```

`MenuTreeDto` 字段说明：

| 字段       | 类型             | 说明                                           |
|------------|------------------|------------------------------------------------|
| id         | int              | 菜单 ID                                        |
| parentId   | int?             | 父级菜单 ID，顶级菜单为 `null`                  |
| title      | string           | 菜单标题                                       |
| path       | string           | 路由路径                                       |
| component  | string?          | 前端组件路径，目录类型可为 `null`               |
| permCode   | string?          | 权限码标识，目录类型可为 `null`                 |
| menuType   | byte             | 菜单类型：`0` 目录，`1` 菜单                    |
| icon       | string?          | 图标名称                                       |
| sort       | int              | 排序权重，值越小越靠前                          |
| children   | MenuTreeDto[]    | 子菜单列表                                     |

---

### 1.4 退出登录

- **URL**: `POST /api/auth/logout`
- **认证**: 需要 Bearer Token
- **成功响应**:

```json
{
  "code": 200,
  "message": "操作成功",
  "data": null
}
```

> 前端收到成功响应后清除本地 Token 即可。当前后端实现为无状态 JWT，不会在服务端记录或注销 Token，因此该接口仅用于前端清理本地认证信息。

---

## 2. 用户管理模块 (User)

> 以下接口均需认证。

### 2.1 条件分页获取用户列表

- **URL**: `GET /api/user/list`
- **Query 参数**:

| 参数       | 类型   | 必填 | 说明                           |
|------------|--------|------|--------------------------------|
| pageIndex  | int    | 否   | 页码，默认 `1`                 |
| pageSize   | int    | 否   | 每页条数，默认 `10`            |
| keyword    | string | 否   | 关键词（用户名/昵称/邮箱模糊） |

- **排序规则**: `items` 按 `id` **升序**排列（`ORDER BY Id ASC`），便于前端表格稳定展示
- **示例**: `GET /api/user/list?pageIndex=1&pageSize=10&keyword=张三`
- **成功响应**:

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "total": 45,
    "items": [
      {
        "id": 1,
        "username": "zhangsan",
        "nickname": "张三",
        "email": "zs@qq.com",
        "status": 1,
        "roles": ["user"],
        "createTime": "2026-05-27 10:00:00"
      }
    ]
  }
}
```

> `status`: `1` 正常，`0` 禁用

---

### 2.2 新增用户

- **URL**: `POST /api/user`
- **请求体**:

```json
{
  "username": "lisi",
  "nickname": "李四",
  "password": "123456",
  "email": "ls@qq.com",
  "status": 1,
  "roleIds": [2]
}
```

- **成功响应**: `data` 为 `null`

---

### 2.3 修改用户

- **URL**: `PUT /api/user/{id}`
- **路径参数**: `id` — 用户 ID
- **请求体**（`password` 可选，不传则不修改密码）:

```json
{
  "nickname": "李四修改版",
  "email": "ls_new@qq.com",
  "status": 1,
  "roleIds": [2]
}
```

- **成功响应**: `data` 为 `null`

---

### 2.4 单条删除用户

- **URL**: `DELETE /api/user/{id}`
- **成功响应**: `data` 为 `null`

---

### 2.5 批量删除用户

- **URL**: `POST /api/user/batch-delete`
- **请求体**: ID 数组

```json
[1, 2, 3]
```

- **成功响应**: `data` 为 `null`

---

## 3. 角色管理模块 (Role)

> 以下接口均需认证。

### 3.1 角色列表

#### 3.1.1 分页获取角色列表

- **URL**: `GET /api/role/list`
- **Query 参数**:

| 参数      | 类型 | 必填 | 说明                                   |
|-----------|------|------|----------------------------------------|
| pageIndex | int  | 否   | 页码，默认 `1`                          |
| pageSize  | int  | 否   | 每页条数，默认 `10`                     |

- **行为说明**:
  - 当 `pageIndex` 和 `pageSize` 同时不传时，接口返回全量角色列表。
  - 如果只传其中一个参数，则另一个参数会使用默认值：`pageIndex=1`、`pageSize=10`。
  - `pageIndex` 小于 `1` 时会被自动修正为 `1`。
  - `pageSize` 小于 `1` 时会被自动修正为 `10`。
  - `items` 按 `id` **升序**排列（`ORDER BY Id ASC`），便于前端表格稳定展示。
- **示例**:
  - 获取第 1 页，10 条：`GET /api/role/list?pageIndex=1&pageSize=10`
  - 获取全量角色：`GET /api/role/list`
- **成功响应**:

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "total": 2,
    "items": [
      {
        "id": 1,
        "roleName": "管理员",
        "roleCode": "admin",
        "description": "系统高权限"
      }
    ]
  }
}
```

---

#### 3.1.2 全量获取角色列表

- **URL**: `GET /api/role/all`
- **认证**: 需要 Bearer Token
- **说明**:
  - 返回系统中全部角色，不分页。
  - `items` 按 `id` 升序排列。
- **成功响应**:

```json
{
  "code": 200,
  "message": "操作成功",
  "data": [
    {
      "id": 1,
      "roleName": "管理员",
      "roleCode": "admin",
      "description": "系统高权限"
    }
  ]
}
```

---

### 3.2 新增角色

- **URL**: `POST /api/role`
- **请求体**:

```json
{
  "roleName": "编辑",
  "roleCode": "editor",
  "description": "内容编辑权限"
}
```

- **说明**:
  - `roleCode` 必须全局唯一。
  - 如果角色编码已存在，接口会返回错误信息 `角色编码已存在`。
- **成功响应**: `data` 为 `null`

---

### 3.3 修改角色

- **URL**: `PUT /api/role/{id}`
- **请求体**: 同新增

- **说明**:
  - `roleCode` 必须全局唯一。
  - 修改时如果指定的 `roleCode` 与其他角色冲突，会返回错误信息 `角色编码已存在`。
- **成功响应**: `data` 为 `null`

---

### 3.4 删除角色

- **URL**: `DELETE /api/role/{id}`
- **权限码**: `system:role:delete`（`[HasPermission]` 校验）
- **成功响应**: `data` 为 `null`

---

### 3.5 获取角色权限分配数据（el-tree 回显）

- **URL**: `GET /api/role/{id}/permissions`
- **路径参数**: `id` — 角色 ID
- **成功响应** (`data` 为 `RolePermissionDto`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "allMenus": [
      {
        "id": 1,
        "parentId": null,
        "title": "系统管理",
        "path": "/system",
        "component": "Layout",
        "permCode": null,
        "menuType": 0,
        "icon": "setting",
        "sort": 1,
        "children": [
          {
            "id": 2,
            "parentId": 1,
            "title": "用户管理",
            "menuType": 1,
            "children": [
              {
                "id": 4,
                "title": "用户新增",
                "menuType": 2,
                "permCode": "system:user:create",
                "children": []
              }
            ]
          }
        ]
      }
    ],
    "checkedMenuIds": [1, 2, 3, 4, 5, 6, 7]
  }
}
```

- **字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| allMenus | MenuTreeDto[] | 可分配的权限配置树（目录 0 / 菜单 1 / 按钮 2），按 `Sort`、`Id` 升序递归 |
| checkedMenuIds | int[] | 当前角色已绑定的可编辑 MenuId，供 `el-tree.setCheckedKeys()` |

- **业务说明**:
  - `allMenus` 来自 `SysMenu` 且 `Status = 1`，但 **排除** `文献大厅`、`个人文献中心` 及其全部子孙节点（JWT 基础自助模块，固化权限，不在后台勾选）
  - 业务中台下权限树仅展示 **文献资产管理**、**流转审计日志** 两个需 `HasPermission` 鉴权的大模块
  - `checkedMenuIds` 来自 `SysRoleMenu`，同样排除上述 JWT 基础模块的 Id，避免 el-tree 回显不可编辑节点

---

### 3.6 保存角色权限分配

- **URL**: `POST /api/role/{id}/permissions`
- **路径参数**: `id` — 角色 ID
- **请求体**: 平铺的 `int` 数组，**不包裹** `dto` 或其它对象壳

```json
[1, 2, 3, 4, 5, 6, 7]
```

- 传空数组 `[]` 表示清空该角色全部权限绑定
- **成功响应**: `data` 为 `null`
- **业务说明**（事务内执行）:
  1. 校验角色存在
  2. 忽略请求体中误入的 JWT 基础模块 MenuId（`文献大厅`、`个人文献中心` 子树）
  3. 校验其余 `menuIds` 均为启用中的有效菜单 ID
  4. 删除 `SysRoleMenu` 中该角色旧关联后批量插入；**保留**该角色上已有的 JWT 基础模块绑定，防止保存权限时误删

**前端示例**:

```typescript
// 保存勾选的 keys（含半选父节点时按业务收集 menuIds）
await api.post(`/api/role/${roleId}/permissions`, checkedKeys);
```

---

## 4. 接口级权限控制

项目提供 `[HasPermission]` 自定义属性，用于对接口进行细粒度权限校验。

### 使用方式

在 Controller 方法上标注权限码：

```csharp
[HasPermission("system:user:delete")]
public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
```

### 校验行为

- 请求未认证（无有效 JWT）时，返回 HTTP `401`
- 当前用户无对应权限码时，返回 HTTP `403`，body 为：

```json
{
  "code": 403,
  "message": "Forbidden",
  "data": null
}
```

- 校验通过则正常执行业务逻辑

### 权限判定链路

`SysUserRole -> SysRoleMenu -> SysMenu`，判断当前用户关联的所有角色是否拥有目标权限码（`SysMenu.PermCode`），且对应菜单需为启用状态（`Status = 1`）。

### 与前端 v-has-perm 的关系

- `GET /api/auth/info` 返回的 `permissions` 数组包含 **MenuType = 2（按钮级）** 的 `PermCode`（如 `system:user:create`），与 `[HasPermission]` 校验口径一致。
- 动态侧边栏菜单使用 `GET /api/auth/menus`，仅返回目录与菜单（`MenuType` 0、1），不含按钮节点；`文献大厅`、`个人文献中心` 对所有合法用户无条件并入，与角色勾选无关。
- 角色权限配置树使用 `GET /api/role/{id}/permissions`，不含 JWT 基础自助模块，仅用于分配 `HasPermission` 类权限。

### 已接入 HasPermission 的接口示例

| 接口 | 权限码 |
|------|--------|
| `POST /api/user` | `system:user:create` |
| `PUT /api/user/{id}` | `system:user:assignRole` |
| `DELETE /api/user/{id}` | `system:user:delete` |
| `POST /api/role` | `system:role:create` |
| `POST /api/role/{id}/permissions` | `system:role:assignPerm` |
| `DELETE /api/role/{id}` | `system:role:delete` |

---

## 5. 默认种子数据

`DbInitializer.SeedAsync()` 在首次启动时通过 `EnsureDeletedAsync()` + `EnsureCreatedAsync()` 物理重建数据库后注入：

### 用户

| 字段 | 值 |
|------|-----|
| 用户名 | `admin` |
| 密码 | `password123`（BCrypt 哈希存储） |
| 昵称 | `超级管理员` |
| 邮箱 | `admin@example.com` |
| 状态 | `1`（正常） |

### 角色

| RoleName | RoleCode | Description |
|----------|----------|-------------|
| 管理员 | `admin` | 超级管理员 |
| 普通用户 | `user` | 普通用户 |

### 菜单权限树（共 24 条）

| 层级 | Title | Path | Component | PermCode | MenuType | ParentId |
|------|-------|------|-----------|----------|----------|----------|
| 目录 | 系统管理 | `/system` | `Layout` | - | 0 | `null`（根节点） |
| 菜单 | 用户管理 | `user` | `system/user/index` | `system:user:list` | 1 | 系统管理.Id |
| 菜单 | 角色管理 | `role` | `system/role/index` | `system:role:list` | 1 | 系统管理.Id |
| 按钮 | 用户新增 | - | - | `system:user:create` | 2 | 用户管理.Id |
| 按钮 | 用户删除 | - | - | `system:user:delete` | 2 | 用户管理.Id |
| 按钮 | 分配角色 | - | - | `system:user:assignRole` | 2 | 用户管理.Id |
| 按钮 | 角色新增 | - | - | `system:role:create` | 2 | 角色管理.Id |
| 按钮 | 角色删除 | - | - | `system:role:delete` | 2 | 角色管理.Id |
| 按钮 | 分配权限 | - | - | `system:role:assignPerm` | 2 | 角色管理.Id |
| 目录 | 业务中台 | `/business` | `Layout` | - | 0 | `null`（根节点） |
| 菜单 | 文献大厅 | `hall` | `views/knowledge/hall.vue` | - | 1 | 业务中台.Id |
| 按钮 | 浏览文献 | - | - | - | 2 | 文献大厅.Id |
| 按钮 | 借阅文献 | - | - | - | 2 | 文献大厅.Id |
| 菜单 | 个人文献中心 | `personal` | `views/knowledge/personal.vue` | - | 1 | 业务中台.Id |
| 按钮 | 我的借阅 | - | - | - | 2 | 个人文献中心.Id |
| 按钮 | 我的历史 | - | - | - | 2 | 个人文献中心.Id |
| 按钮 | 自助归还 | - | - | - | 2 | 个人文献中心.Id |
| 菜单 | 文献资产管理 | `knowledge` | `views/knowledge/book.vue` | `system:knowledge:bookList` | 1 | 业务中台.Id |
| 按钮 | 新增文献 | - | - | `system:knowledge:create` | 2 | 文献资产管理.Id |
| 按钮 | 编辑文献 | - | - | `system:knowledge:edit` | 2 | 文献资产管理.Id |
| 按钮 | 删除文献 | - | - | `system:knowledge:delete` | 2 | 文献资产管理.Id |
| 菜单 | 流转审计日志 | `audit-log` | `views/knowledge/audit-log.vue` | `system:knowledge:adminLog` | 1 | 业务中台.Id |
| 按钮 | 批量指派借阅 | - | - | `system:knowledge:borrow` | 2 | 流转审计日志.Id |
| 按钮 | 管理员归还入库 | - | - | `system:knowledge:return` | 2 | 流转审计日志.Id |

> 根节点 `ParentId = null`（非 `0`），因为 `SysMenu` 表存在自关联外键约束，`ParentId = 0` 会触发 FK 冲突。

### 关联绑定

- **用户角色**：`admin` 用户 ↔ `admin` 角色
- **角色菜单（admin）**：`admin` 角色 ↔ 上述全部 24 条菜单/按钮（管理员拥有全栈权限）
- **角色菜单（user）**：`user` 角色 ↔ 业务中台目录 + 文献大厅 + 个人文献中心及其子按钮（共 8 条，仅 JWT 可见，不含 `system:knowledge:*` 权限码）

### JWT 基础自助模块（权限树与动态菜单分流）

| 模块 | `GET /api/auth/menus`（侧边栏） | `GET /api/role/{id}/permissions`（权限配置树） |
|------|--------------------------------|-----------------------------------------------|
| 文献大厅 + 子按钮 | 所有合法用户无条件可见 | **不展示、不可勾选** |
| 个人文献中心 + 子按钮 | 所有合法用户无条件可见 | **不展示、不可勾选** |
| 文献资产管理 + CRUD 按钮 | 按角色 `SysRoleMenu` 绑定 | 可展示、可勾选 |
| 流转审计日志 + 管理员按钮 | 按角色 `SysRoleMenu` 绑定 | 可展示、可勾选 |

---

## 6. 前端 Axios 示例

```typescript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5014',
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use((response) => {
  const { code, message, data } = response.data;
  if (code === 200) return data;
  if (code === 401) {
    localStorage.removeItem('token');
    window.location.href = '/login';
  }
  return Promise.reject(new Error(message));
});

export default api;
```

---

## 7. 文献管理系统 (Knowledge)

> 以下接口均需认证；CRUD 和指派/归还接口已接入 `[HasPermission]` 细粒度鉴权，自助借阅/归还/个人历史接口仅需 JWT 认证。
> 
> **动态隔离**：`GET /api/knowledge/log/list` 接口根据当前用户是否拥有 `system:knowledge:adminLog` 权限自动切换行为：管理员查全量大盘，普通用户仅能查看自己的日志。

### 7.1 条件分页获取文献资产列表

- **URL**: `GET /api/knowledge/book/list`
- **认证**: 仅需 JWT 认证（无需权限码）
- **说明**: 返回正常流转中的文献，**自动过滤 `Status = 2`（已下架/逻辑删除）的文献**
- **Query 参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| pageIndex | int | 否 | 页码，默认 `1` |
| pageSize | int | 否 | 每页条数，默认 `10` |
| keyword | string | 否 | 按 `title` / `isbn` 模糊搜索 |
| category | string | 否 | 文献分类精确筛选 |

成功响应 `data` 为扁平分页壳：

```json
{
  "total": 12,
  "items": [
    {
      "id": 1,
      "title": "ASP.NET Core 架构实践",
      "isbn": "KB-2026-001",
      "category": "技术文献",
      "price": 99.00,
      "stock": 5,
      "status": 1,
      "createTime": "2026-06-04 10:00:00"
    }
  ]
}
```

### 7.2 批量流转指派借阅

- **URL**: `POST /api/knowledge/borrow`
- **权限码**: `system:knowledge:borrow`
- **请求体**: 扁平 JSON，不包裹 `dto` / `data`

```json
{
  "userId": 3,
  "bookIds": [1, 2, 5],
  "borrowDays": 14
}
```

业务逻辑在数据库事务中执行：校验用户正常、逐本校验文献存在且 `status = 1`、`stock > 0`，扣减库存并批量写入 `SysBorrowLog`。任一文献不满足条件时整批回滚。

### 7.3 办理资产归还入库

- **URL**: `POST /api/knowledge/return/{logId}`
- **权限码**: `system:knowledge:return`

归还成功后，将 `ActualReturnTime` 设置为当前时间、`LogStatus` 改为 `1`，并将关联 `SysBook.Stock` 加 `1`。已归还记录不可重复入库。

### 7.4 获取流转审计历史日志

- **URL**: `GET /api/knowledge/log/list`
- **认证**: 需登录（JWT），无需额外权限码
- **动态隔离**: 后端通过 JWT 解析 `currentUserId`，检查用户是否拥有 `system:knowledge:adminLog` 权限：
  - **管理员**：返回全量流转日志（审计大盘）
  - **普通用户**：强制 `.Where(x => x.UserId == currentUserId)` 仅返回自身日志，阻断隐私外泄
- **Query 参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| pageIndex | int | 否 | 页码，默认 `1` |
| pageSize | int | 否 | 每页条数，默认 `10` |
| logStatus | byte | 否 | `0` 流转中，`1` 已归还，`2` 逾期未还 |

查询前会将已超过 `ReturnTime` 且仍流转中的记录批量标记为 `2`，以保证逾期筛选结果准确。

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "total": 1,
    "items": [
      {
        "id": 3,
        "bookId": 2,
        "bookTitle": "企业级 SaaS 中台设计权限白皮书",
        "isbn": "KB-2026-002",
        "userId": 1,
        "username": "admin",
        "nickname": "超级管理员",
        "borrowTime": "2026-06-04 10:30:00",
        "returnTime": "2026-06-11 10:30:00",
        "actualReturnTime": null,
        "logStatus": 0
      }
    ]
  }
}
```

### 7.5 普通用户自助借阅

- **URL**: `POST /api/knowledge/borrow/self`
- **认证**: 需登录（JWT），无需额外权限码
- **安全**: 不接收 `userId`，由后端从 JWT Claim 自动提取当前用户身份
- **请求体**:

```json
{
  "bookIds": [1, 2],
  "borrowDays": 14
}
```

`KnowledgeSelfBorrowRequest` 字段说明：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| bookIds | int[] | 是 | 拟借文献 ID 列表 |
| borrowDays | int | 是 | 借阅天数，必须 > 0 |

**业务防御规则**（任一不满足即返回 500 错误）：

1. 单人额度熔断：`当前未还数 + 本次拟借数 > 5` → `"您的借阅额度已满（单人上限 5 本），请先归还现有文献。"`
2. 禁止重复借阅：该用户已借阅同一文献且未归还 → `"您已借阅过文献《XXX》，在归还前无需重复借阅。"`
3. 库存校验：文献 `Stock <= 0` 或 `Status != 1` → 提示库存不足/维护中
4. 事务落库：`Serializable` 隔离级别，批量扣减库存 + 写入 `SysBorrowLog`，任一步失败全量回滚

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": null
}
```

- **失败响应** (`code: 200`，通过 `code` 字段区分):

```json
{
  "code": 500,
  "message": "您的借阅额度已满（单人上限 5 本），请先归还现有文献。",
  "data": null
}
```

### 7.6 获取当前用户个人借阅历史

- **URL**: `GET /api/knowledge/log/my-list`
- **认证**: 需登录（JWT），无需额外权限码
- **安全**: 后端强锁 `currentUserId`，只能查看自己的借阅记录
- **Query 参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| pageIndex | int | 否 | 页码，默认 `1` |
| pageSize | int | 否 | 每页条数，默认 `10` |
| logStatus | byte | 否 | `0` 流转中，`1` 已归还，`2` 逾期未还 |

查询前会对当前用户已过应还时间的记录自动标记逾期。

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "total": 1,
    "items": [
      {
        "id": 3,
        "bookId": 2,
        "bookTitle": "企业级 SaaS 中台设计权限白皮书",
        "isbn": "KB-2026-002",
        "userId": 1,
        "username": "admin",
        "nickname": "超级管理员",
        "borrowTime": "2026-06-04 10:30:00",
        "returnTime": "2026-06-11 10:30:00",
        "actualReturnTime": null,
        "logStatus": 0
      }
    ]
  }
}
```

### 7.7 普通用户自助归还

- **URL**: `POST /api/knowledge/return/self/{logId}`
- **认证**: 需登录（JWT），无需额外权限码
- **安全**: 所有权卡点校验，同时匹配 `logId` 和 `currentUserId`，防止横向越权
- **URL 参数**: `logId` 流转日志 ID

**防御规则**：

1. 所有权校验：`log.Id == logId && log.UserId == currentUserId` 双条件匹配，不匹配 → `"未找到该借阅记录，或无权操作此记录"`
2. 重复归还防护：已归还记录不可重复入库
3. 事务核销：更新 `ActualReturnTime`、`LogStatus = 1`，回补 `SysBook.Stock += 1`

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": null
}
```

- **失败响应**（越权） (`code: 200`):

```json
{
  "code": 500,
  "message": "未找到该借阅记录，或无权操作此记录",
  "data": null
}
```

### 7.8 新增文献

- **URL**: `POST /api/knowledge/book`
- **权限码**: `system:knowledge:create`
- **请求体**: 扁平 JSON，不包裹 `dto` / `data`

```json
{
  "title": "ASP.NET Core 架构实践",
  "isbn": "KB-2026-001",
  "category": "技术文献",
  "price": 99.00,
  "stock": 5,
  "status": 1
}
```

`KnowledgeBookSaveRequest` 字段说明：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 是 | 文献名称，最大 100 字符 |
| isbn | string | 是 | ISBN 编号，最大 30 字符，全局唯一 |
| category | string? | 否 | 文献分类，最大 50 字符 |
| price | decimal | 否 | 价格，精度 decimal(10,2) |
| stock | int | 否 | 库存数量，默认 0 |
| status | byte | 否 | 状态：`1` 正常流转，`0` 盘点维护中，`2` 已下架（逻辑删除），默认 `1` |

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": null
}
```

- **失败响应** (`code: 500`):

```json
{
  "code": 500,
  "message": "ISBN 已存在",
  "data": null
}
```

### 7.9 修改文献

- **URL**: `PUT /api/knowledge/book/{id}`
- **权限码**: `system:knowledge:edit`
- **请求体**: 与新增文献相同的 `KnowledgeBookSaveRequest` 结构

```json
{
  "title": "ASP.NET Core 架构实践（第2版）",
  "isbn": "KB-2026-001",
  "category": "技术文献",
  "price": 128.00,
  "stock": 3,
  "status": 1
}
```

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": null
}
```

- **失败响应** (`code: 500`):

```json
{
  "code": 500,
  "message": "文献不存在",
  "data": null
}
```

### 7.10 删除文献

- **URL**: `DELETE /api/knowledge/book/{id}`
- **权限码**: `system:knowledge:delete`
- **说明**: 
  - 先校验是否有 `LogStatus = 0`（流转中）的记录，如有则拒绝删除
  - 若无则**逻辑删除**（更新 `Status = 2`），保留文献档案和所有历史借阅日志
  - **级联删除已禁用**，确保历史记录不丢失

- **成功响应** (`code: 200`):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": null
}
```

- **失败响应** (`code: 500`):

```json
{
  "code": 500,
  "message": "文献不存在"
}
```

```json
{
  "code": 500,
  "message": "当前仍有文献流转在读者手中，无法强行销毁"
}
```

### 7.11 权限码汇总

| 接口 | 权限码 |
|------|--------|
| `POST /api/knowledge/book` | `system:knowledge:create` |
| `PUT /api/knowledge/book/{id}` | `system:knowledge:edit` |
| `DELETE /api/knowledge/book/{id}` | `system:knowledge:delete` |
| `GET /api/knowledge/book/list` | 仅需 JWT 认证 |
| `POST /api/knowledge/borrow` | `system:knowledge:borrow` |
| `POST /api/knowledge/return/{logId}` | `system:knowledge:return` |
| `GET /api/knowledge/log/list` | 动态隔离（管理员全量/普通用户仅自己） |
| `POST /api/knowledge/borrow/self` | 仅需 JWT 认证 |
| `GET /api/knowledge/log/my-list` | 仅需 JWT 认证 |
| `POST /api/knowledge/return/self/{logId}` | 仅需 JWT 认证 |

---

## 8. Dashboard 首页聚合 (Dashboard)

> 以下接口仅需 JWT 认证，不在 Controller 层使用 `[HasPermission]`。各 Widget 是否返回数据由 Service 根据当前用户权限码动态裁剪；无权限的管理员指标为 `null`，`recentActivities` 无权限时返回 `[]`。

### 8.1 获取首页聚合数据

- **URL**: `GET /api/dashboard`
- **认证**: 需登录（JWT）
- **安全**: 用户 ID 从 JWT 自动提取，禁止前端传递 `userId`
- **说明**:
  - 单次请求返回指标卡、图表、最近动态、个人最近借阅
  - 查询前会将已超过 `ReturnTime` 且仍流转中的记录批量标记为 `LogStatus = 2`（与流转审计接口一致）
  - `DueSoonTotal`：当前用户 `LogStatus = 0` 且应还时间在当前起 **3 天内** 的记录数

**Widget 权限映射**：
| 数据块 | 权限码 | 无权限时 |
|--------|--------|----------|
| `summary.bookTotal`、`summary.stockTotal` | `system:knowledge:bookList` | 字段为 `null` |
| `summary.borrowedTotal`、`summary.overdueTotal` | `system:knowledge:adminLog` | 字段为 `null` |
| `summary.userTotal` | `system:user:list` | 字段为 `null` |
| `summary.roleTotal` | `system:role:list` | 字段为 `null` |
| `charts.borrowTrend`、`charts.hotCategories`、`charts.logStatusDistribution` | `system:knowledge:adminLog` | 字段为 `null` |
| `charts.userGrowthTrend` | `system:user:list` | 字段为 `null` |
| `charts.preferenceCategories` | 仅需登录 | 始终返回（可为空数组） |
| `summary.currentBorrowTotal` 等个人指标 | 仅需登录 | 始终返回 |
| `recentActivities` | `system:knowledge:adminLog` | 返回 `[]` |
| `recentBorrows` | 仅需登录 | 始终返回（可为空数组） |

- **成功响应** (`code: 200`，admin 全权限示例):

```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "summary": {
      "bookTotal": 120,
      "borrowedTotal": 15,
      "stockTotal": 350,
      "overdueTotal": 2,
      "userTotal": 8,
      "roleTotal": 2,
      "currentBorrowTotal": 1,
      "dueSoonTotal": 0,
      "overdueBorrowTotal": 0,
      "historyTotal": 5
    },
    "charts": {
      "borrowTrend": [
        { "date": "2026-05-08", "count": 3 },
        { "date": "2026-05-09", "count": 0 }
      ],
      "hotCategories": [
        { "category": "计算机", "count": 42 }
      ],
      "logStatusDistribution": {
        "borrowing": 15,
        "returned": 200,
        "overdue": 2
      },
      "userGrowthTrend": [
        { "date": "2026-05-08", "count": 1 }
      ],
      "preferenceCategories": [
        { "category": "文学", "count": 3 }
      ]
    },
    "recentActivities": [
      {
        "activityType": "borrow",
        "time": "2026-06-05 14:30:00",
        "username": "user1",
        "nickname": "张三",
        "bookTitle": "深入理解计算机系统"
      }
    ],
    "recentBorrows": [
      {
        "id": 10,
        "bookTitle": "深入理解计算机系统",
        "borrowTime": "2026-06-05 14:30:00",
        "returnTime": "2026-06-19 14:30:00",
        "logStatus": 0
      }
    ]
  }
}
```

- **普通用户**（仅 JWT 基础权限）示例：`summary` 中管理员指标均为 `null`，`charts` 中仅 `preferenceCategories` 有值，其余图表字段为 `null`，`recentActivities` 为 `[]`，个人指标与 `recentBorrows` 正常返回。

**DTO 字段说明**：

| 字段 | 类型 | 说明 |
|------|------|------|
| summary.bookTotal | int? | 文献总数（排除已下架 `Status = 2`） |
| summary.borrowedTotal | int? | 当前流转中总数 |
| summary.stockTotal | int? | 可借库存总和 |
| summary.overdueTotal | int? | 逾期未还总数 |
| summary.userTotal | int? | 用户总数 |
| summary.roleTotal | int? | 角色总数 |
| summary.currentBorrowTotal | int | 当前用户流转中数量 |
| summary.dueSoonTotal | int | 当前用户即将到期数量（3 天内） |
| summary.overdueBorrowTotal | int | 当前用户逾期数量 |
| summary.historyTotal | int | 当前用户已归还数量 |
| charts.borrowTrend | DashboardTrendPointDto[]? | 近 30 天借阅趋势 |
| charts.hotCategories | DashboardCategoryStatDto[]? | 热门分类 Top 5 |
| charts.logStatusDistribution | object? | `borrowing` / `returned` / `overdue` |
| charts.userGrowthTrend | DashboardTrendPointDto[]? | 近 30 天用户增长 |
| charts.preferenceCategories | DashboardCategoryStatDto[] | 当前用户阅读偏好（按分类） |
| recentActivities | DashboardActivityDto[] | `activityType`: `borrow` 或 `return` |
| recentBorrows | DashboardRecentBorrowDto[] | 当前用户最近 5 条借阅 |

### 8.2 权限码汇总

| 接口 | 权限 |
|------|------|
| `GET /api/dashboard` | 仅需 JWT 认证（Widget 按权限码裁剪） |
