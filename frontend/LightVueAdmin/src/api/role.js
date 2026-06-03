import request from '@/utils/request'

/**
 * 3.1 分页获取角色列表
 * @param {Object} params - { pageIndex: 1, pageSize: 10 }
 */
export const getRoleList = (params) => {
    return request({
        url: '/api/role/list',
        method: 'get',
        params
    })
}

/**
 * 3.1.2 全量获取角色列表（不分页）
 * URL: GET /api/role/all
 */
export const getAllRoleList = () => {
    return request({
        url: '/api/role/all',
        method: 'get'
    })
}

/**
 * 3.2 新增角色
 * @param {Object} data - { roleName: '', roleCode: '', description: '' }
 */
export const addRole = (data) => {
    return request({
        url: '/api/role',
        method: 'post',
        data
    })
}

/**
 * 3.3 修改角色
 * @param {number} id - 角色ID
 * @param {Object} data - { roleName: '', roleCode: '', description: '' }
 */
export const updateRole = (id, data) => {
    return request({
        url: `/api/role/${id}`,
        method: 'put',
        data
    })
}

/**
 * 3.4 删除角色
 * @param {number} id - 角色ID
 */
export const deleteRole = (id) => {
    return request({
        url: `/api/role/${id}`,
        method: 'delete'
    })
}

/**
 * 3.5 获取角色权限分配数据 (el-tree 回显)
 * @param {number} id - 角色 ID
 * @returns {Promise} - 返回权限树数据
 */
export const getRolePermissions = (id) => {
    return request({
        url: `/api/role/${id}/permissions`,
        method: 'get'
    })
}

/**
 * 3.6 保存角色权限分配
 * @param {number} id - 角色 ID
 * @param {number[]} menuIds - 选中的菜单 ID 数组
 * @return {Promise} - 返回保存结果
 */
export const saveRolePermissions = (id, menuIds) => {
    return request({
        url: `/api/role/${id}/permissions`,
        method: 'post',
        data: menuIds
    })
}