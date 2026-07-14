<!-- innovayse-sheets/client/components/toolbar/SheetToolbar.vue -->
<script setup lang="ts">
import { computed } from 'vue'
import type { Cell, CellWrite } from '../../composables/useSheetApi'

interface CellFormat {
  bold?: boolean
  italic?: boolean
  color?: string
  backgroundColor?: string
  align?: 'left' | 'center' | 'right'
}

const props = defineProps<{
  selectedCell: { row: number; col: number } | null
  cells: Cell[]
  canUndo: boolean
  canRedo: boolean
}>()

const emit = defineEmits<{
  (e: 'cell-commit', write: CellWrite): void
  (e: 'undo'): void
  (e: 'redo'): void
}>()

const currentFormat = computed<CellFormat>(() => {
  if (!props.selectedCell) return {}
  const cell = props.cells.find(c => c.row === props.selectedCell!.row && c.col === props.selectedCell!.col)
  if (!cell?.formatJson) return {}
  try {
    return JSON.parse(cell.formatJson) as CellFormat
  } catch {
    return {}
  }
})

const currentRawValue = computed(() => {
  if (!props.selectedCell) return ''
  const cell = props.cells.find(c => c.row === props.selectedCell!.row && c.col === props.selectedCell!.col)
  return cell?.rawValue ?? ''
})

function applyFormat(patch: Partial<CellFormat>) {
  if (!props.selectedCell) return
  const next: CellFormat = { ...currentFormat.value, ...patch }
  emit('cell-commit', {
    row: props.selectedCell.row,
    col: props.selectedCell.col,
    rawValue: currentRawValue.value,
    formatJson: JSON.stringify(next)
  })
}

function toggleBold() {
  applyFormat({ bold: !currentFormat.value.bold })
}

function toggleItalic() {
  applyFormat({ italic: !currentFormat.value.italic })
}

function setAlign(align: 'left' | 'center' | 'right') {
  applyFormat({ align })
}
</script>

