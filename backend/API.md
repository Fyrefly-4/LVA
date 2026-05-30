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
    "roles": ["admin"]
  }
}
```

---

### 1.3 退出登录

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

> 前端收到成功响应后清除本地 Token 即可（服务端无 Token 黑名单）。

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

### 3.1 分页获取角色列表

- **URL**: `GET /api/role/list`
- **Query 参数**:

| 参数      | 类型 | 必填 | 说明                |
|-----------|------|------|---------------------|
| pageIndex | int  | 否   | 页码，默认 `1`      |
| pageSize  | int  | 否   | 每页条数，默认 `10` |

- **排序规则**: `items` 按 `id` **升序**排列（`ORDER BY Id ASC`），便于前端表格稳定展示
- **示例**: `GET /api/role/list?pageIndex=1&pageSize=10`
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

- **成功响应**: `data` 为 `null`

---

### 3.3 修改角色

- **URL**: `PUT /api/role/{id}`
- **请求体**: 同新增

- **成功响应**: `data` 为 `null`

---

### 3.4 删除角色

- **URL**: `DELETE /api/role/{id}`
- **成功响应**: `data` 为 `null`

---

## 4. 默认种子数据

首次启动时自动迁移数据库并写入：

| 类型 | 值 |
|------|-----|
| 管理员账号 | `admin` / `password123` |
| 管理员角色 | `admin`（管理员） |
| 普通用户角色 | `user`（普通用户） |

---

## 5. 前端 Axios 示例

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
