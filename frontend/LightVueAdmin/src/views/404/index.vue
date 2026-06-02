<script setup>
    import { useRouter } from 'vue-router'

    const router = useRouter()

    const goHome = () => {
        //重定向回仪表盘首页
        router.push('/')
    }

</script>

<template>
    <div class="not-found-container">
        <el-empty description="404 Not Found" >
            <template #default>
                <el-button type="primary" round @click="goHome">
                    返回首页
                </el-button>
            </template>
        </el-empty>
    </div>
</template>

<style scoped>
    .not-found-container {
        display: flex;
        justify-content: center;
        align-items: center;
        min-height: 80vh;
        background-color: #f5f7fa;
    }
    
</style>