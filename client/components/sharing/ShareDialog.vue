<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { Share, Link } from '../../composables/useSharingApi'

const props = defineProps<{
  spreadsheetId: string
  sharingApi: {
    listShares: (id: string) => Promise<Share[]>
    addShare: (id: string, identifier: string, role: 'View' | 'Edit') => Promise<Share>
    removeShare: (id: string, userId: string) => Promise<void>
    getLink: (id: string) => Promise<Link>
    createLink: (id: string, role: 'View' | 'Edit') => Promise<Link>
    revokeLink: (id: string) => Promise<void>
  }
}>()

const shares = ref<Share[]>([])
const link = ref<Link | null>(null)
const newIdentifier = ref('')
const newRole = ref<'View' | 'Edit'>('View')

async function loadShares() {
  shares.value = await props.sharingApi.listShares(props.spreadsheetId)
}

async function loadLink() {
  try {
    link.value = await props.sharingApi.getLink(props.spreadsheetId)
  } catch {
    link.value = null
  }
}

async function onAddShare() {
  await props.sharingApi.addShare(props.spreadsheetId, newIdentifier.value, newRole.value)
  newIdentifier.value = ''
  await loadShares()
}

async function onRemoveShare(userId: string) {
  await props.sharingApi.removeShare(props.spreadsheetId, userId)
  await loadShares()
}

async function onGenerateLink() {
  link.value = await props.sharingApi.createLink(props.spreadsheetId, 'View')
}

async function onRevokeLink() {
  await props.sharingApi.revokeLink(props.spreadsheetId)
  link.value = null
}

onMounted(() => {
  loadShares()
  loadLink()
})
</script>

<template>
  <div class="p-4 bg-white rounded shadow">
    <ul>
      <li v-for="share in shares" :key="share.userId">
        {{ share.userId }} — {{ share.role }}
        <button @click="onRemoveShare(share.userId)">Remove</button>
      </li>
    </ul>

    <input data-testid="share-identifier-input" v-model="newIdentifier" placeholder="email" />
    <select data-testid="share-role-select" v-model="newRole">
      <option value="View">View</option>
      <option value="Edit">Edit</option>
    </select>
    <button data-testid="share-add-button" @click="onAddShare">Add</button>

    <div v-if="link">
      Link: {{ link.token }}
      <button @click="onRevokeLink">Revoke</button>
    </div>
    <button v-else data-testid="generate-link-button" @click="onGenerateLink">Generate link</button>
  </div>
</template>
