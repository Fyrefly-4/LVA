<script setup>

    import { ref, onMounted } from 'vue'
    import { ElMessage } from 'element-plus'
    import { getDashboardData } from '@/api/dashboard';

    // 引入子组件
    import StatisticCards from './components/StatisticCards.vue'
    import TrendChart from './components/TrendChart.vue'
    import PreferencePie from './components/PreferencePie.vue'
    import ActivityTimeline from './components/ActivityTimeline.vue'

    const loading = ref(true)
    const dashboardData = ref(null)

    /**
     * 单次异步清洗和分发
     */
    const initDashboard = async () => {
        try {
            loading.value = true
            const res = await getDashboardData()

            if (res && res.summary) {
                dashboardData.value = res
                
            } else {
                ElMessage.error(res.message || '仪表盘数据连接失败')
            }
        } catch (error) {
            console.error('Dashboard 载入崩溃:', error)
            ElMessage.error('读取仪表盘数据时发生异常')
        } finally {
            loading.value = false
        }
    }

    onMounted(() => {
        initDashboard()
    })

</script>

<template>
    <div class="dashboard-container"  v-loading="loading" element-loading-text="仪表盘数据加载中...">
        <template v-if="!loading && dashboardData">
            <StatisticCards :summary="dashboardData.summary"/>

            <el-row :gutter="20" class="margin-top-20">
                <el-col :xs="24" :sm="24" :md="16" v-if="dashboardData.charts.borrowTrend">
                    <TrendChart 
                        title="全站借阅趋势 (近30天)"
                        :chart-data="dashboardData.charts.borrowTrend || []"
                        color="#409EFF"
                    />
                </el-col>

                <el-col :xs="24" :sm="24" :md="dashboardData.charts.borrowTrend ? 8 : 24">
                    <PreferencePie 
                        title="我的借阅偏好分析"
                        :chart-data="dashboardData.charts.preferenceCategories || []"
                        type="preference"
                    />
                </el-col>

            </el-row>

            <el-row :gutter="20" class="margin-top-20" v-if="dashboardData.charts.userGrowthTrend">
                <el-col :xs="24" :sm="24" :md="16">
                    <TrendChart 
                        title="全站用户增长态势 (近30天)"
                        :chart-data="dashboardData.charts.userGrowthTrend || []"
                        color="#67C23A"
                    />
                </el-col>

                <el-col :xs="24" :sm="24" :md="8">
                    <PreferencePie 
                        title="全站文献流转状态分布"
                        :chart-data="dashboardData.charts.logStatusDistribution || []"
                        type="distribution"
                    />
                </el-col>
            </el-row>

            <el-row :gutter="20" class="margin-top-20">
                <el-col :xs="24" :sm="24" :md="12" v-if="dashboardData.recentActivities && dashboardData.recentActivities.length > 0">
                    <ActivityTimeline 
                        title="全站流转实时情报流"
                        :activities="dashboardData.recentActivities || []"
                        type="activity"
                    />
                </el-col>
                
                <el-col :xs="24" :sm="24" :md="dashboardData.recentBorrows && dashboardData.recentActivities.length > 0 ? 12 : 24">
                    <ActivityTimeline 
                        title="个人最近借阅快照 (Top 5)"
                        :activities="dashboardData.recentBorrows || []"
                        type="borrowSnapshot"
                    />
                </el-col>
            </el-row>

        </template>
    </div>
</template>

<style scoped>
    
    .dashboard-container {
        padding: 20px;
        min-height: calc(100vh - 84px);
        background-color: #f0f2f5;
    }

    .margin-top-20 {
        margin-top: 20px;
    }

</style>