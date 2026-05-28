<script setup>

    import { ref, onMounted } from 'vue'
    import { getUserList } from '@/api/user'
    import { ElMessage } from 'element-plus';

    //储存表格数据
    const tableData = ref([])
    //页面挂载时调用
    onMounted(async () => {
        try {
            const res = await getUserList()

            //拦截器已剥离外壳
            if(res && res.items) {
                tableData.value = res.items
                ElMessage('用户数据加载成功')
            } else {
                //未剥离外壳
                tableData.value = res.data?.items || []
            }

        } catch (error) {
            console.error('获取用户列表失败', error)
        }
    })

</script>

<template>

    <div class="user-container">
        <h3 style="margin-bottom: 20px;">用户管理模块</h3>
        
        <el-table :data="tableData" style="width: 100%; border">
            <el-table-column prop="id" label="ID" width="80"/>
            <el-table-column prop="username" label="用户名" width="150"/>
            <el-table-column prop="nickname" label="昵称" width="150"/>            
            <el-table-column prop="email" label="邮箱" width="200"/>
            <el-table-column prop="createTime" label="创建时间"/>
        </el-table>
    </div>

</template>

<style scoped>
    .uer-container {
        padding: 20px;
        background-color: #fff;
        border-radius: 4px;
        min-height: 200px;
    }
</style>