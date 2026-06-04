# FrontendGuide

## 项目简介

- **项目定位**：LightVueAdmin 后台管理系统前端（Vue3 单页应用）
- **技术栈**：Vue 3 + Vite
- **UI 框架**：Element Plus
- **状态管理方案**：Pinia
- **路由方案**：Vue Router 5（`createWebHistory` 模式）

---

## 项目结构

```
src
├── api          # 接口封装层，按业务模块拆分
├── router       # 路由配置（静态路由 + 动态路由注入逻辑）
├── store        # Pinia 状态管理（当前仅 PermissionStore）
├── views        # 页面视图组件，按业务模块分目录
├── layout       # 布局组件（主框架 + 侧边栏）
├── components   # 全局可复用组件（当前为空）
├── utils        # 工具函数（Axios 实例封装、防抖/节流）
├── directives   # 自定义指令（权限指令 v-has-perm）
├── App.vue      # 根组件（仅包含 `<router-view />`）
└── main.js      # 应用入口，注册插件与全局配置
```

**各目录职责说明**：

| 目录 | 职责 |
|------|------|
| `api/` | 封装后端接口调用，一个模块一个文件，统一从 `utils/request.js` 导入 Axios 实例 |
| `router/` | 定义静态路由表与路由守卫，路由守卫中完成动态路由的获取与注入 |
| `store/` | Pinia Store，管理全局状态（权限、菜单、用户信息等） |
| `views/` | 业务页面组件，按模块独立目录，每个模块一个 `index.vue` |
| `layout/` | 后台主布局框架（侧边栏 + 顶栏 + 内容区），包含 Sidebar 组件 |
| `components/` | 全局通用组件（当前为空，项目暂无跨模块复用组件） |
| `utils/` | 基础设施工具：Axios 封装（拦截器、Loading）、通用函数（防抖、节流） |
| `directives/` | Vue 自定义指令，用于按钮级权限控制 |

---

## 路由架构

### 静态路由

定义在 `src/router/index.js` 中，固定不依赖后端：

| 路径 | 说明 |
|------|------|
| `/login` | 登录页 |
| `/` | 主布局（Layout），默认重定向至 `/dashboard`，内含 Dashboard 子路由 |
| `/404` | 404 页面 |

### 动态路由

所有业务页面的路由**完全由后端控制**，前端不预先写死。

### 菜单来源

后端接口返回菜单树，前端根据菜单树的 `component` 字段通过 `import.meta.glob` 动态匹配 `views/` 下的 `.vue` 文件，递归转换为 Vue Router 路由格式。

### 路由注册完整流程

```
后端菜单接口 (/api/auth/menus)
    ↓
PermissionStore.generateRoutes()
    ↓ (递归调用 backendMenusToRoutes)
动态路由表 (Vue Router 格式)
    ↓
路由守卫中 router.addRoute() 逐个注入
    ↓
注入 404 通配路由 (/:pathMatch(.*)* → /404)
    ↓
执行 replace: true 重定向，触发路由重新匹配
    ↓
Sidebar 渲染 menuTree
```

**关键点**：
- 路由守卫在首次进入或刷新页面时，依次执行：获取用户信息（`/api/auth/info`）→ 获取菜单树（`/api/auth/menus`）→ 转换为路由 → 逐条 `addRoute` 注入。
- 动态路由注入后必须执行 `{ ...to, replace: true }` 重定向，否则新路由不生效。
- 白名单路由（`/login`、`/404`）不需要 token 即可访问。
- 路由切换时自动调用 `forceCloseLoading()` 清理上一个页面可能残留的 Loading。

---

## 状态管理

当前项目仅有一个 Pinia Store：**PermissionStore**（`src/store/permission.js`）。

### PermissionStore

**状态（state）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `userId` | `number/null` | 当前用户 ID |
| `username` | `string` | 当前用户名 |
| `nickname` | `string` | 当前用户昵称 |
| `roles` | `string[]` | 角色编码列表 |
| `permissions` | `string[]` | 权限码列表 |
| `menuTree` | `object[]` | 后端返回的原始菜单树 |
| `dynamicRoutes` | `object[]` | 转换后的 Vue Router 路由表 |

**行为（actions）**：

| 方法 | 说明 |
|------|------|
| `fetchUserInfo()` | 调用 `/api/auth/info`，获取用户基本信息、角色列表、权限码列表 |
| `generateRoutes()` | 调用 `/api/auth/menus`，获取菜单树并递归转换为 Vue Router 路由格式，存入 `dynamicRoutes` |
| `resetState()` | 清空所有 state 字段，用于退出登录或 token 失效时 |
| `hasPermission(value)` | 核心鉴权方法；支持传入单个权限码字符串或数组（数组为"多选一"）；`admin` 角色和 `*.*.*` 权限码直接放行 |

**注意**：当前项目中用户 token 直接存储在 `localStorage` 中（key 为 `token`），不在 Pinia Store 中维护。

---

## API 组织规范

### 模块划分

API 按业务模块拆分为独立文件，位于 `src/api/`：

| 文件 | 对应后端模块 | 接口说明 |
|------|-------------|---------|
| `auth.js` | 认证模块 | `POST /api/auth/login` |
| `user.js` | 用户管理 | 分页查询、新增、编辑、删除 |
| `role.js` | 角色管理 | 分页查询、全量查询、新增、编辑、删除、获取角色权限、保存角色权限 |

