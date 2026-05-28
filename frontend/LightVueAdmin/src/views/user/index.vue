<script setup>

    import { ref, onMounted } from 'vue'
    import { getUserList, addUser, deleteUser } from '@/api/user'
    import { ElMessage, ElMessageBox } from 'element-plus';

    //储存表格数据
    const tableData = ref([])

    //渲染表格
    const fetchUserList = async () => {
        try {
            const res = await getUserList()

            //拦截器已剥离外壳
            if(res && res.items) {
                tableData.value = res.items
                //ElMessage('用户数据加载成功')
            } else {
                //未剥离外壳
                tableData.value = res.data?.items || []
            }

        } catch (error) {
            console.error('获取用户列表失败', error)
        }
    }

    //页面挂载时调用
    onMounted(() => {
        fetchUserList()
    })

    //控制弹窗dialog显示隐藏的开关，默认隐藏
    const dialogVisible = ref(false)

    //收集表单数据
    const formModel = ref({
        username: '',
        nickname: '',
        password: '',
        email: '',
        status: 1,      // 默认启用
        roleIds: [2]    // 默认给普通用户角色
    })

    //添加用户
    const openAddDialog = () => {
        //先清空表单
        formModel.value = {
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

    //点击“确定”按钮
    const submitForm = async () => {
        try {
            //1.调用接口，发送表单数据至后端
            await addUser(formModel.value)
            
            ElMessage.success("用户添加成功")

            dialogVisible.value =false

            //调用函数，重新渲染表格
            fetchUserList()
        } catch(error) {    
            console.log("添加用户失败", error)
        }
    }

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

    <div class="user-container">
        <h3 style="margin-bottom: 20px;">用户管理模块</h3>
        
        <div style="margin-bottom: 15px;">
            <el-button type="primary" @click="openAddDialog">添加用户</el-button>
        </div>

        <el-table :data="tableData" style="width: 100%; border">
            <el-table-column prop="id" label="ID" width="80"/>
            <el-table-column prop="username" label="用户名" width="150"/>
            <el-table-column prop="nickname" label="昵称" width="150"/>            
            <el-table-column prop="email" label="邮箱" width="200"/>
            <el-table-column prop="createTime" label="创建时间"/>

            <el-table-column label="操作" width="120" fixed="right">
                <template #default="scope">
                    <el-button type="danger" size="small" @click="handleDelete(scope.row)">
                        删除
                    </el-button>
                </template>
            </el-table-column>
        </el-table>

        <el-dialog v-model="dialogVisible" title="新建用户" width="500px">
            <el-form :model="formModel" label-width="80px">
                <el-form-item label="用户名">
                    <el-input v-model="formModel.username" placeholder="请输入用户名" />
                </el-form-item>
                <el-form-item label="昵称">
                    <el-input v-model="formModel.nickname" placeholder="请输入昵称" />
                </el-form-item>
                <el-form-item label="密码">
                    <el-input v-model="formModel.password" type="password" placeholder="请输入密码" />
                </el-form-item>
                <el-form-item label="邮箱">
                    <el-input v-model="formModel.email" placeholder="请输入邮箱" />
                </el-form-item>
            </el-form>

            <template #footer>
                <span class="dialog"-footer>
                    <el-button type="primary" @click="submitForm">确定</el-button>
                    <el-button @click="dialogVisible = false">取消</el-button>
                </span>
            </template>
        </el-dialog>

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