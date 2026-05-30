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
                    <span style="font-weight: bold; font-size: 16px;">角色管理模块</span>
                    <el-button type="primary" size="small">新增角色</el-button>
                </div>
            </template>

            <el-table :data="tableData" stripe border style="width: 100%;">
                <el-table-column prop="id" label="角色ID" width="100" align="center"/>
                <el-table-column prop="roleName" label="角色名称" width="180"/>
                
                <el-table-column prop="roleCode" label="角色编码" width="180">
                    <template #default="scope">
                        <el-tag type="warning" size="small"> {{ scope.row.roleCode }} </el-tag>
                    </template>
                </el-table-column>

                <el-table-column prop="description" label="描述说明" min-width="250" show-overflow-tooltip />

                <el-table-column label="操作" width="150" align="center" fixed="right" >
                    <template #default="scope">
                        <el-button type="primary" link size="small">编辑</el-button>
                        <el-button type="danger" link size="small">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>

            <div class="pagination-container" >
            <el-pagination v-model:current-page="queryParams.pageIndex" v-model:page-size="queryParams.pageSize" :page-sizes="[5, 10, 20, 50]"
                layout="total, sizes, prev, pager, next, jumper" :total="total" @size-change="handleSizeChange" @current-change="handleCurrentChange" />
        </div>

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

    .pagination-container{
        margin-top: 20px; 
        display: flex; 
        justify-content: flex-end
    }

</style>