<script setup>

    import { ref, reactive, onMounted } from 'vue';
    import { ElMessage, ElMessageBox } from 'element-plus';
    import { getBorrowLogs, returnBook, getBookList, borrowBooks } from '@/api/knowledge';
    import { getUserList } from '@/api/user'
    import { throttle } from '@/utils/tool'
    import { selfBorrowBooks } from '@/api/knowledge';

    const tableData = ref([])
    const selectedLogIds = ref([])
    const total = ref(0)
    const loading = ref(false)

    const queryParams = reactive({
        pageIndex: 1,
        pageSize: 10,
        logStatus: ''
    })

    const fetchLogList = async () => {
        loading.value = true

        try {
            const res = await getBorrowLogs(queryParams)

            tableData.value = res.items || []
            total.value = res.total || 0

        } catch (error) {
            console.error('拉取流转日志失败:', error)
        } finally {
            loading.value = false
        }
    }

    onMounted(() => {
        fetchLogList()
    })

    const handleSearch = () => {
        queryParams.pageIndex = 1
        fetchLogList()
    }

    const handleReset = () => {
        queryParams.logStatus = ''
        queryParams.pageIndex = 1
        fetchLogList()
    }

    const getStatusTagType = (status) => {
        switch (status) {
            case 0: return 'warning' // 流转中
            case 1: return 'success' // 已归还
            case 2: return 'danger'  // 逾期未还
            default: return 'info'
        }
    }

    const getStatusLabel = (status) => {
        switch (status) {
            case 0: return '流转中'
            case 1: return '已归还'
            case 2: return '逾期未还'
            default: return '未知'
        }
    }

    const handleReturnAsset = (row) => {
        ElMessageBox.confirm(
            `确认将借阅人 [${row.nickname}] 名下的文献《${row.bookTitle}》办理归还入库？`,
            '资产入库审计提示',
            {
                confirmButtonText: '办理入库',
                cancelButtonText: '取消',
                type: 'info'
            }
        ).then(async () => {
            loading.value = true
            try {
                await returnBook(row.id)

                ElMessage.success('资产归还成功, 关联物理库存已回补')
                
                fetchLogList()

            } catch (error) {
                console.error('归还入库失败:', error)
            } finally { 
                loading.value = false
            }
        }).catch(() => {
            //取消直接放行
        })
    }

    const dialogVisible = ref(false)
    const formRef = ref(null)

    const userOptions = ref([])
    const bookTransferData = ref([])

    const formModel = ref({
        userId: null,
        borrowDays: 14, //默认两周
        bookIds: []
    })

    // 指派规则校验守卫
    const formRules = reactive({
        userId:[
            { required: true, message: '必须指定流转的目标借阅用户', trigger: 'change' }
        ],
        borrowDays: [
            { required: true, message: '请输入借阅天数', trigger: 'blur' }
        ],
        bookIds: [
            { type: 'array', required: true, message: '请至少通过穿梭框选择一本文献资产', trigger: 'change' }
        ]
    })

    // 加载弹窗所需的用户列表 & 文献穿梭数据
    const loadTransferDictionaries = async () => {
        try {
            // 抓取全量用户
            const userRes = await getUserList({ pageIndex: 1, pageSize: 1000, keyword: '' })
            userOptions.value = userRes.items || []
        
            // 抓取全量文献
            const bookRes = await getBookList({ pageIndex: 1, pageSize: 1000, keyword: '', category: '' })
            const allBooks = bookRes.items || []

            bookTransferData.value = allBooks.map(book => ({
                key: book.id,
                label: `${book.title} [库存: ${book.stock}本]`,
                disabled: book.stock <= 0 || book.status !== 1
            }))
        } catch (error) {
            console.error('加载指派数据字典失败:', error)
            ElMessage.error('无法加载最新的资产配置数据字典')
        }
    }

    const openBorrowDialog = async () => {
        loading.value= true
        await loadTransferDictionaries() // 异步拉取字典
        loading.value = false

        formModel.value = {
            userId: null,
            borrowDays: 14,
            bookIds: []
        }
        dialogVisible.value = true
    }

    const doSubmit = async () => {
        if (!formRef.value) return
        
        formRef.value.validate(async (valid) => {
            if (!valid) return

            loading.value = true
            try {
                await borrowBooks(formModel.value)
                ElMessage.success('批量流转指派成功, 库存已动态扣减')
                dialogVisible.value = false
                fetchLogList()

            } catch (error) {
                console.error('流转指派失败:', error)
            } finally {
                loading.value = false
            }
        })
    }

    const submitForm = throttle(doSubmit, 1500)

    const handleSelectionChange = (selection) => {
        selectedLogIds.value = selection.map(row => row.id)
    }

    const checkSelectable = (row) => {
        return row.logStatus != 1
    }

    const handleSizeChange = (val) => {
        queryParams.pageSize = val
        queryParams.pageIndex = 1
        fetchLogList()
    }

    const handleCurrentChange = (val) => {
        queryParams.pageIndex = val
        fetchLogList()
    }

    const doHandleBatchReturn = async () => {
        if(selectedLogIds.value.length === 0) return

        try {
            await ElMessageBox.confirm(
                `确认要将这 ${selectedLogIds.value.length} 项流转记录强行核销归还入库吗？`,
                '系统审计核销警告',
                { confirmButtonText: '强制核销', cancelButtonText: '取消', type: 'warning' }
            )
            loading.value = true

            // 并发申请
            const promises = selectedLogIds.value.map(id => returnBook(id))
            await Promise.allSettled(promises)

            ElMessage.success('批量资产归还核销成功！')
            selectedLogIds.value = []
            await fetchLogList()

        } catch (error) {
            if (error?.toString() !== 'cancel') {
                console.error('批量核销失败:', error)
            }
        } finally {
            loading.value = false
        }
    }

    const handleBatchReturn = throttle (doHandleBatchReturn, 2000)

