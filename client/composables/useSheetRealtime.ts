import { ref } from 'vue'
import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr'
import type { Cell } from './useSheetApi'
import { useAuthToken } from './useAuthToken'

export function useSheetRealtime(hubUrl: string, sheetId: string) {
  const presence = ref<string[]>([])
  let connection: HubConnection | null = null

  function ensureConnection(): HubConnection {
    if (!connection) {
      const { getToken } = useAuthToken()
      connection = new HubConnectionBuilder()
        .withUrl(hubUrl, { accessTokenFactory: () => getToken() ?? '' })
        .build()

      connection.on('Presence', (userIds: string[]) => {
        presence.value = userIds
      })
    }
    return connection
  }

  // Guard against SSR: HubConnectionBuilder/.build() may touch browser-only
  // globals and must never run during server-side rendering. This vitest
  // suite runs under plain Vite (not the Nuxt runtime), so `import.meta.client`
  // is unavailable here; `typeof window !== 'undefined'` is the
  // framework-agnostic equivalent that is true both in real browsers and in
  // the happy-dom test environment, and false in real Node-based SSR.
  if (typeof window !== 'undefined') {
    ensureConnection()
  }

  async function connect() {
    const conn = ensureConnection()
    await conn.start()
  }

  async function joinSheet() {
    const conn = ensureConnection()
    await conn.invoke('JoinSheet', sheetId)
  }

  function onCellsUpdated(callback: (cells: Cell[]) => void) {
    const conn = ensureConnection()
    conn.on('CellsUpdated', (cells: Cell[]) => callback(cells))
  }

  async function disconnect() {
    if (connection) {
      await connection.stop()
    }
  }

  return { connect, joinSheet, onCellsUpdated, presence, disconnect }
}
