<!-- innovayse-sheets/client/pages/index.vue -->
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useSheetApi, ApiError, type Spreadsheet } from '../composables/useSheetApi'
import { useAuthToken } from '../composables/useAuthToken'
import SpreadsheetCard from '../components/home/SpreadsheetCard.vue'

const router = useRouter()
const api = useSheetApi(useRuntimeConfig().public.sheetsApiUrl as string)
const mainUrl = useRuntimeConfig().public.mainUrl as string
const { getToken, clearToken, redirectToLogin } = useAuthToken()

const spreadsheets = ref<Spreadsheet[]>([])
const loading = ref(true)
const creating = ref(false)
const loadError = ref<string | null>(null)
const actionError = ref<string | null>(null)
const query = ref('')
const sortBy = ref<'name' | 'updated'>('updated')

const visibleSpreadsheets = computed(() => {
  const filtered = spreadsheets.value.filter(s =>
    s.title.toLowerCase().includes(query.value.trim().toLowerCase())
  )
  return [...filtered].sort((a, b) => {
    if (sortBy.value === 'name') return a.title.localeCompare(b.title)
    return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
  })
})

function handleAuthError(err: unknown) {
  if (err instanceof ApiError && (err.status === 401 || err.status === 403)) {
    clearToken()
    redirectToLogin(mainUrl)
    return true
  }
  return false
}

async function load() {
  loading.value = true
  loadError.value = null
  try {
    spreadsheets.value = await api.listSpreadsheets()
  } catch (err) {
    if (!handleAuthError(err)) loadError.value = 'Could not load your spreadsheets. Please try again.'
  } finally {
    loading.value = false
  }
}

async function openSpreadsheet(spreadsheetId: string) {
  try {
    const sheets = await api.listSheets(spreadsheetId)
    if (sheets[0]) {
      router.push(`/sheets/${sheets[0].id}`)
    }
  } catch (err) {
    handleAuthError(err)
  }
}

async function createNew() {
  creating.value = true
  try {
    const spreadsheet = await api.createSpreadsheet('Untitled spreadsheet')
    const sheet = await api.createSheet(spreadsheet.id, 'Sheet1')
    router.push(`/sheets/${sheet.id}`)
  } catch (err) {
    if (!handleAuthError(err)) loadError.value = 'Could not create a new spreadsheet. Please try again.'
  } finally {
    creating.value = false
  }
}

function onRenamed(updated: Spreadsheet) {
  const index = spreadsheets.value.findIndex(s => s.id === updated.id)
  if (index !== -1) spreadsheets.value[index] = updated
}

function onDuplicated(copy: Spreadsheet) {
  spreadsheets.value = [copy, ...spreadsheets.value]
}

function onDeleted(id: string) {
  spreadsheets.value = spreadsheets.value.filter(s => s.id !== id)
}

function onCardError(err: unknown, message: string) {
  if (!handleAuthError(err)) actionError.value = message
}

onMounted(() => {
  if (!getToken()) {
    redirectToLogin(mainUrl)
    return
  }
  load()
})
</script>

<template>
  <div class="mx-auto max-w-5xl px-6 py-10">
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-semibold">Your spreadsheets</h1>
      <button
        class="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        :disabled="creating"
        @click="createNew"
      >
        {{ creating ? 'Creating…' : 'New spreadsheet' }}
      </button>
    </div>

    <div class="mb-6 flex items-center gap-3">
      <input
        data-testid="search-input"
        v-model="query"
        type="text"
        placeholder="Search spreadsheets"
        class="w-64 rounded border border-gray-300 px-3 py-1.5 text-sm"
      />
      <select
        data-testid="sort-select"
        v-model="sortBy"
        class="rounded border border-gray-300 px-2 py-1.5 text-sm"
      >
        <option value="updated">Last modified</option>
        <option value="name">Name A–Z</option>
      </select>
    </div>

    <p v-if="loadError" class="mb-4 text-sm text-red-600">{{ loadError }}</p>
    <div v-if="actionError" data-testid="action-error" class="mb-4 flex items-center justify-between rounded bg-red-50 px-3 py-2 text-sm text-red-700">
      <span>{{ actionError }}</span>
      <button class="ml-4 text-red-500 hover:text-red-700" @click="actionError = null">×</button>
    </div>

    <div v-if="loading" class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
      <div v-for="n in 8" :key="n" class="h-24 animate-pulse rounded border border-gray-200 bg-gray-100"></div>
    </div>

    <div v-else-if="spreadsheets.length === 0" class="flex flex-col items-center gap-3 py-16 text-center">
      <p class="text-sm text-gray-500">You don't have any spreadsheets yet.</p>
      <button
        data-testid="empty-state-create"
        class="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        :disabled="creating"
        @click="createNew"
      >
        Create your first spreadsheet
      </button>
    </div>

    <p v-else-if="visibleSpreadsheets.length === 0" class="py-8 text-center text-sm text-gray-500">
      No spreadsheets match "{{ query }}".
    </p>

    <div v-else class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
      <SpreadsheetCard
        v-for="spreadsheet in visibleSpreadsheets"
        :key="spreadsheet.id"
        :spreadsheet="spreadsheet"
        :api="{ renameSpreadsheet: api.renameSpreadsheet, duplicateSpreadsheet: api.duplicateSpreadsheet, deleteSpreadsheet: api.deleteSpreadsheet }"
        @open="openSpreadsheet"
        @renamed="onRenamed"
        @duplicated="onDuplicated"
        @deleted="onDeleted"
        @error="onCardError"
      />
    </div>
  </div>
</template>
