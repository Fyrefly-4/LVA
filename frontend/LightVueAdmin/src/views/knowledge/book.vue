<script setup>

    import { ref, reactive, onMounted } from 'vue'
    import { ElMessage, ElMessageBox } from 'element-plus'
    import { getBookList, createBook, updateBook, deleteBook } from '@/api/knowledge'
    import { throttle } from '@/utils/tool'

    // 分类数据字典常量
    const BOOK_CATEGORIES = [
        { label: '技术文献', value: '技术文献' },
        { label: '行业报告', value: '行业报告' },
        { label: '外文资料', value: '外文资料' }
    ]

    // 储存表格数据与大盘总数
    const tableData = ref([])
    const total = ref(0)
    const loading = ref(false)

    // 分页与条件查询参数
    const queryParams = reactive({
        pageIndex: 1,
        pageSize: 10,
        keyword: '',
        category: ''
    })

    // 控制弹窗
    const dialogVisible = ref(false)
    const isEdit = ref(false)
    const dialogTitle = ref('添加文献')
    const formRef = ref(null)

    // 收集表单数据
    const formModel = ref({
        id: null,
        title: '',
        isbn: '',
        category: '',
        price: 0.00,
        stock: 0,
        status: 1
    })

    // 引入拦截规则
    const formRules = reactive({
        title: [
            { required: true, message: '文献名称是必填项', trigger: 'blur' },
            { max: 100, message: '最大长度不可超过100字符', trigger: 'blur' }
        ],
        isbn: [
            { required: true, message: 'ISBN 编号是必填项', trigger: 'blur' },
            { pattern: /^[A-Z0-9-]+$/, message: '仅支持大写字母、数字与连字符', trigger: 'blur' }
        ],
        price: [
            { required: true, message: '价格不能为空', trigger: 'change' }
        ]
    })

    // 渲染表格方法
    const fetchBookList = async () => {
        loading.value = true
        try {
            const res = await getBookList(queryParams)
            // 兼容后端扁平脱壳数据返回规范
            tableData.value = res.items || []
            total.value = res.total || 0

        } catch (error) {
            console.error('获取文献列表失败', error)
        } finally {
            loading.value = false
        }
    }

    // 搜索按钮
    const handleSearch = () => {
        queryParams.pageIndex = 1
        fetchBookList()
    }

    // 重置按钮
    const handleReset = () => {
        queryParams.keyword = ''
        queryParams.category = ''
        queryParams.pageIndex = 1
        queryParams.pageSize = 10
        fetchBookList()
    }

    // 页数尺寸与当前页改变
    const handleSizeChange = (val) => {
        queryParams.pageSize = val
        queryParams.pageIndex = 1
        fetchBookList()
    }

    const handleCurrentChange = (val) => {
        queryParams.pageIndex = val
        fetchBookList()
    }

    // 页面加载挂载
    onMounted(() => {
        fetchBookList()
    })

    // 打开添加弹窗
    const openAddDialog = () => {
        isEdit.value = false
        dialogTitle.value = '添加文献'

        // 重置表单
        formModel.value = {
            id: null,
            title: '',
            isbn: '',
            category: '',
            price: 0.00,
            stock: 0,
            status: 1
        }
        dialogVisible.value = true
    }

    // 打开编辑弹窗
    const openEditDialog = async (row) => {
        isEdit.value = true
        dialogTitle.value = '编辑文献属性'

        // 利用 JSON 序列化执行深拷贝，隔离行内指针污染
        formModel.value = JSON.parse(JSON.stringify(row))

        dialogVisible.value = true
    }

    // 核心提交保存逻辑
    const doSubmit = async () => {
        if (!formRef.value) return

        // 嵌入异步表单规则守卫
        formRef.value.validate(async (valid) => {
            if (!valid) return

            try {
                if (isEdit.value) {
                    // 修改分支
                    await updateBook(formModel.value.id, formModel.value)
                    ElMessage.success('文献修改成功')
                } else {
                    // 新增分支
                    await createBook(formModel.value)
                    ElMessage.success('文献上架成功')
                }

                dialogVisible.value = false
                // 防最后一页边界塌陷逻辑
                if (tableData.value.length === 1 && queryParams.pageIndex > 1 && !isEdit.value) {
                    queryParams.pageIndex--
                }
                fetchBookList()
            } catch (error) {
                console.error('操作失败', error)
            }
        })
    }

    // 绑定 1.5s 冷却时间的节流控制器
    const submitForm = throttle(doSubmit, 1500)

    // 删除/下架文献
    const handleDelete = (row) => {
        ElMessageBox.confirm(
            `确定要下架并删除文献《${row.title}》吗？此操作不可逆！`,
            '警告',
            {
                confirmButtonText: '确定下架',
                cancelButtonText: '取消',
                type: 'warning'
            }
        ).then(async () => {
            try {
                await deleteBook(row.id)
                ElMessage.success('下架删除成功')

                // 处理单条页码塌陷
                if (tableData.value.length === 1 && queryParams.pageIndex > 1) {
                    queryParams.pageIndex--
                }
                fetchBookList()
            } catch (error) {
                console.error('删除失败:', error)
                ElMessage.error('删除失败，请检查网络或权限')
            }
        }).catch(() => {
            ElMessage.info('已取消操作')
        })
    }

