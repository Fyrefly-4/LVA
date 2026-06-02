import { el } from 'element-plus/es/locale/index.mjs'
import { createRouter, createWebHistory } from 'vue-router'
import { forceCloseLoading } from '@/utils/request'
import usePermissionStore from '@/store/permission' 

//静态公共路由
export const constantRoutes = [
  { path: '/login', component: () => import('@/views/login/index.vue') },
    {
      path: '/',
      component: () => import('@/layout/index.vue'),
      redirect: '/dashboard',
      children: [
        {
          path: 'dashboard',
          component: () => import('@/views/dashboard/index.vue'),
          meta: { title: '仪表盘', icon: 'Odometer' }
        },
      ]
    },
    { path: '/404', component: () => import('@/views/404/index.vue')}
]

const router = createRouter({
  history: createWebHistory(),
  routes: constantRoutes
})

const whiteList = ['/login', '/404'] //白名单

//路由守卫
router.beforeEach(async (to, from) => {

  //获取通行证
  const permissionStore = usePermissionStore()
  const hasRoles = permissionStore.roles && permissionStore.roles.length > 0
  const token = localStorage.getItem('token')

  //切换路由时，关闭上一个页面可能残留的Loading
  if (hasRoles || !token) {
    forceCloseLoading()
  }
  
  if (token) {
    //有token已登录的情况
    if (to.path === '/login') {
      return '/dashboard' //无需再进入登录页面
    }

    if (hasRoles) {
      return true //直接放行
    } else {
      try{
        // 第一次登录进入或刷新页面，pinia为空，触发全套动态加载流
        // 1.获取用户信息和按钮权限
        await permissionStore.fetchUserInfo()

        // 2.获取后端动态菜单树，并转化为前端路由表
        const accessRoutes = await permissionStore.generateRoutes()
        //console.log(accessRoutes)
        // 3.将动态路由逐个注入 Vue Router中
        accessRoutes.forEach(route => {
          router.addRoute(route)
        })


        // 只有当上面的静态路由、后端动态路由全部撞不上时，才会进入 404 页面
        router.addRoute({
          path: '/:pathMatch(.*)*',
          redirect: '/404'
        })

        // 4.动态注入路由后，必须执行一次带 replace: true 的重定向
        // 这样可以中断当前的导航并引发一次全新的路由匹配，确保新注入的路由生效，彻底解决刷新白屏和死锁
        return { ...to, replace: true }

      } catch (error) {    
        
        console.error('身份验证拦截：加载动态权限失败，强制洗白驱逐。', error)
        localStorage.removeItem('token')
        permissionStore.resetState()
        return '/login'
      }
    } 

  } else {
    //无token的情况
    if (whiteList.includes(to.path)) {
      //位于白名单中的路由，放行
      return true
    } else {
      return '/login' //重定向至登录页面
    }
  }

})

export default router