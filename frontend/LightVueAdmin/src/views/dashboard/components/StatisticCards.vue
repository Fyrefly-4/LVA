<script setup>

    import {
        Reading, AlarmClock, Warning, Collection,
        Management, Box, Share, User
    } from '@element-plus/icons-vue'

    defineProps({
        summary: {
            type: Object,
            required: true,
            default: () => ({
                currentBorrowTotal: 0,
                dueSoonTotal: 0,
                overdueBorrowTotal: 0,
                historyTotal: 0,
                bookTotal: null,
                stockTotal: null,
                borrowedTotal: null,
                userTotal: null
            })
        }
    })

</script>

<template>
    <div class="statistic-cards-box">
        <el-row :gutter="20">
            <el-col :xs="24" :sm="12" :md="6" class="card-col">
                <el-card shadow="hover" class="metric-card theme-blue">
                    <div class="card-header">
                        <span class="card-title">当前在手文献</span>
                        <div class="card-icon">
                            <el-icon>
                                <Reading />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.currentBorrowTotal ?? 0" />
                    </div>
                </el-card>
            </el-col>

            <el-col :xs="24" :sm="12" :md="6" class="card-col">
                <el-card shadow="hover" class="metric-card theme-warning">
                    <div class="card-header">
                        <span class="card-title">3日内到期</span>
                        <div class="card-icon">
                            <el-icon>
                                <AlarmClock />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.dueSoonTotal ?? 0" />
                    </div>
                </el-card>
            </el-col>

            <el-col :xs="24" :sm="12" :md="6" class="card-col">
                <el-card shadow="hover" class="metric-card theme-danger">
                    <div class="card-header">
                        <span class="card-title">个人已逾期</span>
                        <div class="card-icon">
                            <el-icon>
                                <Warning />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.overdueBorrowTotal ?? 0" />
                    </div>
                </el-card>
            </el-col>

            <el-col :xs="24" :sm="12" :md="6" class="card-col">
                <el-card shadow="hover" class="metric-card theme-purple">
                    <div class="card-header">
                        <span class="card-title">累计借阅量</span>
                        <div class="card-icon">
                            <el-icon>
                                <Collection />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.historyTotal ?? 0" />
                    </div>
                </el-card>
            </el-col>

            <el-col :xs="24" :sm="12" :md="6" class="card-col" v-if="summary.bookTotal !== null">
                <el-card shadow="hover" class="metric-card theme-info">
                    <div class="card-header">
                        <span class="card-title">馆藏总书目</span>
                        <div class="card-icon">
                            <el-icon>
                                <Management />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.bookTotal" />
                    </div>
                </el-card>
            </el-col>

            <el-col :xs="24" :sm="12" :md="6" class="card-col" v-if="summary.stockTotal !== null">
                <el-card shadow="hover" class="metric-card theme-success">
                    <div class="card-header">
                        <span class="card-title">物理在馆库存</span>
                        <div class="card-icon">
                            <el-icon>
                                <Box />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.stockTotal" />
                    </div>
                </el-card>
            </el-col>

            <el-col :xs="24" :sm="12" :md="6" class="card-col" v-if="summary.borrowedTotal !== null">
                <el-card shadow="hover" class="metric-card theme-orange">
                    <div class="card-header">
                        <span class="card-title">全馆外借流转</span>
                        <div class="card-icon">
                            <el-icon>
                                <Share />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.borrowedTotal" />
                    </div>
                </el-card>
            </el-col>

            <el-col :xs="24" :sm="12" :md="6" class="card-col" v-if="summary.userTotal !== null">
                <el-card shadow="hover" class="metric-card theme-cyan">
                    <div class="card-header">
                        <span class="card-title">注册用户数</span>
                        <div class="card-icon">
                            <el-icon>
                                <User />
                            </el-icon>
                        </div>
                    </div>
                    <div class="card-body">
                        <el-statistic :value="summary.userTotal" />
                    </div>
                </el-card>
            </el-col>
        </el-row>
    </div>
</template>

<style scoped>

    .metric-card {
        --card-padding-vertical: 14px;
        --card-padding-horizontal: 18px;
        --icon-box-size: 48px;
        --icon-font-size: 19px;
        --card-radius: 8px;
        
        border: none !important;
        border-radius: var(--card-radius);
        background: #ffffff;
        transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
    }

    .metric-card :deep(.el-card__body) {
        padding: var(--card-padding-vertical) var(--card-padding-horizontal);
    }

    .card-col {
        margin-bottom: 16px;
    }

    .card-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 8px;
    }

    .card-title {
        font-size: 13px;
        color: #8c8c8c;
        font-weight: 500;
        letter-spacing: 0.3px;
    }

    .card-icon {
        font-size: var(--icon-font-size);
        width: var(--icon-box-size);
        height: var(--icon-box-size);
        border-radius: 6px;
        display: flex;
        justify-content: center;
        align-items: center;
        transition: transform 0.3s ease;
    }

    .card-body {
        text-align: left;
        display: flex;
        align-items: baseline;
    }

    :deep(.el-statistic__number) {
        font-size: 28px;
        font-weight: 700;
        color: #1f2d3d;
        line-height: 1.2;
        font-family: "Helvetica Neue", Helvetica, "PingFang SC", sans-serif;
    }

    .metric-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05) !important;
    }

    .metric-card:hover .card-icon {
        transform: scale(1.05);
    }

    .theme-blue .card-icon { background-color: #e6f7ff; color: #1890ff; }
    .theme-warning .card-icon { background-color: #fffbe6; color: #faad14; }
    .theme-danger .card-icon { background-color: #fff1f0; color: #ff4d4f; }
    .theme-purple .card-icon { background-color: #f9f0ff; color: #722ed1; }
    .theme-info .card-icon { background-color: #f5f5f5; color: #8c8c8c; }
    .theme-success .card-icon { background-color: #f6ffed; color: #52c41a; }
    .theme-orange .card-icon { background-color: #fff7e6; color: #fa8c16; }
    .theme-cyan .card-icon { background-color: #e6fffb; color: #13c2c2; }

</style>