</script>

<template>
  <div class="book-container"  style="padding: 20px;">
    <h3 style="font-weight: bold; font-size: 16px;">文献资产管理模块</h3>
    
    <div class="search-bar" style="margin-bottom: 20px; display: flex; align-items: center; gap: 10px">
      <el-input 
        v-model="queryParams.keyword" 
        @keyup.enter="handleSearch" 
        placeholder="请输入文献名称 / ISBN 编号" 
        style="width: 260px" 
        clearable 
        @clear="handleSearch"
      />
      
      <el-select v-model="queryParams.category" placeholder="文献分类筛选" style="width: 180px;" clearable @change="handleSearch">
        <el-option
          v-for="item in BOOK_CATEGORIES"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>

      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button @click="handleReset">重置</el-button>
      
      <el-button 
        type="primary" 
        style="margin-left: auto;" 
        @click="openAddDialog" 
        v-has-perm="'system:knowledge:create'"
      >
        添加文献
      </el-button>
    </div>

    <el-table :data="tableData" v-loading="loading" style="width: 100%; border">
      <el-table-column prop="id" label="ID" width="80"/>
      <el-table-column prop="isbn" label="ISBN 编号" width="140" />
      <el-table-column prop="title" label="文献标题" min-width="200" show-overflow-tooltip />
      <el-table-column prop="category" label="分类" width="120" />
      
      <el-table-column prop="price" label="价格" width="120">
        <template #default="scope">
          <span :style="{ fontWeight: 'bold', color: '#f56c6c' }">>
            {{ scope.row.price ? scope.row.price.toLocaleString('zh-CN', { style: 'currency', currency: 'CNY' }) : '¥0.00' }}
          </span>
        </template>
      </el-table-column>
      
      <el-table-column prop="stock" label="当前库存" width="100" align="center" />
      
      <el-table-column prop="status" label="状态" width="100%">
        <template #default="scope">
          <el-tag :type="scope.row.status === 1 ? 'success' : 'danger'">
            {{ scope.row.status === 1 ? '正常' : '维护' }}
          </el-tag>
        </template>
      </el-table-column>
      
      <el-table-column label="操作" width="180" fixed="right" align="center">
        <template #default="scope">
          <el-button 
            type="primary" 
            size="small" 
            @click="openEditDialog(scope.row)"
            v-has-perm="'system:knowledge:edit'"
          >
            编辑
          </el-button>

          <el-button 
            type="danger" 
            size="small" 
            @click="handleDelete(scope.row)"
            v-has-perm="'system:knowledge:delete'"
          >
            删除
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <div class="pagination-container">
      <el-pagination 
        v-model:current-page="queryParams.pageIndex" 
        v-model:page-size="queryParams.pageSize" 
        :page-sizes="[5, 10, 20, 50]"
        layout="total, sizes, prev, pager, next, jumper" 
        :total="total" 
        @size-change="handleSizeChange" 
        @current-change="handleCurrentChange" 
      />
    </div>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
      <el-form ref="formRef" :model="formModel" :rules="formRules" label-width="80px" :style="{ paddingRight: '10px' }">
        
        <el-form-item label="文献名称" prop="title">
          <el-input v-model="formModel.title" placeholder="请输入完整文献标题" />
        </el-form-item>
        
        <el-form-item label="ISBN" prop="isbn">
          <el-input v-model="formModel.isbn" :disabled="isEdit" placeholder="请输入 ISBN 编号" />
        </el-form-item>
        
        <el-form-item label="分类" prop="category">
          <el-select v-model="formModel.category" placeholder="请选择分类" :style="{ width: '100%' }">
            <el-option
              v-for="item in BOOK_CATEGORIES"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        
        <el-form-item label="价格" prop="price">
          <el-input-number v-model="formModel.price" :precision="2" :step="1" :min="0" :style="{ width: '100%' }" />
        </el-form-item>
        
        <el-form-item label="库存" prop="stock">
          <el-input-number v-model="formModel.stock" :min="0" :step="1" :style="{ width: '100%' }" />
        </el-form-item>
        
        <el-form-item label="状态" prop="status">
          <el-radio-group v-model="formModel.status">
            <el-radio :value="1">正常</el-radio>
            <el-radio :value="0">维护</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>

      <template #footer>
        <span class="dialog-footer">
          <el-button type="primary" @click="submitForm">确定</el-button>
          <el-button @click="dialogVisible = false">取消</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>

    .book-container {
    padding: 20px;
    background-color: #fff;
    border-radius: 4px;
    min-height: 200px;
  }

  .pagination-container {
    margin-top: 20px; 
    display: flex; 
    justify-content: flex-end;
  }

</style>