### 新增模块规范

1. 在 `src/api/` 下新建文件，命名使用小写，如 `dept.js`。
2. 文件内统一从 `@/utils/request` 导入 Axios 实例。
3. 每个接口封装为一个具名导出的函数。
4. GET 请求参数使用 `params` 字段，POST/PUT 请求参数使用 `data` 字段。
5. 登录等特殊接口需显式设置 `showLoading: false` 以避免全局 Loading 干扰。

**示例结构**：
```js
import request from '@/utils/request'

export const getXxxList = (params) => request({ url: '/api/xxx/list', method: 'get', params })
export const addXxx = (data) => request({ url: '/api/xxx', method: 'post', data })
export const updateXxx = (id, data) => request({ url: `/api/xxx/${id}`, method: 'put', data })
export const deleteXxx = (id) => request({ url: `/api/xxx/${id}`, method: 'delete' })
```

### 请求基础设施

`src/utils/request.js` 基于 Axios 封装，提供以下能力：

- **全局 Loading**：超过 200ms 的请求自动显示 Loading 遮罩，至少显示 300ms 防止闪屏；请求全部完成后自动关闭。
- **Token 注入**：请求拦截器自动从 `localStorage` 读取 token 并携带 `Authorization: Bearer` 头。
- **响应解包**：后端返回 `{ code, message, data }` 格式，响应拦截器自动剥离 `data`（`code === 200` 时），非 200 时统一 `ElMessage.error` 提示。
- **错误处理**：401 自动跳转登录页；502/504 视为后端未启动，清除 token 并跳转登录页；其他状态码给出中文提示。
- **Vite 代理**：`/api` 请求由 Vite 开发服务器代理至 `http://localhost:5014`。

---

## 页面模块说明

| 模块 | 页面位置 | 对应 API 模块 | 权限码前缀 | 说明 |
|------|---------|-------------|-----------|------|
| 登录 | `views/login/index.vue` | `api/auth.js` | — | 白名单页面，不依赖权限 |
| Dashboard | `views/dashboard/index.vue` | — | — | 静态首页，登录后默认展示 |
| 用户管理 | `views/user/index.vue` | `api/user.js`、`api/role.js`（角色字典） | `system:user:*` | 用户列表、增删改、分配角色 |
| 角色管理 | `views/role/index.vue` | `api/role.js` | `system:role:*` | 角色列表、增删改、分配权限 |
| 404 | `views/404/index.vue` | — | — | 路由未匹配时展示 |

### 权限码列表（实际使用）

- `system:user:create` — 新增用户按钮
- `system:user:edit` — 编辑用户按钮
- `system:user:delete` — 删除用户按钮
- `system:user:assignRole` — 用户分配角色区域
- `system:role:create` — 新增角色按钮
- `system:role:edit` — 编辑角色按钮
- `system:role:delete` — 删除角色按钮
- `system:role:assignPerm` — 角色分配权限按钮

**权限控制方式**：通过 `v-has-perm` 自定义指令（`src/directives/hasPerm.js`）控制按钮/元素显隐，不满足权限时直接 `removeChild` 移除 DOM 元素。

---

## 页面开发规范

以下规范从现有代码中总结得出，新增页面应保持一致：

### 列表页统一结构

1. **搜索区**：`el-input` + `el-button`（搜索/重置），回车键触发搜索（`@keyup.enter`）。
2. **操作区**：新增按钮放置在搜索区右侧（`margin-left: auto`），使用 `v-has-perm` 控制。
3. **表格区**：`el-table`，操作列 `fixed="right"`，编辑/删除按钮使用 `v-has-perm` 控制。
4. **分页区**：`el-pagination`，`page-sizes` 统一为 `[5, 10, 20, 50]`，页码改变时重置到第一页。

### 新增/编辑弹窗统一

- 使用 `el-dialog`，`v-model` 控制显隐。
- `isEdit` 标记区分新增与编辑模式，弹窗标题动态切换。
- 表单使用 `el-form`，`label-width` 统一 80~90px。
- 编辑模式下先深拷贝 `row` 数据到 `formModel`，避免污染表格。
- 提交按钮使用 `throttle` 节流（1500ms CD），防止重复提交。
- 提交成功后关闭弹窗并刷新列表。

### 删除统一

- 使用 `ElMessageBox.confirm`，类型为 `warning`。
- 提示文案包含被删除对象的名称，注明"此操作不可逆"。
- 确认后调用接口，成功后 `ElMessage.success` 提示并刷新列表。

### 消息提示统一

- 成功：`ElMessage.success`
- 警告：`ElMessage.warning`
- 错误：由 `utils/request.js` 响应拦截器统一处理，页面侧一般不额外提示；特殊场景使用 `ElMessage.error`。

### 权限控制统一

- 按钮/元素级别使用 `v-has-perm` 指令，传入权限码字符串。
- 角色管理页中 `admin` 角色（`roleCode === 'admin'` 或 `id === 1`）的编辑/删除/分配权限按钮直接 `disabled`，不以权限码控制。

### 其他约定

- 路由切换或翻页时，`pageIndex` 重置为 1。
- 删除最后一条记录后，若当前页已无数据，`pageIndex` 自动减 1。
- 表单提交使用 `throttle` 包裹（`src/utils/tool.js`），默认 CD 1.5s。
- 所有接口函数从 `api/` 目录导入，不在页面内直接调用 `request`。