</script>

<template>
    <div class="borrow-container">
        <h3 class="page-title">资产借阅与流转历史审计中心</h3>

        <div class="search-bar">
            <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 12px;">
                <el-radio-group v-model="queryParams.logStatus" @change="handleSearch">
                    <el-radio :value="''">全部记录</el-radio>
                    <el-radio :value="0">流转中</el-radio>
                    <el-radio :value="1">已归还</el-radio>
                    <el-radio :value="2">逾期未还</el-radio>
                </el-radio-group>

                <el-button type="primary" @click="handleSearch">刷新审计流</el-button>
                <el-button @click="handleReset">重置</el-button>
            </div>

            <div style="display: flex; align-items: center; gap: 12px; flex-wrap: wrap;">
                <el-button
                    type="primary"
                    style="margin-left: auto;"
                    @click="openBorrowDialog"
                    v-has-perm="'system:knowledge:borrow'"
                >
                    批量指派借阅
                </el-button>

                <el-button
                    type="warning"
                    :disabled="selectedLogIds.length === 0"
                    @click="handleBatchReturn"
                    v-has-perm="'system:knowledge:return'"
                >
                    批量核销归还 (已选 {{ selectedLogIds.length }} 项)
                </el-button>
            </div>
        </div>

        <div class="table-wrapper">
            <el-table :data="tableData" v-loading="loading" style="width: 100%; margin-top: 15px;" border @selection-change="handleSelectionChange">
                <el-table-column type="selection" :selectable="checkSelectable" width="55" align="center" fixed="left" />
                
                <el-table-column prop="id" label="流水号 (LogID)" width="120" align="center" />
                <el-table-column prop="username" label="借阅人账号" width="130" show-overflow-tooltip />
                <el-table-column prop="nickname" label="借阅人昵称" width="130" show-overflow-tooltip />
                <el-table-column prop="bookTitle" label="指派文献资产" min-width="200" show-overflow-tooltip />
                <el-table-column prop="isbn" label="ISBN 编号" width="140" />
                <el-table-column prop="borrowTime" label="借阅指派时间" width="160" />
                <el-table-column prop="returnTime" label="应还截止时间" width="160" />
                
                <el-table-column prop="actualReturnTime" label="实际归还时间" width="160">
                    <template #default="scope">
                        <span>{{ scope.row.actualReturnTime  || '--' }}</span>
                    </template>
                </el-table-column>    

                <el-table-column prop="logStatus" label="流转状态" width="120" fixed="right" align="center">
                    <template #default="scope">
                        <el-tag :type="getStatusTagType(scope.row.logStatus)" effect="dark">
                            {{ getStatusLabel(scope.row.logStatus) }}
                        </el-tag>
                    </template>
                </el-table-column>

                <el-table-column label="流转操作" width="120" fixed="right" align="center">
                    <template #default="scope">
                        <el-button
                            type="success"
                            size="small"
                            :disabled="scope.row.logStatus === 1"
                            @click="handleReturnAsset(scope.row)"
                            v-has-perm="'system:knowledge:return'"
                        >
                            归还入库
                        </el-button>
                    </template>
                </el-table-column>
            </el-table>
        </div>

        <div class="pagination-container">
            <el-pagination
                v-model:current-page="queryParams.pageIndex"
                v-model:page-size="queryParams.pageSize"
                :page-sizes="[10, 20, 50, 100]"
                layout="total, sizes, prev, pager, next, jumper"
                :total="total"
                @size-change="handleSizeChange"
                @current-change="handleCurrentChange"
            />
        </div>

        <el-dialog v-model="dialogVisible" title="发起资产批量流转指派" width="700px" destroy-on-close>
            <el-form ref="formRef" :model="formModel" :rules="formRules" label-width="120px">

                <el-form-item label="指派目标用户" prop="userId">
                    <el-select v-model="formModel.userId" placeholder="请选择或输入搜索系统用户" filterable style="width: 100%;">
                        <el-option 
                            v-for="user in userOptions"
                            :key="user.id"
                            :label="`${user.nickname} (${user.username})`"
                            :value="user.id"
                        />
                    </el-select>
                </el-form-item>

                <el-form-item label="流转借阅时限" prop="borrowDays">
                    <el-input-number v-model="formModel.borrowDays" :min="1" :max="365" style="width: 100%;" />
                    <div style="font-size: 12px; color: #909399; margin-top: 4px;">单位（天），最长允许指派 365 天。</div>
                </el-form-item>

                <el-form-item 
                    label="挑选指派文献" 
                    prop="bookIds"
                    style="display: block; margin-top: 20px;"
                >
                    <div div class="transfer-responsive-wrapper" style="width: 100%; margin-top: 8px;">
                        <el-transfer
                            v-model="formModel.bookIds"
                            :data="bookTransferData"
                            :titles="['可指派文献仓', '已选中指派队列']"
                            button-text
                            filter-placeholder="按书名搜索"
                            filterable
                            class="dynamic-transfer"
                        />
                    </div>
                </el-form-item>
            </el-form>

            <template #footer>
                <span class="dialog-footer">
                    <el-button type="primary" @click="submitForm">确认下发指派</el-button>
                    <el-button @click="dialogVisible = false">取消</el-button>
                </span>
            </template>
        </el-dialog>
    </div>
