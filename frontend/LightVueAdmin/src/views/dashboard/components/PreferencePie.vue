<script setup>

    import { ref, onMounted, onBeforeUnmount, watch, nextTick } from 'vue'
    import * as echarts from 'echarts'

    const props = defineProps({
        title: {
            type: String,
            default: '文件流转状态分布'
        },
        chartData: {
            type: Object,
            required: true,
            default: () => ({})
        }
    })

    const pieRef = ref(null)
    let pieInstance = null

    const initPieChart = () => {
        if (!pieRef.value) return
        
        const rawData = props.chartData
        if (!rawData) return

        let pieData = []

        if (Array.isArray(rawData)) {
            // 模式一：传过来的是数组（对应：个人借阅偏好 / 全站热点分类）
            pieData = rawData.map(item => ({
                name: item.category || '其它',
                value: item.count ?? 0
            }))
        } else if (typeof rawData === 'object') {
            //  模式二：传过来的是纯对象（对应：文件流转状态分布键值对）
            const statusMap = {
                borrowing: '借阅中',
                returned: '已归还',
                overdue: '已逾期'
            }
            pieData = Object.keys(rawData).map(key => ({
                name: statusMap[key] || key,
                value: rawData[key] ?? 0
            }))
        }

        const totalValue = pieData.reduce((sum, item) => sum + item.value, 0)
        if (totalValue === 0) return

        if (pieInstance) {
            pieInstance.dispose()
        }
        pieInstance = echarts.init(pieRef.value)

        const option = {
            tooltip: {
                trigger: 'item',
                formatter: '{b} : {c} 册 ({d}%)',
                backgroundColor: 'rgba(255, 255, 255, 0.95)',
                borderColor: '#e4e7ed',
                textStyle: { color: '#303133', fontSize: 13 },
                extraCssText: 'box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);'
            },
            legend: {
                orient: 'horizontal',
                bottom: '0',
                left: 'center',
                icon: 'circle',
                itemWidth: 10,
                itemHeight: 10,
                textStyle: { color: '#909399', fontSize: 12 }
            },
            color: ['#1890ff', '#52c41a', '#722ed1', '#fa8c16', '#ff4d4f', '#13c2c2'],
            series: [
                {
                    name: props.title,
                    type: 'pie',
                    radius: ['45%', '70%'], 
                    avoidLabelOverlap: false,
                    itemStyle: {
                        borderRadius: 6,   
                        borderColor: '#fff',
                        borderWidth: 2
                    },
                    label: { show: false, position: 'center' },
                    emphasis: {
                        label: {
                            show: true,
                            fontSize: 16,
                            fontWeight: 'bold',
                            formatter: '{b}'
                        }
                    },
                    labelLine: { show: false },
                    data: pieData 
                }
            ]
        }

        pieInstance.setOption(option)
    }

    const handleResize = () => {
        if (pieInstance) {
            pieInstance.resize()
        }
    }

    watch(
        () => props.chartData,
        () => {
            nextTick(() => {
                initPieChart()
            })
        },
        { deep: true }
    )

    onMounted(() => {
        initPieChart()
        window.addEventListener('resize', handleResize)
    })

    onBeforeUnmount(() => {
        window.removeEventListener('resize', handleResize)
        if (pieInstance) {
            pieInstance.dispose()
            pieInstance
        }
    })

</script>

<template>
    <el-card shadow="hover" class="preference-pie-card">
        <template #header>
            <div class="pie-header">
                <span> {{ title }} </span>
            </div>
        </template>

        <div class="pie-body">
            <div ref="pieRef" class="echarts-pie-container"></div>
        </div>
       
    </el-card>
</template>

<style scoped>

    .preference-pie-card {
        border: none;
        border-radius: 8px;
        height: 100%;
    }

    .pie-header {
        display: flex;
        align-items: center;
    }

    .pie-title {
        font-size: 15px;
        font-weight: 600;
        color: #303133;
    }

    .pie-body {
        width: 100%;
        display: flex;
        justify-content: center;
        align-items: center;
    }

    .echarts-pie-container {
        width: 100%;
        height: 320px; 
        touch-action: none; 
    }

    :deep(.el-card__header) {
        border-bottom: 1px solid #f0f2f5;
        padding: 16px 20px;
    }

</style>