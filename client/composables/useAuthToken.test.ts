import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useAuthToken } from './useAuthToken'

describe('useAuthToken', () => {
  beforeEach(() => {
    window.sessionStorage.clear()
    // Reset the module-level cache between tests by clearing via the API itself.
    useAuthToken().clearToken()
  })

  it('getToken returns null when nothing has been set', () => {
    const { getToken } = useAuthToken()
    expect(getToken()).toBeNull()
  })

  it('setToken then getToken returns the same token (in-memory cache)', () => {
    const { setToken, getToken } = useAuthToken()
    setToken('abc123')
    expect(getToken()).toBe('abc123')
  })

  it('setToken persists to sessionStorage so a later getToken can recover it after the in-memory cache is cleared', () => {
    const { setToken, clearToken, getToken } = useAuthToken()
    setToken('persisted-token')

    // Directly manipulate sessionStorage to simulate the in-memory cache being
    // gone (e.g. a fresh module instance after a full page reload) while the
    // sessionStorage entry survives.
    clearToken()
    window.sessionStorage.setItem('sheets_auth_token', 'persisted-token')

    expect(getToken()).toBe('persisted-token')
  })

  it('clearToken removes the token from both memory and sessionStorage', () => {
    const { setToken, clearToken, getToken } = useAuthToken()
    setToken('to-be-cleared')
    clearToken()

    expect(getToken()).toBeNull()
    expect(window.sessionStorage.getItem('sheets_auth_token')).toBeNull()
  })

  it('redirectToLogin navigates to the main app\'s token-relay endpoint with the current path as returnTo', () => {
    const { redirectToLogin } = useAuthToken()

    const fakeLocation = { pathname: '/sheets/abc', search: '?foo=bar', href: '' }
    Object.defineProperty(window, 'location', { value: fakeLocation, writable: true, configurable: true })

    redirectToLogin('http://app.local')

    expect(fakeLocation.href).toBe(
      'http://app.local/api/sheets-token?returnTo=' + encodeURIComponent('/sheets/abc?foo=bar')
    )
  })
})
