<!-- innovayse-sheets/client/pages/links/[token].vue -->
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useSharingApi } from '../../composables/useSharingApi'

const route = useRoute()
const router = useRouter()
const token = route.params.token as string
const api = useSharingApi(useRuntimeConfig().public.sheetsApiUrl as string)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    const result = await api.claimLink(token)
    router.push(`/sheets/${result.spreadsheetId}`)
  } catch {
    error.value = 'This link is invalid or has been revoked.'
  }
})
</script>

<template>
  <div v-if="error">{{ error }}</div>
  <div v-else>Joining spreadsheet…</div>
</template>
