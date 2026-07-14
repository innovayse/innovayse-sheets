<!-- innovayse-sheets/client/components/grid/SheetGrid.vue -->
<script setup lang="ts">
import { computed } from 'vue'
import type { Cell, CellWrite } from '../../composables/useSheetApi'
import { colIndexToLabel } from '../../lib/columnLabel'

interface CellFormat {
  bold?: boolean
  italic?: boolean
  color?: string
  backgroundColor?: string
  align?: 'left' | 'center' | 'right'
}

const props = defineProps<{
  cells: Cell[]
  rows: number
  cols: number
  selectedCell: { row: number; col: number } | null
}>()

const emit = defineEmits<{
  (e: 'cell-commit', write: CellWrite): void
  (e: 'cell-select', cell: { row: number; col: number }): void
}>()

const cellMap = computed(() => {
  const map = new Map<string, Cell>()
  for (const cell of props.cells) map.set(`${cell.row}-${cell.col}`, cell)
  return map
})

function displayValue(row: number, col: number): string {
  const cell = cellMap.value.get(`${row}-${col}`)
  if (!cell) return ''
  if (cell.textValue) return cell.textValue
  if (cell.error) return cell.error
  return cell.computedValue?.toString() ?? ''
}

function rawValue(row: number, col: number): string {
  return cellMap.value.get(`${row}-${col}`)?.rawValue ?? ''
}

function parseFormat(row: number, col: number): CellFormat {
  const raw = cellMap.value.get(`${row}-${col}`)?.formatJson
  if (!raw) return {}
  try {
    return JSON.parse(raw) as CellFormat
  } catch {
    return {}
  }
}

function cellStyle(row: number, col: number): Record<string, string> {
  const format = parseFormat(row, col)
  const style: Record<string, string> = {}
  if (format.bold) style['font-weight'] = 'bold'
  if (format.italic) style['font-style'] = 'italic'
  if (format.color) style['color'] = format.color
  if (format.backgroundColor) style['background-color'] = format.backgroundColor
  if (format.align) style['text-align'] = format.align
  return style
}

function isSelected(row: number, col: number): boolean {
  return props.selectedCell?.row === row && props.selectedCell?.col === col
}

function onFocus(row: number, col: number, event: Event) {
  emit('cell-select', { row, col })
  ;(event.target as HTMLInputElement).value = rawValue(row, col)
}

function onBlur(row: number, col: number, event: Event) {
  const target = event.target as HTMLInputElement
  emit('cell-commit', { row, col, rawValue: target.value })
}
</script>

<template>
  <table class="border-collapse">
    <thead>
      <tr>
        <th class="border px-1 bg-gray-100"></th>
        <th
          v-for="col in props.cols"
          :key="col"
          :data-testid="`col-header-${col - 1}`"
          class="border px-1 bg-gray-100 font-normal text-gray-600"
        >{{ colIndexToLabel(col - 1) }}</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="row in props.rows" :key="row">
        <th :data-testid="`row-header-${row - 1}`" class="border px-1 bg-gray-100 font-normal text-gray-600">{{ row }}</th>
        <td v-for="col in props.cols" :key="col" class="border px-1">
          <span class="sr-only">{{ displayValue(row - 1, col - 1) }}</span>
          <input
            :data-testid="`cell-${row - 1}-${col - 1}`"
            :value="displayValue(row - 1, col - 1)"
            :style="cellStyle(row - 1, col - 1)"
            :class="{ 'ring-2': isSelected(row - 1, col - 1) }"
            @focus="onFocus(row - 1, col - 1, $event)"
            @blur="onBlur(row - 1, col - 1, $event)"
            class="w-20 outline-none"
          />
        </td>
      </tr>
    </tbody>
  </table>
</template>
