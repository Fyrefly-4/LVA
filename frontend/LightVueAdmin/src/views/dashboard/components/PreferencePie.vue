<template>
  <div style="height: 200px; line-height: 200px; background: #fdf6ec; border: 1px dashed #E6A23C; border-radius: 4px; text-align: center; color: #E6A23C; margin-top: 10px;">
    🎯 结构偏好图组件：{{ title }} [施工中]
  </div>
</template>
<script setup>
defineProps({ title: String, chartData: [Array, Object], type: String })
</script>