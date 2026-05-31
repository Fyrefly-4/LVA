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

    //页数改变
    const handleSizeChange = (val) => {
        queryParams.pageSize = val
        queryParams.pageIndex = 1
        fetchRoleList()
    }

    const handleCurrentChange = (val) => {
        queryParams.pageIndex = val
        fetchRoleList()
    }

    //控制弹窗dialog显示隐藏的开关，默认隐藏
    const dialogVisible = ref(false)
    //是否为编辑模式
    const isEdit = ref(false)
    //动态弹窗标题
    const dialogTitle = ref('新增角色')

    //收集表单数据
    const formModel = ref({
        id: null,
        roleName: '',
        roleCode: '',
        description: ''
    })

    //添加角色
    const openAddDialog = () => {
        isEdit.value = false
        dialogTitle.value = '新增角色'
        //清空表单
        formModel.value = {
            id: null,
            roleName: '',
            roleCode: '',
            description: ''
        }
        //打开dialog
        dialogVisible.value = true
    }

    //点击编辑按钮
    const openEditDialog = (row) => {
        isEdit.value = true
        dialogTitle.value =  '编辑角色'
        //深拷贝
        formModel.value = JSON.parse(JSON.stringify(row))
        dialogVisible.value = true
    }

    //点击“确定”按钮
    const doSubmit = async () => {
        try {
            if(isEdit.value) {
                //修改分支
                const updatePayload = {
                    roleName: formModel.value.roleName,
                    roleCode: formModel.value.roleCode,
                    description: formModel.value.description
                }
                //调用接口，发送表单数据至后端
                await updateRole(formModel.value.id, updatePayload)

                ElMessage.success('角色修改成功')
            } else {
                //新增分支
                //调用接口，发送表单数据至后端
                await addRole(formModel.value)
                
                ElMessage.success("角色添加成功")
            }

            dialogVisible.value =false
            //调用函数，重新渲染表格
            fetchRoleList()
        } catch(error) {    
            console.log("操作失败", error)
        }
    }

    //删除角色
    const handleDelete = (row) => {
        //弹出确认框
        ElMessageBox.confirm(
            `确定要删除角色[${row.roleName}]吗？此操作不可逆！`,
            '警告',
            {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }
        ).then(async () => {
            //确认删除，调用接口函数
            try {
                await deleteRole(row.id)
                ElMessage.success("删除成功")
                
                if (tableData.value.length === 1 && queryParams.pageIndex > 1) {
                    queryParams.pageIndex--
                }
                fetchRoleList() //刷新列表
            } catch (error) {
                console.log('删除失败:', error)
                ElMessage.error('删除失败，请检查网络或权限')
            }
        }).catch(() => {
            //用户点了取消
            ElMessage.info('已取消删除')
        })
    }
    

    // 节流调用，1.5s CD
    const submitForm = throttle(doSubmit, 1500)

</script>

<template>

    <div class="role-container">
        <el-card shadow="never">
            <template #header>
                <div class="card-header">
                    <span style="font-weight: bold; font-size: 16px;">角色管理模块</span>
                    <el-button type="primary" style="margin-left: auto;" @click="openAddDialog">新增角色</el-button>
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
                        <el-button type="primary" size="small" @click="openEditDialog(scope.row)">
                            编辑
                        </el-button>

                        <el-button type="danger" size="small" @click="handleDelete(scope.row)">
                            删除
                        </el-button>
                    </template>
                </el-table-column>
            </el-table>

            <div class="pagination-container" >
                <el-pagination v-model:current-page="queryParams.pageIndex" v-model:page-size="queryParams.pageSize" :page-sizes="[5, 10, 20, 50]"
                    layout="total, sizes, prev, pager, next, jumper" :total="total" @size-change="handleSizeChange" @current-change="handleCurrentChange" />
            </div>

        </el-card>

        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="formModel" label-width="90px" style="padding-right: 20px;">
                <el-form-item label="角色名称">
                    <el-input v-model="formModel.roleName" placeholder="请输入角色名称, 如: 高级管理员" />
                </el-form-item>
                <el-form-item label="角色编码">
                    <el-input v-model="formModel.roleCode" :readonly="isEdit" placeholder="请输入角色编码, 如: admin" />
                </el-form-item>
                <el-form-item label="描述说明">
                    <el-input v-model="formModel.description" type="textarea" :rows="3" placeholder="请输入该角色的权限或职责描述" />
                </el-form-item>
            </el-form>

            <template #footer>
                <span class="dialog-footer">
                    <el-button type="primary" @click="submitForm">确定</el-button>
                    <el-button @click="dialogVisible = false">取消</el-button>
                </span>
            </template>
        </el-dialog>

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