</template>

<style scoped>

    .borrow-container {
        padding: 20px;
        background-color: #fff;
        border-radius: 4px;
        min-height: calc(100vh - 120px);
    }

    .page-title {
        font-weight: bold;
        font-size: 16px;
        margin-bottom: 20px;
        color: #303133;
    }

    .search-bar {
        margin-bottom: 20px;
        display: flex;
        align-items: center;
        gap: 12px;
        justify-content: space-between;
        flex-wrap: wrap;
        padding: 12px;
        border-radius: 4px;
    }

    .pagination-container {
        margin-top: 20px;
        display: flex;
        justify-content: flex-end;
    }

    :deep(.el-transfer) {
        display:flex !important;
        align-items: center;
        justify-content: flex-start;
        gap:15px
    }

    :deep(.el-transfer-panel) {
        width: 220px !important;
        flex-shrink: 0;
    }

    :deep(.el-transfer-panel__label span) {
        display: inline-block;
        max-width: 170px;           
        overflow: hidden;
        text-transform: none;
        text-overflow: ellipsis;   
        white-space: nowrap;
        vertical-align: middle;
    }

    :deep(.el-transfer__buttons) {
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 10px;
    }

    :deep(.el-transfer__buttons .el-button) {
        width: 44px !important;      
        padding: 0 !important;       
        display: inline-flex;
        justify-content: center;
        align-items: center;
        margin-left: 0 !important;  
        }

    :deep(.el-transfer-panel__item) {
        padding-left: 15px;
    }

    :deep(.el-form-item) {
        align-items: flex-start;    
    }

    .dynamic-transfer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        width: 100%;
        gap: 8px;
    }

    :deep(.el-transfer-panel) {
        flex: 1; 
        min-width: 180px; 
        max-width: 45%; 
        transition: all 0.3s ease;
    }


    :deep(.el-transfer-panel__body) {
        width: 100%;
    }

    :deep(.el-transfer-panel .el-input__inner) {
        width: 100%;
    }


    :deep(.el-transfer__buttons) {
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 10px;
        padding: 0 4px;
    }

</style>