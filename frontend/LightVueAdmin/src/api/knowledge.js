import request from '@/utils/request'

/**
 * 1. 条件分页获取文献资产列表
 * @param {Object} params -筛选参数
 * @param {number} params.pageIndex -当前页码(默认1)
 * @param {number} params.pageSize -每页条数(默认10)
 * @param {string} [params.keyword] -按书名/ISBN模糊搜索
 * @param {string} [params.category] -分类精确筛选
 * @returns {Promise} 扁平分页响应体 { total: 0, items: [] }
 */ 
export const getBookList = (params) => {
    return request({
        url: '/api/knowledge/book/list',
        method: 'get',
        params
    })
}


/**
 * 2. 新增文献资产
 * @param {Object} data -文献信息
 * @param {string} data.title -文献名称
 * @param {string} data.isbn - ISBN 编号 (全局统一)
 * @param {string} [data.category] - 文档分类
 * @param {number} [data.price] - 价格
 * @param {number} [data.stock] - 库存数量
 * @param {number} [data.status] - 状态 (1:正常流转, 0:盘点维护)
 */ 
export const createBook = (data) => {
    return request({
        url: '/api/knowledge/book',
        method: 'post',
        data 
    })
}


/**
 * 3. 修改文献资产
 * @param {number|string} id - 文献主键 ID
 * @param {Object} data - 覆盖修改的文献信息
 */
export const updateBook = (id, data) => {
    return request({
        url: `/api/knowledge/book/${id}`,
        method: 'put',
        data
    })
}


/**
 * 4. 删除文献资产 (下架)
 * @param {number|string} id - 文献主键 ID
 */
export const deleteBook = (id) => {
    return request({
        url: `/api/knowledge/book/${id}`,
        method: 'delete'
    })
}


/**
 * 5. 批量流转指派借阅 
 * @param {Object} data - 指派 Payload
 * @param {number} data.userId - 目标借阅人 ID
 * @param {Array<number>} data.bookIds - 批量借阅的文献 ID 数组
 * @param {number} data.borrowDays - 借阅天数
 */
export const borrowBooks = (data) => {
    return request({
        url: '/api/knowledge/borrow',
        method: 'post',
        data
    })
}


/**
 * 6. 办理资产归还入库
 * @param {number|string} logId - 借阅日志记录 ID
 */
export const returnBook = (logId) => {
    return request({
        url: `/api/knowledge/return/${logId}`,
        method: 'post'
    })
}


/**
 * 7. 获取流转审计历史日志
 * @param {Object} params - 过滤参数
 * @param {number} params.pageIndex - 当前页码
 * @param {number} params.pageSize - 每页条数
 * @param {number} [params.logStatus] - 状态筛选 (0:流转中, 1:已归还, 2:逾期未还)
 */
export const getBorrowLogs = (params) => {
    return request({
        url: '/api/knowledge/log/list',
        method: 'get',
        params
    })
}

// ——— 以下为 普通用户自助流转接口 (仅需 JWT 认证) ───

/**
 * 8. 普通用户自助借阅文献资产
 * @param {Object} data - 自助借阅 Payload
 * @param {Array<number>} data.bookIds - 拟接文献 ID 列表
 * @param {number} data.borrowDays - 借阅天数 (必须 > 0) 
 */
export const selfBorrowBooks = (data) => {
    return request({
        url: '/api/knowledge/borrow/self',
        method: 'post',
        data
    })
}


/**
 * 9. 获取当前用户个人借阅历史
 * @param {Object} params - 过滤参数
 * @param {number} params.pageIndex - 当前页码
 * @param {number} params.pageSize - 每条页数
 * @param {number} [params.logStatus] - 状态筛选 (0:流转中, 1:已归还, 2:逾期未还)
 * @returns {Promise} 分页响应体 { total: 0, items: [] }
 */
export const getMyBorrowLogs = (params) => {
    return request({
        url: '/api/knowledge/log/my-list',
        method: 'get',
        params
    })
}


/**
 * 10. 普通用户自助归还文献资产
 * @param {number|string} logId - 流转日志 ID
 */
export const selfReturnBook = (logId) => {
    return request({
        url: `/api/knowledge/return/self/${logId}`,
        method: 'post'
    })
}