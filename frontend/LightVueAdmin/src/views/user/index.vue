<script setup>

    import { ref, reactive, onMounted } from 'vue'
    import { getUserList, addUser, updateUser, deleteUser } from '@/api/user'
    import { ElMessage, ElMessageBox } from 'element-plus';
    import { debounce, throttle } from '@/utils/tool';

    //储存表格数据
    const tableData = ref([])
    const total = ref(0)

    //分页请求参数
    const queryParams = reactive({
        pageIndex: 1,
        pageSize: 10,
        keyword: ''
    })

    //控制弹窗dialog显示隐藏的开关，默认隐藏
    const dialogVisible = ref(false)

    //是否为编辑模式
    const isEdit = ref(false)
    //动态弹窗标题
    const dialogTitle = ref('新增用户')

    //渲染表格
    const fetchUserList = async () => {
        try {
            const res = await getUserList(queryParams)

            tableData.value = res.items || []
            total.value = res.total || 0

        } catch (error) {
            console.error('获取用户列表失败', error)
        }
    }

    //点击搜索按钮
    const handleSearch = () => {
        queryParams.pageIndex = 1 //搜索时从第一页开始展示
        fetchUserList()
    }

    //重置按钮点击
    const handleReset = () => {
        queryParams.keyword = ''
        queryParams.pageIndex = 1
        queryParams.pageSize = 10
        fetchUserList()
    }

    //页数改变
    const handleSizeChange = (val) => {
        queryParams.pageSize = val
        queryParams.pageIndex = 1
        fetchUserList()
    }

    const handleCurrentChange = (val) => {
        queryParams.pageIndex = val
        fetchUserList()
    }

    //页面挂载时调用
    onMounted(() => {
        fetchUserList()
    })

    //收集表单数据
    const formModel = ref({
        id: null,
        username: '',
        nickname: '',
        password: '',
        email: '',
        status: 1,      // 默认启用
        roleIds: [2]    // 默认给普通用户角色
    })

    //添加用户
    const openAddDialog = () => {
        isEdit.value = false
        dialogTitle.value = '新增用户'
        //清空表单
        formModel.value = {
            id: null,
            username: '',
            nickname: '',
            password: '',
            email: '',
            status: 1,     
            roleIds: [2]    
        }
        //打开dialog
        dialogVisible.value = true
    }

    //点击编辑按钮
    const openEditDialog = (row) => {
        isEdit.value = true
        dialogTitle.value =  '编辑用户'
        //浅拷贝，把当前行的数据克隆给表单，防止表格被直接修改
        formModel.value = { ...row }
        //先清空前端表单的密码字段，使输入框“看起来是空的”
        formModel.value.password = ''

        //加上roleIds
        if (row.roles && Array.isArray(row.roles)) {
            formModel.value.roleIds = row.roles.map(item => {
                return typeof item === 'object' ? item.id : item
            })
        } else if(!row.roleIds) {
            formModel.value.roleIds = [2]
        }

        dialogVisible.value = true
    }

    //点击“确定”按钮
    const doSubmit = async () => {
        try {
            if(isEdit.value) {
                //修改分支
                //提纯roleIds
                const cleanRoleIds = (formModel.value.roleIds || [2]).map(id => {
                    const parsed = parseInt(id, 10)
                    return isNaN(parsed) ? 2 : parsed // 如果解析失败，强行喂给它数字 2
                })    
                //对齐业务结构
                const updatePayload = {
                    nickname: formModel.value.nickname,
                    email: formModel.value.email,
                    status: formModel.value.status ?? 1,
                    roleIds: cleanRoleIds
                }
                //只有当用户在输入框里真的敲了新密码，才带上 password 字段
                if (formModel.value.password && formModel.value.password.trim() !=='') {
                    updatePayload.password = formModel.value.password
                }
                //调用接口，发送表单数据至后端
                await updateUser(formModel.value.id, updatePayload)

                ElMessage.success('用户修改成功')
            } else {
                //新增分支
                //调用接口，发送表单数据至后端
                await addUser(formModel.value)
                
                ElMessage.success("用户添加成功")
            }

            dialogVisible.value =false
            //调用函数，重新渲染表格
            fetchUserList()
        } catch(error) {    
            console.log("操作失败", error)
        }
    }

    // 节流调用，1.5s CD
    const submitForm = throttle(doSubmit, 1500)

    //删除用户
    const handleDelete = (row) => {
        //弹出确认框
        ElMessageBox.confirm(
            `确定要删除用户[${row.username}]吗？此操作不可逆！`,
            '警告',
            {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }
        ).then(async () => {
            //确认删除，调用接口函数
            try {
                await deleteUser(row.id)
                ElMessage.success("删除成功")
                fetchUserList() //刷新列表
            } catch (error) {
                console.log('删除失败:', error)
                ElMessage.error('删除失败，请检查网络或权限')
            }
        }).catch(() => {
            //用户点了取消
            ElMessage.info('已取消删除')
        })
    }

</script>

<template>

    <div class="user-container" style="padding: 20px;">
        <h3 style="font-weight: bold; font-size: 16px;">用户管理模块</h3>
        
        <div class="search-bar" style="margin-bottom: 20px; display: flex; align-items: center; gap: 10px">
            <el-input v-model="queryParams.keyword" @keyup.enter="handleSearch" placeholder="请输入用户名/昵称/邮箱" style="width: 260px" clearable @clear="handleSearch"/>
            <el-button type="primary" @click="handleSearch" >搜索</el-button>
            <el-button @click="handleReset">重置</el-button>
            
            <el-button type="primary" style="margin-left: auto;" @click="openAddDialog">添加用户</el-button>
        </div>

        <el-table :data="tableData" style="width: 100%; border">
            <el-table-column prop="id" label="ID" width="80"/>
            <el-table-column prop="username" label="用户名" width="150"/>
            <el-table-column prop="nickname" label="昵称" width="150"/>            
            <el-table-column prop="email" label="邮箱" width="200"/>

            <el-table-column prop="status" label="状态" width="100%" >
                <template #default="scope">
                    <el-tag :type="scope.row.status === 1 ? 'success' : 'danger' ">
                        {{ scope.row.status === 1 ? '正常' : '禁用' }}
                    </el-tag>
                </template>
            </el-table-column>

            <el-table-column prop="createTime" label="创建时间"/>

            <el-table-column label="操作" width="180" fixed="right">
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

        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="formModel" label-width="80px">
                <el-form-item label="用户名">
                    <el-input v-model="formModel.username" :disabled="isEdit" placeholder="请输入用户名" />
                </el-form-item>
                <el-form-item label="昵称">
                    <el-input v-model="formModel.nickname" placeholder="请输入昵称" />
                </el-form-item>
                
                <el-form-item label="密码">
                    <el-input v-model="formModel.password" type="password" show-password
                    :placeholder="isEdit ? '留空则不修改密码' : '请输入密码'" />
                </el-form-item>
                <el-form-item label="邮箱">
                    <el-input v-model="formModel.email" placeholder="请输入邮箱" />
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
    .user-container {
        padding: 20px;
        background-color: #fff;
        border-radius: 4px;
        min-height: 200px;
    }

    .pagination-container{
        margin-top: 20px; 
        display: flex; 
        justify-content: flex-end
    }
</style>