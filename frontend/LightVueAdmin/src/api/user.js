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

//单条增加用户
export const addUser = (data) => request({
    url: '/api/user',
    method: 'post',
    data //POST 请求提交的数据在data中
})

//编辑用户
export const updateUser = (id, data) => request({
    url: `/api/user/${id}`,
    method: 'put',
    data //包含id和修改后的属性
})

//单条删除用户
export const deleteUser = (id) => request({
    url: `/api/user/${id}`,
    method: 'delete'
})