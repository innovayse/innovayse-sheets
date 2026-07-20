<!-- innovayse-sheets/client/components/home/SpreadsheetCard.vue -->
<script setup lang="ts">
import { ref } from 'vue'
import type { Spreadsheet } from '../../composables/useSheetApi'
import ConfirmDialog from './ConfirmDialog.vue'

const props = defineProps<{
  spreadsheet: Spreadsheet
  api: {
    renameSpreadsheet: (id: string, title: string) => Promise<Spreadsheet>
    duplicateSpreadsheet: (id: string) => Promise<Spreadsheet>
    deleteSpreadsheet: (id: string) => Promise<void>
  }
}>()

const emit = defineEmits<{
  (e: 'open', id: string): void
  (e: 'renamed', spreadsheet: Spreadsheet): void
  (e: 'duplicated', spreadsheet: Spreadsheet): void
  (e: 'deleted', id: string): void
  (e: 'error', error: unknown, message: string): void
}>()

const menuOpen = ref(false)
const renaming = ref(false)
const renameValue = ref('')
const confirmingDelete = ref(false)

function toggleMenu() {
  menuOpen.value = !menuOpen.value
}

function startRename() {
  renameValue.value = props.spreadsheet.title
  renaming.value = true
  menuOpen.value = false
}

async function commitRename() {
  if (!renaming.value) return
  const title = renameValue.value.trim()
  renaming.value = false
  if (!title || title === props.spreadsheet.title) return
  try {
    const updated = await props.api.renameSpreadsheet(props.spreadsheet.id, title)
    emit('renamed', updated)
  } catch (err) {
    emit('error', err, 'Could not rename this spreadsheet. Please try again.')
  }
}

function cancelRename() {
  renaming.value = false
}

async function duplicate() {
  menuOpen.value = false
  try {
    const copy = await props.api.duplicateSpreadsheet(props.spreadsheet.id)
    emit('duplicated', copy)
  } catch (err) {
    emit('error', err, 'Could not duplicate this spreadsheet. Please try again.')
  }
}

function askDelete() {
  menuOpen.value = false
  confirmingDelete.value = true
}

async function confirmDelete() {
  try {
    await props.api.deleteSpreadsheet(props.spreadsheet.id)
    confirmingDelete.value = false
    emit('deleted', props.spreadsheet.id)
  } catch (err) {
    confirmingDelete.value = false
    emit('error', err, 'Could not delete this spreadsheet. Please try again.')
  }
}

function cancelDelete() {
  confirmingDelete.value = false
}
</script>

<template>
  <div class="relative rounded border border-gray-200 hover:shadow-sm">
    <div
      data-testid="card-body"
      class="cursor-pointer p-4"
      @click="emit('open', spreadsheet.id)"
    >
      <input
        v-if="renaming"
        data-testid="card-rename-input"
        v-model="renameValue"
        class="w-full rounded border border-blue-400 px-1 py-0.5 text-sm font-medium"
        @click.stop
        @keyup.enter="commitRename"
        @keyup.escape="cancelRename"
        @blur="commitRename"
      />
      <span v-else class="block truncate font-medium">{{ spreadsheet.title }}</span>

      <div class="mt-2 flex items-center justify-between text-xs text-gray-400">
        <p>{{ spreadsheet.accessLevel }}</p>
      </div>
    </div>

    <button
      data-testid="card-menu-button"
      class="absolute right-2 top-2 rounded px-1.5 py-0.5 text-gray-500 hover:bg-gray-100"
      @click.stop="toggleMenu"
    >
      ⋮
    </button>

    <div
      v-if="menuOpen"
      class="absolute right-2 top-9 z-10 w-36 rounded border border-gray-200 bg-white py-1 shadow-md"
    >
      <button
        v-if="spreadsheet.accessLevel === 'Owner'"
        data-testid="card-menu-rename"
        class="block w-full px-3 py-1.5 text-left text-sm hover:bg-gray-50"
        @click.stop="startRename"
      >
        Rename
      </button>
      <button
        v-if="spreadsheet.accessLevel === 'Owner' || spreadsheet.accessLevel === 'Edit'"
        data-testid="card-menu-duplicate"
        class="block w-full px-3 py-1.5 text-left text-sm hover:bg-gray-50"
        @click.stop="duplicate"
      >
        Duplicate
      </button>
      <button
        v-if="spreadsheet.accessLevel === 'Owner'"
        data-testid="card-menu-delete"
        class="block w-full px-3 py-1.5 text-left text-sm text-red-600 hover:bg-red-50"
        @click.stop="askDelete"
      >
        Delete
      </button>
    </div>

    <ConfirmDialog
      :open="confirmingDelete"
      :title="`Delete '${spreadsheet.title}'?`"
      message="This can't be undone."
      danger
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />
  </div>
</template>
