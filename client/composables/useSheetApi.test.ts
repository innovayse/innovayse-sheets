import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useSheetApi } from './useSheetApi'
import { useAuthToken } from './useAuthToken'

describe('useSheetApi', () => {
  beforeEach(() => {
    globalThis.fetch = vi.fn()
    useAuthToken().clearToken()
  })

  it('attaches an Authorization header when a token is present', async () => {
    useAuthToken().setToken('my-jwt')
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => ([]) })

    const api = useSheetApi('http://sheets.local')
    await api.getCells('sheet-1')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/sheets/sheet-1/cells', expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer my-jwt' })
    }))
  })

  it('omits the Authorization header when no token is present', async () => {
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => ([]) })

    const api = useSheetApi('http://sheets.local')
    await api.getCells('sheet-1')

    const callHeaders = (fetch as any).mock.calls[0][1].headers
    expect(callHeaders.Authorization).toBeUndefined()
  })

  it('createSpreadsheet posts the title and returns the created spreadsheet', async () => {
    const mockResponse = { id: '1', title: 'My Sheet', createdAt: '2026-07-09T00:00:00Z', updatedAt: '2026-07-09T00:00:00Z', accessLevel: 'Owner' }
    ;(fetch as any).mockResolvedValue({
      ok: true,
      json: async () => mockResponse
    })

    const api = useSheetApi('http://sheets.local')
    const result = await api.createSpreadsheet('My Sheet')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/spreadsheets', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ title: 'My Sheet' })
    }))
    expect(result).toEqual(mockResponse)
  })

  it('renameSpreadsheet sends a PATCH with the new title', async () => {
    const mockResponse = { id: '1', title: 'Renamed', createdAt: '2026-07-09T00:00:00Z', updatedAt: '2026-07-19T00:00:00Z', accessLevel: 'Owner' }
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => mockResponse })

    const api = useSheetApi('http://sheets.local')
    const result = await api.renameSpreadsheet('1', 'Renamed')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/spreadsheets/1', expect.objectContaining({
      method: 'PATCH',
      body: JSON.stringify({ title: 'Renamed' })
    }))
    expect(result).toEqual(mockResponse)
  })

  it('duplicateSpreadsheet posts to the duplicate endpoint and returns the new spreadsheet', async () => {
    const mockResponse = { id: '2', title: 'Original (copy)', createdAt: '2026-07-19T00:00:00Z', updatedAt: '2026-07-19T00:00:00Z', accessLevel: 'Owner' }
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => mockResponse })

    const api = useSheetApi('http://sheets.local')
    const result = await api.duplicateSpreadsheet('1')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/spreadsheets/1/duplicate', expect.objectContaining({
      method: 'POST'
    }))
    expect(result).toEqual(mockResponse)
  })

  it('deleteSpreadsheet sends a DELETE to the spreadsheet endpoint', async () => {
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => ({}) })

    const api = useSheetApi('http://sheets.local')
    await api.deleteSpreadsheet('1')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/spreadsheets/1', expect.objectContaining({
      method: 'DELETE'
    }))
  })

  it('writeCells sends a PATCH with the cell batch', async () => {
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => ({}) })

    const api = useSheetApi('http://sheets.local')
    await api.writeCells('sheet-1', [{ row: 0, col: 0, rawValue: '5' }])

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/sheets/sheet-1/cells', expect.objectContaining({
      method: 'PATCH',
      body: JSON.stringify({ cells: [{ row: 0, col: 0, rawValue: '5' }] })
    }))
  })

  it('getSheet fetches and returns the sheet summary including spreadsheetId', async () => {
    const mockSheet = { id: 'sheet-1', spreadsheetId: 'spreadsheet-1', name: 'Sheet1', order: 0 }
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => mockSheet })

    const api = useSheetApi('http://sheets.local')
    const result = await api.getSheet('sheet-1')

    expect(fetch).toHaveBeenCalledWith('http://sheets.local/api/sheets/sheet-1', expect.objectContaining({}))
    expect(result).toEqual(mockSheet)
  })

  it('getCells fetches and returns the cell list', async () => {
    const mockCells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, error: null, formatJson: null }]
    ;(fetch as any).mockResolvedValue({ ok: true, json: async () => mockCells })

    const api = useSheetApi('http://sheets.local')
    const result = await api.getCells('sheet-1')

    expect(result).toEqual(mockCells)
  })

  it('writeCells rejects with the HTTP status attached when the request fails', async () => {
    ;(fetch as any).mockResolvedValue({ ok: false, status: 401, json: async () => ({}) })

    const api = useSheetApi('http://sheets.local')

    await expect(api.writeCells('sheet-1', [{ row: 0, col: 0, rawValue: '5' }]))
      .rejects.toMatchObject({ status: 401 })
  })
})
