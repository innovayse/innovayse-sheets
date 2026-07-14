<!-- innovayse-sheets/client/pages/sheets/[id].vue -->
<script setup lang="ts">
import { ref, shallowRef, watch, onMounted, onUnmounted } from 'vue'
import { useSheetApi, type Cell, type CellWrite, type Sheet } from '../../composables/useSheetApi'
import { useSharingApi } from '../../composables/useSharingApi'
import { useSheetRealtime } from '../../composables/useSheetRealtime'
import { useAuthToken } from '../../composables/useAuthToken'
import { useUndoRedo } from '../../composables/useUndoRedo'
import SheetGrid from '../../components/grid/SheetGrid.vue'
import ShareDialog from '../../components/sharing/ShareDialog.vue'
import SheetToolbar from '../../components/toolbar/SheetToolbar.vue'
import FormulaBar from '../../components/toolbar/FormulaBar.vue'
import SheetTabs from '../../components/tabs/SheetTabs.vue'

const route = useRoute()
const router = useRouter()
const api = useSheetApi(useRuntimeConfig().public.sheetsApiUrl as string)
const sharingApi = useSharingApi(useRuntimeConfig().public.sheetsApiUrl as string)
const sheetsApiUrl = useRuntimeConfig().public.sheetsApiUrl as string
const mainUrl = useRuntimeConfig().public.mainUrl as string
const { getToken, redirectToLogin } = useAuthToken()
const { canUndo, canRedo, push: pushUndo, undo: popUndo, redo: popRedo, clear: clearUndoRedo } = useUndoRedo()

const showShareDialog = ref(false)
const spreadsheetId = ref<string | null>(null)
const cells = ref<Cell[]>([])
const selectedCell = ref<{ row: number; col: number } | null>(null)
const sheets = ref<Sheet[]>([])

// useSheetRealtime captures sheetId in a closure at construction time and has
// no way to be told "the sheet changed" — switching sheets means disconnecting
// this instance and building a brand new one for the new sheet id.
const realtime = shallowRef(useSheetRealtime(`${sheetsApiUrl}/hubs/sheets`, route.params.id as string))

function onCellSelect(cell: { row: number; col: number }) {
  selectedCell.value = cell
}

async function load(sheetId: string) {
  cells.value = await api.getCells(sheetId)
}

async function loadSpreadsheetId(sheetId: string) {
  const sheet = await api.getSheet(sheetId)
  spreadsheetId.value = sheet.spreadsheetId
  return sheet.spreadsheetId
}

async function loadSheets(spreadsheetIdValue: string) {
  sheets.value = await api.listSheets(spreadsheetIdValue)
}

async function onCellCommit(write: CellWrite) {
  const sheetId = route.params.id as string
  const existing = cells.value.find(c => c.row === write.row && c.col === write.col)
  pushUndo({
    row: write.row,
    col: write.col,
    prevRawValue: existing?.rawValue ?? '',
    prevFormatJson: existing?.formatJson ?? null,
    nextRawValue: write.rawValue,
    nextFormatJson: write.formatJson ?? null
  })
  await api.writeCells(sheetId, [write])
  await load(sheetId)
}

async function onUndo() {
  const record = popUndo()
  if (!record) return
  const sheetId = route.params.id as string
  try {
    await api.writeCells(sheetId, [{ row: record.row, col: record.col, rawValue: record.prevRawValue, formatJson: record.prevFormatJson }])
    await load(sheetId)
  } catch {
    // Dropped: matches this app's existing no-dedicated-error-UI posture.
  }
}

async function onRedo() {
  const record = popRedo()
  if (!record) return
  const sheetId = route.params.id as string
  try {
    await api.writeCells(sheetId, [{ row: record.row, col: record.col, rawValue: record.nextRawValue, formatJson: record.nextFormatJson }])
    await load(sheetId)
  } catch {
    // Dropped: matches this app's existing no-dedicated-error-UI posture.
  }
}

