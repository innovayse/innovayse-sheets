<!-- innovayse-sheets/client/components/tabs/SheetTabs.vue -->
<script setup lang="ts">
import type { Sheet } from '../../composables/useSheetApi'

const props = defineProps<{
  sheets: Sheet[]
  currentSheetId: string
}>()

const emit = defineEmits<{
  (e: 'select-sheet', sheetId: string): void
  (e: 'create-sheet'): void
}>()
</script>

<template>
  <div class="flex items-center gap-1.5 overflow-x-auto px-2 py-1.5 border-t bg-white">
    <button
      v-for="sheet in props.sheets"
      :key="sheet.id"
      :data-testid="`tab-${sheet.id}`"
      :class="[
        'shrink-0 rounded-full px-4 py-1.5 text-sm whitespace-nowrap',
        sheet.id === props.currentSheetId ? 'bg-sky-500 text-white font-semibold' : 'bg-slate-100 text-slate-600'
      ]"
      @click="emit('select-sheet', sheet.id)"
    >{{ sheet.name }}</button>
    <button
      data-testid="add-sheet-button"
      class="shrink-0 w-6 h-6 rounded-full border border-dashed border-slate-400 text-slate-500 flex items-center justify-center ml-1"
      @click="emit('create-sheet')"
    >+</button>
  </div>
</template>
