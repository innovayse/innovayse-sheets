import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useSharingApi } from './useSharingApi'

describe('useSharingApi', () => {
  beforeEach(() => {
    globalThis.fetch = vi.fn()
  })

  it('listShares fetches shares for a spreadsheet', async () => {
    const mockShares = [{ userId: 'u1', role: 'View' }]
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => mockShares })

    const api = useSharingApi('http://sheets.local')
    const result = await api.listShares('sheet-1')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/spreadsheets/sheet-1/shares', expect.objectContaining({ method: undefined }))
    expect(result).toEqual(mockShares)
  })

  it('addShare posts the identifier and role', async () => {
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => ({ userId: 'u1', role: 'Edit' }) })

    const api = useSharingApi('http://sheets.local')
    await api.addShare('sheet-1', 'person@example.com', 'Edit')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/spreadsheets/sheet-1/shares', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ userIdentifier: 'person@example.com', role: 'Edit' })
    }))
  })

  it('createLink posts the role and returns the link', async () => {
    const mockLink = { token: 'abc123', role: 'View' }
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => mockLink })

    const api = useSharingApi('http://sheets.local')
    const result = await api.createLink('sheet-1', 'View')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/spreadsheets/sheet-1/links', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ role: 'View' })
    }))
    expect(result).toEqual(mockLink)
  })

  it('claimLink posts to the claim endpoint and returns the spreadsheet id', async () => {
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => ({ spreadsheetId: 'sheet-1' }) })

    const api = useSharingApi('http://sheets.local')
    const result = await api.claimLink('abc123')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/links/abc123/claim', expect.objectContaining({ method: 'POST' }))
    expect(result).toEqual({ spreadsheetId: 'sheet-1' })
  })
})
