import service from '@/utils/request'

/**
 * 获取首页聚合大盘数据 (权限驱动型组件视图)
 * * @description 
 * 该接口统一了管理员与普通用户的看板数据获取逻辑。
 * 内部基于当前登录用户的 JWT 自动进行组件级（Widget）权限裁剪与数据填充，禁止前端传参 userId。
 * * @returns {Promise<Object>} 返回响应包装体，其 data 属性包含 DashboardDto
 * @property {number} code - 状态码 (200 为成功)
 * @property {string} message - 提示信息
 * @property {Object} data - 首页聚合大盘核心数据体
 * * @property {Object} data.summary - 核心指标卡片数据
 * @property {number} data.summary.currentBorrowTotal - [全员] 当前流转中 (在手) 文献数
 * @property {number} data.summary.dueSoonTotal - [全员] 3天内即将到期文献数
 * @property {number} data.summary.overdueBorrowTotal - [全员] 个人已逾期未还文献数
 * @property {number} data.summary.historyTotal - [全员] 个人历史累计借阅总数
 * @property {number|null} data.summary.bookTotal - [管理员] 全馆文献总馆藏量 (无权限返回 null)
 * @property {number|null} data.summary.stockTotal - [管理员] 全馆当前剩余可用物理库存 (无权限返回 null)
 * @property {number|null} data.summary.borrowedTotal - [管理员] 当前全馆外借流转中总数 (无权限返回 null)
 * @property {number|null} data.summary.overdueTotal - [管理员] 当前全馆逾期未还总数 (无权限返回 null)
 * @property {number|null} data.summary.userTotal - [管理员] 系统当前注册总用户数 (无权限返回 null)
 * @property {number|null} data.summary.roleTotal - [管理员] 系统当前配置角色总数 (无权限返回 null)
 * * @property {Object} data.charts - 多维可视化图表数据
 * @property {Array<{date: string, count: number}>|null} data.charts.borrowTrend - [管理员] 最近30天全馆每日借阅趋势 (无权限返回 null)
 * @property {Array<{date: string, count: number}>|null} data.charts.userGrowthTrend - [管理员] 最近30天全馆每日用户增长趋势 (无权限返回 null)
 * @property {Array<{category: string, count: number}>|null} data.charts.hotCategories - [管理员] 全馆文献借阅 Top 5 分类排行 (无权限返回 null)
 * @property {{borrowing: number, returned: number, overdue: number}|null} data.charts.logStatusDistribution - [管理员] 全馆流转状态全局占比字典 (无权限返回 null)
 * @property {Array<{category: string, count: number}>} data.charts.preferenceCategories - [全员] 个人借阅偏好分类统计数据
 * * @property {Array<Object>} data.recentActivities - [管理员] 全馆流转实时情报流 (最近借阅+归还合并排序 Top 10, 无权限返回 [])
 * @property {string} data.recentActivities[].activityType - 动作类型: 'borrow' (借阅出库) / 'return' (归还入库)
 * @property {string} data.recentActivities[].time - 动作触发时间
 * @property {string} data.recentActivities[].username - 操作人账号
 * @property {string} data.recentActivities[].nickname - 操作人昵称
 * @property {string} data.recentActivities[].bookTitle - 涉及文献名称
 * * @property {Array<Object>} data.recentBorrows - [全员] 个人最近 5 条借阅记录快照
 * @property {number} data.recentBorrows[].id - 流转流水号 LogID
 * @property {string} data.recentBorrows[].bookTitle - 借阅文献名称
 * @property {string} data.recentBorrows[].borrowTime - 借阅出库时间
 * @property {string} data.recentBorrows[].returnTime - 应还截止时间
 * @property {number} data.recentBorrows[].logStatus - 个人当前流转状态 (0:流转中, 1:已归还, 2:逾期未还)
 */
export const getDashboardData = () => {
    return service({
        url: '/api/dashboard',
        method: 'get'
    })
}