import axios from 'axios'
import { ElMessage } from 'element-plus'

const service = axios.create({
  baseURL: '', // 留空，走 vite.config.js 的 /api 代理
  timeout: 5000
})

// 请求拦截器：自动贴上 Token
service.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// 响应拦截器：统一剥离外壳，处理错误
service.interceptors.response.use(
  (response) => {
    // 对应后端 { code, message, data } 格式
    const { code, message, data } = response.data
    if (code === 200) return data
    
    // 业务失败提示
    ElMessage.error(message || '系统异常')
    if (code === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(new Error(message || 'Error'))
  },
  (error) => {
    ElMessage.error(error.message || '网络连接失败')
    return Promise.reject(error)
  }
)

export default service