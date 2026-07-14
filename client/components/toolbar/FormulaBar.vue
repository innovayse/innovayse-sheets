<!-- innovayse-sheets/client/components/toolbar/FormulaBar.vue -->
<script setup lang="ts">
import { computed } from 'vue'
import type { Cell, CellWrite } from '../../composables/useSheetApi'
import { colIndexToLabel } from '../../lib/columnLabel'

const props = defineProps<{
  selectedCell: { row: number; col: number } | null
  cells: Cell[]
}>()

const emit = defineEmits<{
  (e: 'cell-commit', write: CellWrite): void
}>()

const reference = computed(() => {
  if (!props.selectedCell) return ''
  return `${colIndexToLabel(props.selectedCell.col)}${props.selectedCell.row + 1}`
})

const rawValue = computed(() => {
  if (!props.selectedCell) return ''
  const cell = props.cells.find(c => c.row === props.selectedCell!.row && c.col === props.selectedCell!.col)
  return cell?.rawValue ?? ''
})

function onEnter(event: Event) {
  if (!props.selectedCell) return
  const target = event.target as HTMLInputElement
  emit('cell-commit', { row: props.selectedCell.row, col: props.selectedCell.col, rawValue: target.value })
}
</script>

<template>
  <div class="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-2 py-1">
    <span
      data-testid="formula-bar-reference"
      class="flex h-6 min-w-12 items-center justify-center rounded-md bg-slate-100 px-2 text-xs font-medium text-slate-500"
    >{{ reference }}</span>
    <span class="h-5 w-px bg-slate-200" aria-hidden="true" />
    <input
      data-testid="formula-bar-input"
      :value="rawValue"
      :disabled="!selectedCell"
      placeholder="Enter a value or formula"
      class="flex-1 bg-transparent py-1 text-sm text-slate-800 outline-none placeholder:text-slate-400 disabled:text-slate-300"
      @keydown.enter="onEnter"
    />
  </div>
</template>
