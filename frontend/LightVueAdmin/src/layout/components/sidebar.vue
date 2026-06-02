<script setup>
    import { computed } from 'vue'
    import { useRoute } from 'vue-router'
    import usePermissionStore from '@/store/permission'

    const router = useRoute()
    const permissionStore = usePermissionStore()

    //本侧边栏展示菜单树
    const menuTree = computed(() => permissionStore.menuTree)

    const resolvePath = (parentPath, childPath) => {
        if (childPath.startsWith('/')) 
            return childPath

        const base = parentPath.startsWith('/') ? parentPath : `/${parentPath}`
        return `${base}/${childPath}`
    }

</script>

<template>
    <el-menu background-color="#304156" text-color="#fff" active-text-color="#ffd04b" 
        :default-active="router.path" router
    >
        <el-menu-item index="/dashboard">
            <el-icon><Odometer /></el-icon>
            <span>仪表盘</span>
        </el-menu-item>

       <template v-for="menu in menuTree" :key="menu.id">
            <el-sub-menu v-if="menu.children && menu.children.length > 0" :index="menu.path">
                <template #title>
                    <el-icon v-if="menu.icon">
                        <component :is="menu.icon || 'Setting'"/>
                    </el-icon>
                    <span>{{ menu.title }}</span>
                </template>

                <el-menu-item v-for="child in menu.children" :key="child.id"
                    :index="resolvePath(menu.path, child.path)"
                >
                    <el-icon>
                        <component :is="child.icon || 'Menu' " />
                    </el-icon>
                    <span>{{ child.title }}</span>
                </el-menu-item>
            </el-sub-menu>

            <el-menu-item v-else :index="menu.path">
                <el-icon>
                    <component :is="menu.icon || 'Document'" />
                </el-icon>
                <span>{{ menu.template }}</span>
            </el-menu-item>            
       </template>
        
    </el-menu>
</template>

<style scoped>

</style>