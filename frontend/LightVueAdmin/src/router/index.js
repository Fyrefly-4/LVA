import { el } from 'element-plus/es/locale/index.mjs'
import { createRouter, createWebHistory } from 'vue-router'
import { forceCloseLoading } from '@/utils/request'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: () => import('@/views/login/index.vue') },
    {
      path: '/',
      component: () => import('@/layout/index.vue'),
      redirect: '/dashboard',
      children: [
        {
          path: 'dashboard',
          component: () => import('@/views/dashboard/index.vue'),
          meta: { title: '仪表盘' }
        },
        {
          path: 'user',
          component: () => import('@/views/user/index.vue'),
          meta: { title: '用户管理' }
        },
        {
          path: 'role',
          component: () => import('@/views/role/index.vue'),
          meta: { title: '角色管理' }
        }
      ]
    }
  ]
})

const whiteList = ['/login', '/404'] //白名单

//路由守卫
router.beforeEach((to, from) => {
  //切换路由时，关闭上一个页面可能残留的Loading
  forceCloseLoading()

  //获取通行证
  const token = localStorage.getItem('token')
  
  if (token) {
    //有token已登录的情况
    if (to.path === '/login') {
      return '/dashboard' //无需再进入登录页面
    }
    // 其他页面直接放行
    return true

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