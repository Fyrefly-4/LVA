<script setup>

    import { useRoute, useRouter } from 'vue-router'
    import { ElMessage } from 'element-plus'

    const router = useRouter()

    const handleLogout = () => {
        localStorage.removeItem('token')
        ElMessage.success('已退出登录')
        router.push('/login')
    }

</script>

<template>

    <div class="dashboard">
        <h3>欢迎来到后台管理系统</h3>
        <p>登录成功！这是首页目录界面</p>
        
        <el-button type="danger" @click="handleLogout" >退出登录</el-button>
    </div>

</template>

<style scoped>
    .dashboard { padding: 20px; }
</style>