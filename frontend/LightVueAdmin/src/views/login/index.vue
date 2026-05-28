<script setup>

    import { loginAPI } from '@/api/auth'
    import { ElMessage } from 'element-plus'

    const handleLogin = async () => {
    try {
        const data = await loginAPI({
        username: 'admin',
        password: 'password123'
        })
        console.log('联调成功！拿到JWT令牌了:', data.token)
        localStorage.setItem('token', data.token)
        ElMessage.success('提示：前后端纯 JS 联调成功，数据握手完毕！')
    } catch (error) {
        console.error(error)
    }
    }

</script>

<template>

    <div style="padding: 100px; text-align: center;">
        <h2>MyAdmin 后台管理系统（纯JS版）骨架已就绪</h2>
        <el-button type="primary" @click="handleLogin">一键联调后端种子数据</el-button>
    </div>

</template>

<style scoped>

</style>