<template>
  <div class="flex items-center gap-1 rounded-lg border border-slate-200 bg-slate-50 px-2 py-1.5">
    <button
      data-testid="toolbar-undo"
      :disabled="!canUndo"
      class="flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-200/70 disabled:opacity-30 disabled:hover:bg-transparent"
      title="Undo"
      @click="emit('undo')"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M7.5 4.5 3 9l4.5 4.5V10.5h4.5a4 4 0 0 1 0 8H8v2h4a6 6 0 0 0 0-12H7.5V4.5Z"/></svg>
    </button>
    <button
      data-testid="toolbar-redo"
      :disabled="!canRedo"
      class="flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-200/70 disabled:opacity-30 disabled:hover:bg-transparent"
      title="Redo"
      @click="emit('redo')"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M12.5 4.5 17 9l-4.5 4.5V10.5H8a4 4 0 0 0 0 8h4v2H8a6 6 0 0 1 0-12h4.5V4.5Z"/></svg>
    </button>

    <span class="mx-1 h-5 w-px bg-slate-200" aria-hidden="true" />

    <button
      data-testid="toolbar-bold"
      :disabled="!selectedCell"
      :class="currentFormat.bold ? 'bg-sky-100 text-sky-600' : 'text-slate-500 hover:bg-slate-200/70'"
      class="flex h-7 w-7 items-center justify-center rounded-md transition-colors disabled:opacity-30 disabled:hover:bg-transparent"
      title="Bold"
      @click="toggleBold"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M6 4h5.2a3.4 3.4 0 0 1 2.4 5.8A3.6 3.6 0 0 1 12 16H6V4Zm2.2 2.1v3.2h2.9a1.6 1.6 0 0 0 0-3.2H8.2Zm0 5.3v3.5h3.4a1.75 1.75 0 0 0 0-3.5H8.2Z"/></svg>
    </button>
    <button
      data-testid="toolbar-italic"
      :disabled="!selectedCell"
      :class="currentFormat.italic ? 'bg-sky-100 text-sky-600' : 'text-slate-500 hover:bg-slate-200/70'"
      class="flex h-7 w-7 items-center justify-center rounded-md transition-colors disabled:opacity-30 disabled:hover:bg-transparent"
      title="Italic"
      @click="toggleItalic"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M8.5 4h6v1.6h-2.1l-2.4 6.8h2v1.6h-6v-1.6h2.1l2.4-6.8h-2V4Z"/></svg>
    </button>

    <span class="mx-1 h-5 w-px bg-slate-200" aria-hidden="true" />

    <button
      data-testid="toolbar-align-left"
      :disabled="!selectedCell"
      :class="currentFormat.align === 'left' ? 'bg-sky-100 text-sky-600' : 'text-slate-500 hover:bg-slate-200/70'"
      class="flex h-7 w-7 items-center justify-center rounded-md transition-colors disabled:opacity-30 disabled:hover:bg-transparent"
      title="Align left"
      @click="setAlign('left')"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M3 5h14v1.5H3V5Zm0 4h9v1.5H3V9Zm0 4h14v1.5H3V13Z"/></svg>
    </button>
    <button
      data-testid="toolbar-align-center"
      :disabled="!selectedCell"
      :class="currentFormat.align === 'center' ? 'bg-sky-100 text-sky-600' : 'text-slate-500 hover:bg-slate-200/70'"
      class="flex h-7 w-7 items-center justify-center rounded-md transition-colors disabled:opacity-30 disabled:hover:bg-transparent"
      title="Align center"
      @click="setAlign('center')"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M3 5h14v1.5H3V5Zm2.5 4h9v1.5h-9V9ZM3 13h14v1.5H3V13Z"/></svg>
    </button>
    <button
      data-testid="toolbar-align-right"
      :disabled="!selectedCell"
      :class="currentFormat.align === 'right' ? 'bg-sky-100 text-sky-600' : 'text-slate-500 hover:bg-slate-200/70'"
      class="flex h-7 w-7 items-center justify-center rounded-md transition-colors disabled:opacity-30 disabled:hover:bg-transparent"
      title="Align right"
      @click="setAlign('right')"
    >
      <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M3 5h14v1.5H3V5Zm5 4h9v1.5H8V9ZM3 13h14v1.5H3V13Z"/></svg>
    </button>

    <span class="mx-1 h-5 w-px bg-slate-200" aria-hidden="true" />

    <label
      class="group relative flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-200/70 has-[:disabled]:opacity-30 has-[:disabled]:hover:bg-transparent"
      title="Text color"
    >
      <svg viewBox="0 0 20 20" class="h-4 w-4"><path fill="currentColor" d="M8.7 3h2.6l4.3 12h-2.2l-.9-2.7H7.4L6.5 15H4.3L8.7 3Zm-.7 7.6h4l-2-5.8-2 5.8Z"/></svg>
      <span
        class="pointer-events-none absolute bottom-0.5 left-1/2 h-1.5 w-4 -translate-x-1/2 rounded-full border border-white"
        :style="{ backgroundColor: currentFormat.color ?? '#000000' }"
        aria-hidden="true"
      />
      <input
        data-testid="toolbar-text-color"
        type="color"
        :disabled="!selectedCell"
        :value="currentFormat.color ?? '#000000'"
        class="absolute inset-0 h-full w-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
        @change="applyFormat({ color: ($event.target as HTMLInputElement).value })"
      />
    </label>
    <label
      class="group relative flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-200/70 has-[:disabled]:opacity-30 has-[:disabled]:hover:bg-transparent"
      title="Fill color"
    >
      <svg viewBox="0 0 20 20" class="h-4 w-4"><path fill="currentColor" d="M12.7 2.6 10 5.3 5.4 10a2 2 0 0 0 0 2.8l4 4a2 2 0 0 0 2.8 0l5-5a2 2 0 0 0 0-2.8l-4.5-4.5v-.9Zm-.2 3.5L16.1 9.7H8.3l4.2-3.6Z"/></svg>
      <span
        class="pointer-events-none absolute bottom-0.5 left-1/2 h-1.5 w-4 -translate-x-1/2 rounded-full border border-white"
        :style="{ backgroundColor: currentFormat.backgroundColor ?? '#ffffff' }"
        aria-hidden="true"
      />
      <input
        data-testid="toolbar-fill-color"
        type="color"
        :disabled="!selectedCell"
        :value="currentFormat.backgroundColor ?? '#ffffff'"
        class="absolute inset-0 h-full w-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
        @change="applyFormat({ backgroundColor: ($event.target as HTMLInputElement).value })"
      />
    </label>
  </div>
</template>
