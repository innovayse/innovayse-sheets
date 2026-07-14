import { describe, it, expect, vi, beforeEach } from 'vitest'

const mockConnection = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  invoke: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  onreconnected: vi.fn()
}

const mockWithUrl = vi.fn().mockReturnThis()

vi.mock('@microsoft/signalr', () => {
  return {
    HubConnectionBuilder: vi.fn().mockImplementation(function () {
      return {
        withUrl: mockWithUrl,
        withAutomaticReconnect: vi.fn().mockReturnThis(),
        build: vi.fn().mockReturnValue(mockConnection)
      }
    })
  }
})

import { useSheetRealtime } from './useSheetRealtime'
import { useAuthToken } from './useAuthToken'

describe('useSheetRealtime', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    useAuthToken().clearToken()
  })

  it('passes an accessTokenFactory that resolves the current auth token', () => {
    useAuthToken().setToken('my-jwt')

    useSheetRealtime('http://sheets.local/hubs/sheets', 'sheet-1')

    expect(mockWithUrl).toHaveBeenCalledWith('http://sheets.local/hubs/sheets', expect.objectContaining({
      accessTokenFactory: expect.any(Function)
    }))
    const { accessTokenFactory } = mockWithUrl.mock.calls[0][1]
    expect(accessTokenFactory()).toBe('my-jwt')
  })

  it('connect() starts the underlying connection', async () => {
    const realtime = useSheetRealtime('http://sheets.local/hubs/sheets', 'sheet-1')
    await realtime.connect()

    expect(mockConnection.start).toHaveBeenCalledTimes(1)
  })

  it('joinSheet() invokes JoinSheet with the sheet id', async () => {
    const realtime = useSheetRealtime('http://sheets.local/hubs/sheets', 'sheet-1')
    await realtime.connect()
    await realtime.joinSheet()

    expect(mockConnection.invoke).toHaveBeenCalledWith('JoinSheet', 'sheet-1')
  })

  it('onCellsUpdated registers a CellsUpdated listener that forwards to the callback', async () => {
    const realtime = useSheetRealtime('http://sheets.local/hubs/sheets', 'sheet-1')
    const callback = vi.fn()
    realtime.onCellsUpdated(callback)

    const registeredHandler = mockConnection.on.mock.calls.find(call => call[0] === 'CellsUpdated')?.[1]
    expect(registeredHandler).toBeDefined()

    const fakeCells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: null }]
    registeredHandler!(fakeCells)

    expect(callback).toHaveBeenCalledWith(fakeCells)
  })

  it('presence updates when a Presence event is received', async () => {
    const realtime = useSheetRealtime('http://sheets.local/hubs/sheets', 'sheet-1')

    const registeredHandler = mockConnection.on.mock.calls.find(call => call[0] === 'Presence')?.[1]
    expect(registeredHandler).toBeDefined()

    registeredHandler!(['user-a', 'user-b'])

    expect(realtime.presence.value).toEqual(['user-a', 'user-b'])
  })

  it('disconnect() stops the underlying connection', async () => {
    const realtime = useSheetRealtime('http://sheets.local/hubs/sheets', 'sheet-1')
    await realtime.connect()
    await realtime.disconnect()

    expect(mockConnection.stop).toHaveBeenCalledTimes(1)
  })

  it('does not eagerly build a connection when no browser window is present (SSR guard)', async () => {
    const { HubConnectionBuilder } = await import('@microsoft/signalr')
    const originalWindow = globalThis.window

    // @ts-expect-error - simulate an SSR environment where `window` is undefined
    delete globalThis.window

    try {
      useSheetRealtime('http://sheets.local/hubs/sheets', 'sheet-1')
      expect(HubConnectionBuilder).not.toHaveBeenCalled()
    } finally {
      globalThis.window = originalWindow
    }
  })
})
