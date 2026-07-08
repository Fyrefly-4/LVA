import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      // 配置 @ 符号指向 src 目录
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 3000, // 前端开发服务器运行在 3000 端口
    proxy: {
      // 拦截所有以 /api 开头的请求，并转发给后端
      '/api': {
        target: 'http://localhost:5014', // 对齐后端的 5014 端口
        changeOrigin: true
      }
    }
  }
})