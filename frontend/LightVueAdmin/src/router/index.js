import { el } from 'element-plus/es/locale/index.mjs'
import { createRouter, createWebHistory } from 'vue-router'

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
          component: () => import('@/views/dashboard/index.vue')
        },
        {
          path: 'user',
          component: () => import('@/views/user/index.vue')
        },
      ]
    }
  ]
})

const whiteList = ['/login', '/404'] //白名单

//路由守卫
router.beforeEach((to, from, next) => {
  //获取通行证
  const token = localStorage.getItem('token')
  
  if (token) {
    //有token已登录的情况
    if (to.path === '/login') {
      next('/dashboard') //无需再进入登录页面
    } else {
      next() //放行
    }
  } else {
    //无token的情况
    if (whiteList.includes(to.path)) {
      next() //位于白名单中的路由，放行
    } else {
      next('/login') //重定向至登录页面
    }
  }

})

export default router