<!-- innovayse-sheets/client/components/home/ConfirmDialog.vue -->
<script setup lang="ts">
defineProps<{
  open: boolean
  title: string
  message: string
  danger?: boolean
}>()

const emit = defineEmits<{
  (e: 'confirm'): void
  (e: 'cancel'): void
}>()
</script>

<template>
  <div
    v-if="open"
    data-testid="confirm-dialog"
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
  >
    <div class="w-full max-w-sm rounded bg-white p-5 shadow-lg">
      <h2 class="mb-2 text-base font-semibold">{{ title }}</h2>
      <p class="mb-4 text-sm text-gray-600">{{ message }}</p>
      <div class="flex justify-end gap-2">
        <button
          data-testid="confirm-dialog-cancel"
          class="rounded px-3 py-1.5 text-sm font-medium text-gray-600 hover:bg-gray-100"
          @click="emit('cancel')"
        >
          Cancel
        </button>
        <button
          data-testid="confirm-dialog-confirm"
          :class="[
            'rounded px-3 py-1.5 text-sm font-medium text-white',
            danger ? 'bg-red-600 hover:bg-red-700' : 'bg-blue-600 hover:bg-blue-700'
          ]"
          @click="emit('confirm')"
        >
          Confirm
        </button>
      </div>
    </div>
  </div>
</template>
