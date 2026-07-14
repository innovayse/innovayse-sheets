const STORAGE_KEY = 'sheets_auth_token'

let cachedToken: string | null = null

export function useAuthToken() {
  function setToken(token: string) {
    cachedToken = token
    if (typeof window !== 'undefined') {
      window.sessionStorage.setItem(STORAGE_KEY, token)
    }
  }

  function getToken(): string | null {
    if (cachedToken) return cachedToken
    if (typeof window !== 'undefined') {
      const stored = window.sessionStorage.getItem(STORAGE_KEY)
      if (stored) {
        cachedToken = stored
        return stored
      }
    }
    return null
  }

  function clearToken() {
    cachedToken = null
    if (typeof window !== 'undefined') {
      window.sessionStorage.removeItem(STORAGE_KEY)
    }
  }

  function redirectToLogin(mainUrl: string) {
    if (typeof window === 'undefined') return
    const returnTo = window.location.pathname + window.location.search
    window.location.href = `${mainUrl}/api/sheets-token?returnTo=${encodeURIComponent(returnTo)}`
  }

  return { getToken, setToken, clearToken, redirectToLogin }
}
