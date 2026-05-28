import request from '@/utils/request'

/**
 * 条件分页获取用户列表
 * @param {Object} params - 包含 pageIndex, pageSize, keyword 的对象
 */

//获取用户列表
export const getUserList = (params) => request({
    url: '/api/user/list',
    method: 'get',
    params
})