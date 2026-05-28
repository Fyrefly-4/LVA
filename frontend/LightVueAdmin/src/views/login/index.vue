<script setup>

    import { reactive } from 'vue'
    import { loginAPI } from '@/api/auth'
    import { useRouter } from 'vue-router'
    import { ElMessage } from 'element-plus'

    const router = useRouter()
    const form = reactive({
        username: '',
        password: ''
    })

    const onLogin = async () => {
        //基础校验：防止用户未输入就点击登录
        if(!form.username || !form.password) {
            ElMessage.warning("请填写完整信息")
            return
        }

        try {
            //调用接口
            const data = await loginAPI(form)
            //成功后，将登录令牌存到浏览器本地缓存
            localStorage.setItem('token', data.token)
            ElMessage.success("登录成功")
            //路由跳转至首页
            router.push('/')

        } catch (error) {
            //
        }

    }


</script>

<template>

    <div class="login-container">
        <el-card>
            <h3>系统登录</h3>

            <el-form :model="form">
                <el-form-item>
                    <el-input v-model="form.username" placeholder="请输入用户名" />
                </el-form-item>
                    
                <el-form-item>
                    <el-input v-model="form.password" type="password" placeholder="请输入密码" />
                </el-form-item>
                
                <el-button type="primary" style="width: 100%" @click="onLogin" >登录</el-button>
            </el-form>
        </el-card>
    </div>

</template>

<style scoped>
    .login-container {
        height: 100vh; 
        display: flex; /* flex盒子布局 */
        justify-content: center;
        align-items: center;
        background-color: #f0f2f5;
    }
    .login-card {
        width: 450px;
    }
</style>