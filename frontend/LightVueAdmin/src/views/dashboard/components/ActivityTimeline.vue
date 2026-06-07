<template>
  <div style="min-height: 150px; padding: 20px; background: #fef0f0; border: 1px dashed #F56C6C; border-radius: 4px; text-align: center; color: #F56C6C; margin-top: 10px;">
    🔔 情报流/快照区：{{ title }} [施工中] - 共计 {{ activities?.length || 0 }} 条数据
  </div>
</template>
<script setup>
defineProps({ title: String, activities: Array, type: String })
</script>