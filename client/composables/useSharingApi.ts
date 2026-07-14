export interface Share {
  userId: string
  role: 'View' | 'Edit'
}

export interface Link {
  token: string
  role: 'View' | 'Edit'
}

export interface ClaimResult {
  spreadsheetId: string
}

export function useSharingApi(baseUrl: string) {
  async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const response = await fetch(`${baseUrl}${path}`, {
      method: options.method,
      ...options,
      headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) }
    })
    if (!response.ok) throw new Error(`Request to ${path} failed with ${response.status}`)
    return response.json() as Promise<T>
  }

  return {
    listShares: (spreadsheetId: string) => request<Share[]>(`/api/spreadsheets/${spreadsheetId}/shares`),
    addShare: (spreadsheetId: string, userIdentifier: string, role: 'View' | 'Edit') =>
      request<Share>(`/api/spreadsheets/${spreadsheetId}/shares`, { method: 'POST', body: JSON.stringify({ userIdentifier, role }) }),
    removeShare: (spreadsheetId: string, userId: string) =>
      request<void>(`/api/spreadsheets/${spreadsheetId}/shares/${userId}`, { method: 'DELETE' }),
    getLink: (spreadsheetId: string) => request<Link>(`/api/spreadsheets/${spreadsheetId}/links`),
    createLink: (spreadsheetId: string, role: 'View' | 'Edit') =>
      request<Link>(`/api/spreadsheets/${spreadsheetId}/links`, { method: 'POST', body: JSON.stringify({ role }) }),
    revokeLink: (spreadsheetId: string) =>
      request<void>(`/api/spreadsheets/${spreadsheetId}/links`, { method: 'DELETE' }),
    claimLink: (token: string) =>
      request<ClaimResult>(`/api/links/${token}/claim`, { method: 'POST' })
  }
}
