import { ref, computed } from 'vue'

export interface UndoRecord {
  row: number
  col: number
  prevRawValue: string
  prevFormatJson: string | null
  nextRawValue: string
  nextFormatJson: string | null
}

const MAX_STACK_SIZE = 20

export function useUndoRedo() {
  const undoStack = ref<UndoRecord[]>([])
  const redoStack = ref<UndoRecord[]>([])

  const canUndo = computed(() => undoStack.value.length > 0)
  const canRedo = computed(() => redoStack.value.length > 0)

  function push(record: UndoRecord) {
    undoStack.value.push(record)
    if (undoStack.value.length > MAX_STACK_SIZE) {
      undoStack.value.shift()
    }
    redoStack.value = []
  }

  function undo(): UndoRecord | null {
    const record = undoStack.value.pop()
    if (!record) return null
    redoStack.value.push(record)
    return record
  }

  function redo(): UndoRecord | null {
    const record = redoStack.value.pop()
    if (!record) return null
    undoStack.value.push(record)
    return record
  }

  function clear() {
    undoStack.value = []
    redoStack.value = []
  }

  return { canUndo, canRedo, push, undo, redo, clear }
}
