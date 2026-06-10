<script setup>

    import { ref, onMounted, onBeforeUnmount, watch, nextTick } from 'vue'
    import * as echarts from 'echarts'
    import { debounce } from '@/utils/tool'

    const props = defineProps({
        title: {
            type: String,
            required: true
        },
        chartData: {
            type: Array,
            required: true,
            default: () => []
        },
        color: {
            type: String,
            default: '#409EFF'
        }
    })

    const chartRef = ref(null)
    let chartInstance = null

    /**
     * 构建并刷新 ECharts 配置
     */
    const initChart = () => {
        if (!chartRef.value) return

        const rawData = Array.isArray(props.chartData) ? props.chartData : []
        
        if (rawData.length === 0) return

        const xAxisData = rawData.map(item => item.date)
        const seriesData = rawData.map(item => item.count)

        if (!chartInstance) {
            chartInstance = echarts.init(chartRef.value)
        }
       
        const option = {
            grid: {
            top: '12%',
            left: '3%',
            right: '4%',
            bottom: '3%',
            containLabel: true
            },
            tooltip: {
                trigger: 'axis',
                backgroundColor: 'rgba(255, 255, 255, 0.95)',
                borderColor: '#e4e7ed',
                textStyle: {
                    color: '#303133',
                    fontSize: 13
                },
                extraCssText: 'box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);'
            },
            xAxis: {
                type: 'category',
                data: xAxisData,
                boundaryGap: false,
                axisLine: {
                    lineStyle: { color: '#dcdfe6' }
                },
                axisLabel: {
                    color: '#909399',
                    fontSize: 11
                }
            },
            yAxis: {
                type: 'value',
                splitLine: {
                    lineStyle: {
                        type: 'dashed',
                        color: '#f0f2f5'
                    }
                },
                axisLabel: {
                    color: '#909399',
                    fontSize: 11
                }
            },
            series: [
                {
                    name: props.title,
                    type: 'line',
                    data: seriesData,
                    smooth: 0.3, 
                    symbol: 'circle',
                    symbolSize: 6,
                    showSymbol: false,
                    lineStyle: {
                        width: 3,
                        color: props.color
                    },
                    itemStyle: {
                        color: props.color
                    },
                    // 区域渐变填充
                    areaStyle: {
                        color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                            { offset: 0, color: `${props.color}33` }, // 顶端 20% 透明度
                            { offset: 1, color: `${props.color}03` }  // 底端渐变至消失
                        ])
                    }
                }
            ]
        }

        chartInstance.setOption(option, true)
    }

    const doHandleResize = () => {
        if (chartInstance) {
            chartInstance.resize()
        }
    }

    const handleResize = debounce(doHandleResize, 150)

    //监听异步数据，当后端数据加载成功或改变时，重绘图表
    watch(
        () => props.chartData,
        () => {
            nextTick(() => {
                initChart()
            })
        },
        { deep: true }
    )

    onMounted(() => {
        initChart()
        window.addEventListener('resize', handleResize)
    })

    onBeforeUnmount(() => {
        window.removeEventListener('resize', handleResize)
        if (chartInstance) {
            chartInstance.dispose()
            chartInstance = null
        }
    })

</script>

<template>
    <el-card shadow="hover" class="trend-chart-card">
        <template #header>
            <div class="chart-header">
                <span class="chart-title">{{ title }}</span>
            </div>
        </template>
        <div class="chart-body">
            <div ref="chartRef" class="echarts-container"></div>
        </div>
    </el-card>
</template>

<style scoped>

    .trend-chart-card {
        border: none;
        border-radius: 8px;
    }

    .chart-header {
        display: flex;
        align-items: center;
    }

    .chart-title {
        font-size: 15px;
        font-weight: 600;
        color: #303133;
    }

    .chart-body {
        width: 100%;
    }

    .echarts-container {
        width: 100%;
        height: 320px;
        touch-action: none;
    }

    :deep(.el-card__header) {
        border-bottom: 1px solid #f0f2f5;
        padding: 16px 20px;
    }

</style>