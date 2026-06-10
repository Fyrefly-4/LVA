<script setup>

    import { ref, reactive, onMounted } from 'vue'
    import { ElMessage, ElMessageBox } from 'element-plus'
    import { getMyBorrowLogs, selfReturnBook } from '@/api/knowledge'
    import { throttle } from '@/utils/tool' 

    const tableData = ref([])
    const total = ref(0)
    const loading = ref(false)

    const queryParams = reactive({
        pageIndex: 1,
        pageSize: 10,
        logStatus: '' 
    })

    /**
     * 异步拉取当前登录用户的个人借阅历史账本
     */
    const fetchBorrowLogs = async () => {
        loading.value = true
        try {
            const res = await getMyBorrowLogs(queryParams)
            
            tableData.value = res.items || []
            total.value = res.total || 0
        } catch (error) {
            console.error('加载个人借阅账本失败:', error)
        } finally {
            loading.value = false
        }
    }

    onMounted(() => {
        fetchBorrowLogs()
    })

    const handleStatusChange = () => {
        queryParams.pageIndex = 1
        fetchBorrowLogs()
    }

    const handleSizeChange = (val) => {
        queryParams.pageSize = val
        queryParams.pageIndex = 1
        fetchBorrowLogs()
    }

    const handleCurrentChange = (val) => {
        queryParams.pageIndex = val
        fetchBorrowLogs()
    }

    /**
     * 发起自助归还事务提交
     */
    const doReturnSubmit = async (logId) => {
        try {
            await ElMessageBox.confirm(
                '确认要将该文献资产申请自助归还入库吗？',
                '自助归还确认',
                {
                    confirmButtonText: '确定归还',
                    cancelButtonText: '取消',
                    type: 'info'
                }
            )

            loading.value = true

            await selfReturnBook(logId)
            
            ElMessage.success('文献资产已安全归还入库，账本核销完毕！')

            await fetchBorrowLogs()

        } catch (error) {
            if (error === 'cancel' || error && error.message === 'cancel') return

            console.error('自助归还流转异常详情:', error)
            
        } finally {
            loading.value = false
        }
    }

    const handleReturnClick = throttle(doReturnSubmit, 1500)

</script>

<template>
    <div class="personal-borrow-container" style="padding: 20px;">
        <h3 class="page-title" style="margin-bottom: 20px;">个人文献借阅与流转历史中心</h3>

        <div class="search-bar" style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 16px; background: #fff; padding: 12px; border-radius: 4px;">
            <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 12px;">
                <el-radio-group v-model="queryParams.logStatus" @change="handleStatusChange">
                    <el-radio-button :value="''">全部流转记录</el-radio-button>
                    <el-radio-button :value="0">流转中</el-radio-button>
                    <el-radio-button :value="1">已归还入库</el-radio-button>
                    <el-radio-button :value="2">逾期未还</el-radio-button>
                </el-radio-group>
            </div>
            
            <div style="display: flex; align-items: center; gap: 12px;">
                <el-button type="primary" @click="fetchBorrowLogs">刷新我的日志流</el-button>
            </div>
        </div>

        <div class="table-wrapper" style="margin-top: 15px;">
            <el-table :data="tableData" v-loading="loading" border style="width: 100%;">
                <el-table-column prop="id" label="流水号 (LogID)" width="110" align="center" />
                
                <el-table-column prop="bookTitle" label="借阅文献名称" min-width="220" show-overflow-tooltip>
                    <template #default="scope">
                        <span v-if="scope.row.bookTitle">{{ scope.row.bookTitle }}</span>
                        <span v-else style="color: #f56c6c; font-style: italic;">
                            该文献资产已被管理员下架销毁
                        </span>
                    </template>
                </el-table-column>

                <el-table-column prop="isbn" label="ISBN 编号" width="150" />
                <el-table-column prop="borrowTime" label="借阅出库时间" width="170" align="center" />
                <el-table-column prop="returnTime" label="应还截止时间" width="170" align="center" />
                
                <el-table-column prop="actualReturnTime" label="实际归还时间" width="170" align="center">
                    <template #default="scope">
                        <span>{{ scope.row.actualReturnTime || '--' }}</span>
                    </template>
                </el-table-column>

                <el-table-column label="当前流转状态" width="130" align="center">
                    <template #default="scope">
                        <el-tag v-if="scope.row.logStatus === 0" type="primary" effect="plain">流转中</el-tag>
                        <el-tag v-else-if="scope.row.logStatus === 1" type="success" effect="plain">已归还</el-tag>
                        <el-tag v-else-if="scope.row.logStatus === 2" type="danger" effect="dark">逾期未还</el-tag>
                    </template>
                </el-table-column>

                <el-table-column label="流转操作" width="130" fixed="right" align="center">
                    <template #default="scope">
                        <el-button
                            v-if="scope.row.logStatus === 0 || scope.row.logStatus === 2"
                            type="success"
                            size="small"
                            @click="handleReturnClick(scope.row.id)"
                        >
                            自助归还
                        </el-button>
                        <span v-else style="color: #909399; font-size: 13px;">流转完毕</span>
                    </template>
                </el-table-column>
            </el-table>
        </div>

        <div class="pagination-container" style="margin-top: 15px; display: flex; justify-content: flex-end;">
            <el-pagination
                v-model:current-page="queryParams.pageIndex"
                v-model:page-size="queryParams.pageSize"
                :page-sizes="[10, 20, 50]"
                layout="total, sizes, prev, pager, next, jumper"
                :total="total"
                @size-change="handleSizeChange"
                @current-change="handleCurrentChange"
            />
        </div>
    </div>
</template>

<style scoped>

</style>