function onKeydown(event: KeyboardEvent) {
  const isUndo = (event.ctrlKey || event.metaKey) && !event.shiftKey && event.key.toLowerCase() === 'z'
  const isRedo = (event.ctrlKey || event.metaKey) && (event.key.toLowerCase() === 'y' || (event.shiftKey && event.key.toLowerCase() === 'z'))
  if (isUndo) {
    event.preventDefault()
    onUndo()
  } else if (isRedo) {
    event.preventDefault()
    onRedo()
  }
}

function mergeUpdatedCells(updated: Cell[]) {
  for (const updatedCell of updated) {
    const index = cells.value.findIndex(c => c.row === updatedCell.row && c.col === updatedCell.col)
    if (index === -1) {
      cells.value.push(updatedCell)
    } else {
      cells.value[index] = updatedCell
    }
  }
}

async function connectRealtime(sheetId: string) {
  realtime.value = useSheetRealtime(`${sheetsApiUrl}/hubs/sheets`, sheetId)
  realtime.value.onCellsUpdated(mergeUpdatedCells)
  try {
    await realtime.value.connect()
    await realtime.value.joinSheet()
  } catch {
    // Live updates/presence are a nice-to-have — see the original onMounted
    // comment in sub-project 3's version of this file for the full rationale.
  }
}

async function loadSheetData(sheetId: string) {
  await load(sheetId)
  const spreadsheetIdValue = await loadSpreadsheetId(sheetId)
  await loadSheets(spreadsheetIdValue)
}

function onSelectSheet(sheetId: string) {
  router.push(`/sheets/${sheetId}`)
}

async function onCreateSheet() {
  if (!spreadsheetId.value) return
  const nextName = `Sheet ${sheets.value.length + 1}`
  const created = await api.createSheet(spreadsheetId.value, nextName)
  router.push(`/sheets/${created.id}`)
}

watch(
  () => route.params.id as string,
  async (newSheetId, oldSheetId) => {
    if (!newSheetId || newSheetId === oldSheetId) return
    clearUndoRedo()
    await realtime.value.disconnect()
    await loadSheetData(newSheetId)
    await connectRealtime(newSheetId)
  }
)

onMounted(async () => {
  window.addEventListener('keydown', onKeydown)

  if (!getToken()) {
    redirectToLogin(mainUrl)
    return
  }

  const sheetId = route.params.id as string
  await loadSheetData(sheetId)
  await connectRealtime(sheetId)
})

onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  realtime.value.disconnect()
})
</script>

<template>
  <div class="flex items-center gap-2">
    <button @click="showShareDialog = !showShareDialog">Share</button>
    <div v-if="realtime.presence.value.length" class="flex gap-1">
      <span
        v-for="userId in realtime.presence.value"
        :key="userId"
        class="w-6 h-6 rounded-full bg-sky-500 text-white text-xs flex items-center justify-center"
        :title="userId"
      >{{ userId.slice(0, 2).toUpperCase() }}</span>
    </div>
  </div>
  <ShareDialog v-if="showShareDialog && spreadsheetId" :spreadsheet-id="spreadsheetId" :sharing-api="sharingApi" />
  <SheetToolbar
    :selected-cell="selectedCell"
    :cells="cells"
    :can-undo="canUndo"
    :can-redo="canRedo"
    @cell-commit="onCellCommit"
    @undo="onUndo"
    @redo="onRedo"
  />
  <FormulaBar :selected-cell="selectedCell" :cells="cells" @cell-commit="onCellCommit" />
  <SheetGrid
    :cells="cells"
    :rows="50"
    :cols="26"
    :selected-cell="selectedCell"
    @cell-commit="onCellCommit"
    @cell-select="onCellSelect"
  />
  <SheetTabs
    :sheets="sheets"
    :current-sheet-id="$route.params.id as string"
    @select-sheet="onSelectSheet"
    @create-sheet="onCreateSheet"
  />
</template>
