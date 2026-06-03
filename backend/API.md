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
  - 菜单项通过 `SysUserRole -> SysRoleMenu -> SysMenu` 关联链路获取，自动去重
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
| allMenus | MenuTreeDto[] | 全量启用菜单树（目录 0 / 菜单 1 / 按钮 2），按 `Sort`、`Id` 升序递归 |
| checkedMenuIds | int[] | 当前角色已绑定的 MenuId，供 `el-tree.setCheckedKeys()` |

- **业务说明**:
  - `allMenus` 来自 `SysMenu` 且 `Status = 1`
  - `checkedMenuIds` 来自 `SysRoleMenu` 中该 `RoleId` 的记录

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
  2. 校验 `menuIds` 均为启用中的有效菜单 ID
  3. 删除 `SysRoleMenu` 中该角色旧关联
  4. 批量插入新关联

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
- 动态侧边栏菜单使用 `GET /api/auth/menus`，仅返回目录与菜单（`MenuType` 0、1），不含按钮节点。

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

### 菜单权限树（共 9 条）

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

> 根节点 `ParentId = null`（非 `0`），因为 `SysMenu` 表存在自关联外键约束，`ParentId = 0` 会触发 FK 冲突。

### 关联绑定

- **用户角色**：`admin` 用户 ↔ `admin` 角色
- **角色菜单**：`admin` 角色 ↔ 上述全部 9 条菜单/按钮（管理员拥有全栈权限）

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
