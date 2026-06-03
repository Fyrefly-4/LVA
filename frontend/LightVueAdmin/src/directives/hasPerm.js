import usePermissionStore from "@/store/permission";

export default {
    mounted(el, binding) {
        const { value } = binding
        const permissionStore = usePermissionStore()

        if (value) {
            const hasPerm = permissionStore.hasPermission(value)

            if (!hasPerm) {
                el.parentNode && el.parentNode.removeChild(el)
            }
        } else {
            console.error(`[v-has-perm]: 未传入有效的权限码。`)
        }
        
    }
}