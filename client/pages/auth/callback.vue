<!-- innovayse-sheets/client/pages/auth/callback.vue -->
<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthToken } from '../../composables/useAuthToken'

const router = useRouter()
const { setToken } = useAuthToken()

onMounted(() => {
  // Read from the URL fragment, never sent to any server.
  const hash = window.location.hash.startsWith('#') ? window.location.hash.slice(1) : window.location.hash
  const params = new URLSearchParams(hash)
  const token = params.get('token')
  const returnTo = params.get('returnTo') || '/'

  if (token) {
    setToken(token)
  }

  router.replace(returnTo)
})
</script>

<template>
  <div>Signing in…</div>
</template>
