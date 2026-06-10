import axios from 'axios'
import { ElMessage, ElLoading } from 'element-plus'

let loadingInstance = null //保存全局Loading实例
let requestCount = 0 //核心计时器，避免某个请求完了直接关闭全局loading

let openTimer = null       // 专门控制 200ms 延迟显示的定时器
let loadingOpenedAt = 0    // 记录 Loading 真正显示的时间戳

const SHOW_DELAY = 200 //阈值：超过这个时间没返回数据才显示Loading
const MIN_SHOW_TIME = 300 //保底：只要要显示Loading，就至少显示300ms，防止闪屏

// 开启 Loading
const startLoading = () => {
  requestCount++
  if (requestCount === 1) {
    // 只有第一个请求发起时，才开启 200ms 的期待倒计时
    openTimer = setTimeout(() => {
      loadingOpenedAt = Date.now() // 真正点亮遮罩的那一刻，记录时间
      loadingInstance = ElLoading.service({
        lock: true,
        text: '正在加载中...',
        background: 'rgba(255, 255, 255, 0.7)'
      })
    }, SHOW_DELAY)
  }
}

// 关闭 Loading
const endLoading = () => {
  requestCount--
  if (requestCount < 0) requestCount = 0 // 防御性归零

  if (requestCount === 0) {
    // 1. 如果 200ms 倒计时还没到（接口超快），直接掐断发令枪，什么都别发生
    if (openTimer) {
      clearTimeout(openTimer)
      openTimer = null
    }

    // 2. 如果 200ms 已经过了，Loading 已经在屏幕上转起来了
    if (loadingInstance) {
      const elapsedTime = Date.now() - loadingOpenedAt // 计算它转了多久
      
      if (elapsedTime < MIN_SHOW_TIME) {
        // 如果转得不够久（比如刚转了 50ms），计算还差多少毫秒，补齐后关闭
        const remainTime = MIN_SHOW_TIME - elapsedTime
        const currentInstance = loadingInstance // 闭包锁死当前的实例引用
        loadingInstance = null // 立即清空指针，防止被后续并发请求误触
        
        setTimeout(() => {
          currentInstance.close()
        }, remainTime)
      } else {
        // 如果已经转了很久（大于300ms）了，立刻利索地关闭
        loadingInstance.close()
        loadingInstance = null
      }
    }
  }
}

//强制关闭Loading方法
export const forceCloseLoading = () => {
  requestCount = 0
  if (openTimer) {
    clearTimeout(openTimer)
    openTimer = null
  }
  if (loadingInstance) {
    loadingInstance.close()
    loadingInstance = null
  }
}

const service = axios.create({
  baseURL: '', // 留空，走 vite.config.js 的 /api 代理
  timeout: 5000
})

// 请求拦截器：自动贴上 Token
service.interceptors.request.use(
  (config) => {
    // 发请求时, 如果需要则开启Loading
    if (config.showLoading !== false) {
      startLoading()
    }

    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    if (error.config?.showLoading !== false) {
      endLoading()
    }
    return Promise.reject(error)
  }
)

// 响应拦截器：统一剥离外壳，处理错误
service.interceptors.response.use(
  (response) => {
    // 只要这个请求当初开启了 Loading，回来时就要扣减
    if (response.config?.showLoading !== false) {
      endLoading()
    }

    // 对应后端 { code, message, data } 格式
    const { code, message, data } = response.data
    if (code === 200) return data
    
    // 业务失败提示
    let errorMsg = message || '系统异常'
    switch (code) {
      case 400:
        if (!message) errorMsg = '请求参数错误'
        break

      case 401:
        if (window.location.pathname === '/login') {
          errorMsg = '用户名或密码错误'
        } else {
          errorMsg = '登录凭证已过期，请重新登录'
          redirectToLogin()
        }
        break

      case 403:
        if (!message) errorMsg = '您没有权限执行当前操作'
        break

      case 500:
        if (!message) errorMsg = '服务器内部错误'
        break
    }

    ElMessage.error(errorMsg)
    return Promise.reject(new Error(errorMsg))
  },
  (error) => {
    // 网络彻底断连、后端没起来、404/500 等全部死在这里
    // 同样，只要它当时是带 Loading 发出去的，必须扣减计数
    if (error.config?.showLoading !== false) {
      endLoading()
    }

    console.error('响应拦截器捕获到网络异常:', error)

    // 判定后端服务没启动的核心防线（涵盖：无响应体，或者 Vite 代理返回的 502/504 状态码）
    const isServerDown = !error.response || (error.response && [502, 504].includes(error.response.status))

    if (isServerDown) {
      ElMessage.error('无法连接到后端服务器，请检查后端服务是否启动！')
      
      // 强制清理本地脏 Token（解除伪登录）
      localStorage.removeItem('token')
      
      // 强行重定向到登录页
      router.push('/login')
      
      return Promise.reject(error)
    }

    // 后端虽然开着，但直接返回了非200的 HTTP 状态码
    if (error.response) {
      const status = error.response.status
      let errorMsg = ''

      switch (status) {
        case 400:
          errorMsg = '请求参数错误 (400)'
          break

        case 401:
          redirectToLogin()
          return Promise.reject(error)

        case 403:
          errorMsg = '拒绝访问：您没有当前操作权限 (403)'
          break

        case 404:
          errorMsg = '请求的资源不存在或路径错误 (404)'
          break

        case 500:
          errorMsg = '服务器内部发生错误，请联系管理员 (500)'
          break

        default:
          errorMsg = `网络异动，状态码：${status}`
      }

      ElMessage.error(errorMsg)
      return Promise.reject(error)
    }
  }
)

export default service