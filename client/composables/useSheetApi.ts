import { useAuthToken } from './useAuthToken'

export interface Spreadsheet {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  accessLevel: string
}

export interface Sheet {
  id: string
  name: string
  order: number
}

export interface SheetSummary {
  id: string
  spreadsheetId: string
  name: string
  order: number
}

export interface Cell {
  row: number
  col: number
  rawValue: string
  computedValue: number | null
  textValue: string | null
  error: string | null
  formatJson: string | null
}

export interface CellWrite {
  row: number
  col: number
  rawValue: string
  formatJson?: string | null
}

export function useSheetApi(baseUrl: string) {
  async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const { getToken } = useAuthToken()
    const token = getToken()
    const response = await fetch(`${baseUrl}${path}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(options.headers ?? {})
      }
    })
    if (!response.ok) throw new Error(`Request to ${path} failed with ${response.status}`)
    return response.json() as Promise<T>
  }

  return {
    listSpreadsheets: () => request<Spreadsheet[]>('/api/spreadsheets'),
    createSpreadsheet: (title: string) =>
      request<Spreadsheet>('/api/spreadsheets', { method: 'POST', body: JSON.stringify({ title }) }),
    listSheets: (spreadsheetId: string) =>
      request<Sheet[]>(`/api/spreadsheets/${spreadsheetId}/sheets`),
    createSheet: (spreadsheetId: string, name: string) =>
      request<Sheet>(`/api/spreadsheets/${spreadsheetId}/sheets`, { method: 'POST', body: JSON.stringify({ name }) }),
    getSheet: (sheetId: string) => request<SheetSummary>(`/api/sheets/${sheetId}`),
    getCells: (sheetId: string) => request<Cell[]>(`/api/sheets/${sheetId}/cells`),
    writeCells: (sheetId: string, cells: CellWrite[]) =>
      request<void>(`/api/sheets/${sheetId}/cells`, { method: 'PATCH', body: JSON.stringify({ cells }) })
  }
}
