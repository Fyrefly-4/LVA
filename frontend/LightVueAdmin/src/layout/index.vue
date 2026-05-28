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

    <div class="common-layout">
        <el-container  style="height: 100vh;">

            <el-aside width="220px" style="background-color: #304156;">

                <div style="height: 60px; line-height: 60px; color: #fff; text-align: center;
                font-weight: bold; background-color: #2b2f3a ;"
                >
                    LightVueAdmin 系统
                </div>

                <el-menu background-color="#304156" text-color="#fff" active-text-color="#ffd04b" 
                router
                >
                    <el-menu-item index="/dashboard">
                        <span>仪表盘</span>
                    </el-menu-item>

                    <el-menu-item index="/user">
                        <span>用户管理</span>
                    </el-menu-item>
                </el-menu>

            </el-aside>

        <el-container>
            <el-header style="background-color: #fff; border-bottom: 1px solid #dcdfe6;
            display: flex; justify-content: space-between; align-items: center; height: 60px;"
            >

                <div style="font-size: 16px; font-weight: bold;">
                    控制台首页
                </div>

                <div>
                    <el-dropdown>
                        <span style="cursor: pointer; display: flex; align-items: center;">
                            欢迎您，管理员
                        </span>

                        <template #dropdown>
                            <el-dropdown-menu>
                                <el-dropdown-item @click="handleLogout">退出登录</el-dropdown-item>
                            </el-dropdown-menu>
                        </template>
                    </el-dropdown>
                </div>

            </el-header>

            <el-main style="background-color: #f0f2f5;">
                <router-view />
            </el-main>

        </el-container>

        </el-container>
    </div>

</template>

<style scoped>

</style>