<script setup>

    import { ref, reactive, onMounted } from 'vue';
    import { getRoleList, addRole, updateRole, deleteRole } from '@/api/role';
    import { ElMessage, ElMessageBox } from 'element-plus';
    import { debounce, throttle } from '@/utils/tool';

    //角色表格数据和条数
    const tableData = ref([])
    const total = ref(0)

    const queryParams = reactive({
        pageIndex: 1,
        pageSize: 10
    })

    //获取角色列表
    const fetchRoleList = async () => {
        try {
            const res = await getRoleList(queryParams)
            tableData.value = res.items || []
            total.value = res.total || 0

        } catch (error) {
            console.log('获取角色列表失败', error)
        }
    }

    onMounted(() => {
        fetchRoleList()
    })

</script>

<template>

    <div class="role-container">
        <el-card shadow="never">
            <template #header>
                <div class="card-header">
                    <span>角色管理模块</span>
                    <el-button type="primary" size="small">新增角色</el-button>
                </div>
            </template>

            <p>沙箱数据已搭建, 等待数据注入...</p>
        </el-card>
    </div>

</template>

<style scoped>

    .role-container {
        padding: 20px;
    }

    .card-header{
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

</style>