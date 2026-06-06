<script setup>

    import { ref, reactive, onMounted } from 'vue'
    import { ElMessage, ElMessageBox } from 'element-plus';
    import { getBookList, selfBorrowBooks, getMyBorrowLogs } from '@/api/knowledge';
    import { throttle } from '@/utils/tool'

    const tableData = ref([])
    const total = ref(0)
    const loading = ref(false)

    const selectedBookIds = ref([])

    const myBorrowedBookIds = ref([])

    const queryParams = reactive({
        pageIndex: 1,
        pageSize: 10,
        keyword: '' 
    })

    /**
     * 异步拉取大厅文献资产列表
     */
    const fetchBookList = async () => {
        loading.value = true
        try {
            // 先拉取用户已经借过的文献的记录
            const borrowRes = await getMyBorrowLogs({
                pageIndex: 1, 
                pageSize: 100, 
                logStatus: 0
            })
            if (borrowRes && borrowRes.items) {
                //提取已经借阅的 bookId
                myBorrowedBookIds.value = borrowRes.items.map(log => log.bookId)
            }

            // 拉取大厅表格
            const res = await getBookList(queryParams)

            tableData.value = res.items || []
            total.value = res.total || 0

        } catch (error) {
            console.error('拉取大厅文献资产失败:', error)
        } finally {
            loading.value = false
        }
    }

    const handleSearch = () => {
        queryParams.pageIndex = 1
        fetchBookList()
    }

    const handleReset = () => {
        queryParams.keyword = '' 
        queryParams.pageIndex = 1
        fetchBookList()
    }

    onMounted(() => {
        fetchBookList()
    })

    /**
     * 表格多选回调守卫
     * @param {Array} selection - 选中的全量行数据对象
     */
    const handleSelectionChange = (selection) => {
        selectedBookIds.value = selection.map(item => item.id)
    }

    /**
     * 核心逻辑: 发起自助借阅流转申请
     * @param {Array<number>} bookIds -拟借阅的文献 ID 数组
     */
        const doBorrowSubmit = async (bookIds) => {
            // 空值检查
            if (!bookIds || bookIds.length === 0) return

            loading.value = true
            try{
                const res = await selfBorrowBooks({
                    bookIds: bookIds,
                    borrowDays: 14
                })

                if (res && res.code === 500) {
                    ElMessage.error(res.message || '借阅失败，触发系统安全防御规则')
                    return
                }

                ElMessage({
                    message: '自助申领成功！文献已顺利流转至您的名下',
                    type: 'success',
                    duration: 2000
                })
        
                await fetchBookList()

                selectedBookIds.value = []

            } catch (error) {
                console.error('自助借阅流转失败:', error)

                const errMsg = error.response?.data?.message || '服务器内部错误，触发防占坑熔断线'
                ElMessage.error(errMsg)
            } finally {
                loading.value = false
            }
        }

        const handleBorrow = throttle(doBorrowSubmit, 1500)

</script>

<template>
    <div class="app-container">
        <div class="page-title">文献资产查阅大厅</div>

        <div class="filter-container" style="margin-bottom: 20px; display: flex; align-items: center; gap: 12px">
            <el-input 
                v-model="queryParams.keyword"
                @keyup.enter="handleSearch"
                @clear="handleSearch"
                placeholder="按文献名称 / ISBN 编号搜索..."
                style="width: 260px;"
                clearable
            />
            <el-button type="primary" @click="handleSearch">搜索</el-button>
            <el-button @click="handleReset">重置</el-button>

            <div style="margin-left: auto; display: flex; align-items: center; gap: 12px; flex-wrap: wrap; justify-content: flex-end;">
                <span style="font-size: 13px; color: #909399; display: inline-flex; align-items: center; gap: 4px; white-space: nowrap;">
                    <el-icon><InfoFilled /></el-icon>
                    勾选列表左侧方框可批量借阅
                </span>

                <el-button 
                type="warning" 
                :disabled="selectedBookIds.length === 0"
                @click="handleBorrow(selectedBookIds)"    
                >
                    批量申领自助借阅 (已选 {{ selectedBookIds.length }} 本)
                </el-button>
            </div>
        </div>

        <div class="table-wrapper">
            <el-table 
                :data="tableData"
                v-loading="loading"
                style="width: 100%; margin-top: 15px;" 
                border
                @selection-change="handleSelectionChange"
            >
                <el-table-column type="selection" width="55" align="center" fixed="left"
                    :selectable="(row) => !myBorrowedBookIds.includes(row.id) && row.stock > 0"
                />
        
                <el-table-column prop="id" label="资产ID" width="100" align="center" />
                <el-table-column prop="title" label="文献名称" min-width="220" show-overflow-tooltip />
                <el-table-column prop="isbn" label="ISBN 编号" width="160" />
                <el-table-column prop="category" label="资产类别" width="140" align="center" />
                
                <el-table-column prop="stock" label="当前物理库存" width="140" align="center">
                    <template #default="scope">
                        <el-tag :type="scope.row.stock > 0 ? 'success' : 'danger'" effect="plain">
                            {{ scope.row.stock > 0 ? `剩余 ${scope.row.stock} 本` : '物理库存罄尽' }}
                        </el-tag>
                    </template>
                </el-table-column>

                <el-table-column label="快捷操作" width="140" align="center" fixed="right">
                    <template #default="scope">
                        <el-button
                            v-if="myBorrowedBookIds.includes(scope.row.id)"
                            type="info"
                            size="small"
                            disabled
                        >
                            已在借阅中
                        </el-button>

                        <el-button
                            v-else-if="scope.row.stock <= 0 || scope.row.status !== 1"
                            type="info"
                            size="small"
                            disabled
                        >
                            无法借阅
                        </el-button>

                        <el-button v-else type="primary" size="small" 
                            @click="handleBorrow([scope.row.id])"
                        >
                            自助借阅
                        </el-button>
                    </template>
                </el-table-column>
            </el-table>
        </div>

        <div class="pagination-container" style="margin-top: 15px; display: flex; justify-content: flex-end;">
            <el-pagination 
                v-model:current-page="queryParams.pageIndex"
                v-model:page-size="queryParams.pageSize"
                :page-sizes="[5, 10, 20, 50]"
                layout="total, sizes, prev, pager, next, jumper"
                :total="total"
                @size-change="fetchBookList"
                @current-change="fetchBookList"
            />
        </div>
    </div>
</template>

<style scoped>

    .app-container {
        padding: 20px;
        background-color: #fff;
        border-radius: 4px;
    }

    .page-title {
        font-weight: bold;
        font-size: 18px;
        margin-bottom: 20px;
        color: #303133;
    }

</style>