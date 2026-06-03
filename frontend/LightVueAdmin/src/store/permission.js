import { defineStore } from 'pinia'
import  service  from '@/utils/request'

//把后端返回的字符串路径，动态映射为 Vite 的 import 函数
const modules = import.meta.glob('/src/views/**/*.vue')

function loadView(component) {
    if (component == 'Layout') {
        return () => import('@/layout/index.vue')
    }

    //补全后端路径，匹配vite的glob模式
    const fullPath = `/src/${component}`

    if (modules[fullPath]) {
        return modules[fullPath]
    } else {
        console.error(`未找到对应组件文件：${fullPath}，请检查后端配置或本地文件是否存在`)
        return () => import('@/views/404/index.vue') // 返回一个默认的 404 组件, 防止系统崩溃
    }
}

//递归后端菜单树，转换为前端路由格式
function backendMenusToRoutes(menus, parentPath = '') {
    return menus.map(item => {
        //拼接路径
        let currentPath = item.path
        if (!currentPath.startsWith('/')) {
            currentPath = parentPath ? `${parentPath}/${currentPath}` : `/${currentPath}`
        }

        const route = {
            path: currentPath,
            name: `DynamicRoute-${item.id}`,
            component: item.component ? loadView(item.component) : null,
            meta: {
                title: item.title,
                icon: item.icon,
                permCode: item.permCode
            }
        }
        //递归处理子菜单
        if (item.children && item.children.length > 0) {
            route.children = backendMenusToRoutes(item.children, currentPath)
        }
        return route
    })
}

const usePermissionStore = defineStore('permission', {
    state: () => ({
        userId: null,
        username: '',
        nickname: '',
        roles: [], //角色列表
        permissions: [], //权限列表
        menuTree: [], //后端返回的菜单树
        dynamicRoutes: [] //转换后的动态路由表
    }),
    

    actions:{
        //1. 抓取用户信息与权限码
        async fetchUserInfo() {
            try {
                const res = await service({
                    url: '/api/auth/info',
                    method: 'get'
                })
                
                this.userId = res.userId
                this.username = res.username
                this.nickname = res.nickname
                this.roles = res.roles || []
                this.permissions = res.permissions || []
                return res

            } catch (error) {
                this.resetState()
                throw error
            }
        },

        //2.抓取后端动态菜单树并转化为前端路由
        async generateRoutes() {
            try {
                const res = await service({
                    url: '/api/auth/menus',
                    method: 'get'
                })
                this.menuTree = res || []

                this.dynamicRoutes = backendMenusToRoutes(this.menuTree)
                return this.dynamicRoutes

            } catch (error) {
                console.error('生成动态路由失败:', error)
                return []
            }
        },
        
        //3. 洗白本地状态
        resetState() {
            this.userId = null
            this.username = ''
            this.nickname = ''
            this.roles = []
            this.permissions = []
            this.menuTree = []
            this.dynamicRoutes = []
        },

        //4. 核心鉴权方法：检查当前用户是否拥有某个或某些权限
        hasPermission(value) {
            // 放行超级管理员或拥有通配符的用户
            if (this.roles.includes('admin') || this.permissions.includes('*.*.*')) {
                return true
            }

            if(!value) return true

            //如果传进来数组, 多选一
            if (Array.isArray(value)) {
                return value.some(perm => this.permissions.includes(perm))
            }
            //如果传进单个字符串
            return this.permissions.includes(value)
        }
    }
    
})

export default usePermissionStore