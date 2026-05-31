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