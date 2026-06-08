<script setup>

    const props = defineProps({
        title: {
            type: String,
            default: '系统实时动态流'
        },
        activities: {
            type: Array,
            required: true,
            default: () => []
        },
        type: {
            type: String,
            default: 'activity'
        }
    })

    const getTimelineType = (actionType) => {
        if (props.type === 'borrowSnapshot') return 'primary'

        if (actionType === 'borrow') return 'primary'
        if (actionType === 'return') return 'success'
        if (actionType === 'overdue') return 'warning'
        return 'primary' 
    }

    const tagTypeMap = {
        borrow: 'primary',
        return: 'success',
        overdue: 'danger'
    }

    const getTagType = (actionType) => {
        if (props.type === 'borrowSnapshot')
            return undefined

        return tagTypeMap[actionType]
    }

    // 时间清洗器
    const formatTime = (timeStr) => {
        if (!timeStr) return ''

        if (timeStr.length >= 16) {
            return timeStr.substring(5, 16)
        }
        return timeStr
    }

    const getActionName = (item) => {
        if (item.activityType) {
            const dict = {
                borrow: '借阅',
                return: '归还',
                overdue: '逾期'
            }
            return dict[item.activityType] || '操作'
        }
        
        if (item.borrowTime) {
            return '借阅'
        }
        return '操作'
    }

</script>

<template>
    <el-card shadow="hover" class="timeline-card">
        <template #header>
            <div class="timeline-header">
                <span class="timeline-title">{{ title }}</span>
            </div>
        </template>

        <div class="timeline-body">
            <el-empty v-if="!activities || activities.length === 0" :image-size="80" description="暂无最新动态" />
        
            <el-timeline v-else>
                <el-timeline-item
                    v-for="(item, index) in activities"
                    :key="index"
                    :timestamp="formatTime(item.time || item.borrowTime)"
                    :type="getTimelineType(item.activityType)"
                    hollow
                    class="custom-timeline-item"
                >
                    <div class="activity-item-inner">
                        <span class="user-name">{{ item.nickname || '我' }}</span>
                        <span class="split-dash">——</span>
                        <span class="book-name">《{{ item.bookTitle }}》</span>
                    
                        <el-tag :type="getTagType(item.activityType)" size="small" effect="light" class="action-tag">
                            {{ getActionName(item) }}
                        </el-tag>
                    </div>
                </el-timeline-item>
            </el-timeline>
        </div>
    </el-card>
</template>

<style scoped>

    .timeline-card {
        border: none;
        border-radius: 8px;
        height: 100%;
    }

    .timeline-header {
        display: flex;
        align-items: center;
    }

    .timeline-title {
        font-size: 15px;
        font-weight: 600;
        color: #303133;
    }

    .timeline-body {
        padding-top: 10px;
        height: 320px;
        overflow-y: auto; 
    }

    .timeline-body::-webkit-scrollbar {
        width: 5px;
    }
    .timeline-body::-webkit-scrollbar-thumb {
        background: #e4e7ed;
        border-radius: 10px;
    }

    .custom-timeline-item :deep(.el-timeline-item__timestamp) {
        font-size: 11px;
        color: #909399;
        margin-bottom: 6px;
    }

    .activity-item-inner {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: 4px;
        font-size: 13px;
        color: #606266;
        line-height: 1.5;
    }

    .user-name {
        font-weight: 600;
        color: #303133;
    }

    .action-text {
        color: #909399;
    }

    .book-name {
        color: #1890ff;
        font-weight: 500;
    }

    .action-tag {
        margin-left: auto; 
        border-radius: 4px;
    }

    :deep(.el-card__header) {
        border-bottom: 1px solid #f0f2f5;
        padding: 16px 20px;
    }

</style>