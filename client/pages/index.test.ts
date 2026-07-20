import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'

const mockPush = vi.fn()
const mockListSpreadsheets = vi.fn()
const mockListSheets = vi.fn()
const mockCreateSpreadsheet = vi.fn()
const mockCreateSheet = vi.fn()
const mockRenameSpreadsheet = vi.fn()
const mockDuplicateSpreadsheet = vi.fn()
const mockDeleteSpreadsheet = vi.fn()
const mockClearToken = vi.fn()
const mockRedirectToLogin = vi.fn()

vi.stubGlobal('useRouter', () => ({ push: mockPush }))
vi.stubGlobal('useRuntimeConfig', () => ({ public: { sheetsApiUrl: 'http://sheets.local', mainUrl: 'http://main.local' } }))

vi.mock('../composables/useSheetApi', async () => {
  const actual = await vi.importActual<typeof import('../composables/useSheetApi')>('../composables/useSheetApi')
  return {
    ...actual,
    useSheetApi: () => ({
      listSpreadsheets: mockListSpreadsheets,
      listSheets: mockListSheets,
      createSpreadsheet: mockCreateSpreadsheet,
      createSheet: mockCreateSheet,
      renameSpreadsheet: mockRenameSpreadsheet,
      duplicateSpreadsheet: mockDuplicateSpreadsheet,
      deleteSpreadsheet: mockDeleteSpreadsheet
    })
  }
})

vi.mock('../composables/useAuthToken', () => ({
  useAuthToken: () => ({
    getToken: () => 'test-token',
    clearToken: mockClearToken,
    redirectToLogin: mockRedirectToLogin
  })
}))

import IndexPage from './index.vue'

function makeSpreadsheet(overrides: Partial<Record<string, any>> = {}) {
  return {
    id: 's1',
    title: 'Budget',
    createdAt: '2026-07-01T00:00:00Z',
    updatedAt: '2026-07-10T00:00:00Z',
    accessLevel: 'Owner',
    ...overrides
  }
}

describe('pages/index.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders a card per spreadsheet after loading', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' }), makeSpreadsheet({ id: 's2', title: 'Roadmap' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    expect(wrapper.text()).toContain('Budget')
    expect(wrapper.text()).toContain('Roadmap')
  })

  it('shows the empty state with a create button when there are no spreadsheets', async () => {
    mockListSpreadsheets.mockResolvedValue([])
    const wrapper = mount(IndexPage)
    await flushPromises()

    expect(wrapper.find('[data-testid="empty-state-create"]').exists()).toBe(true)
  })

  it('filters the grid by the search query', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' }), makeSpreadsheet({ id: 's2', title: 'Roadmap' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    await wrapper.find('[data-testid="search-input"]').setValue('road')
    await flushPromises()

    expect(wrapper.text()).not.toContain('Budget')
    expect(wrapper.text()).toContain('Roadmap')
  })

  it('sorts by name A-Z when selected', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Zebra' }), makeSpreadsheet({ id: 's2', title: 'Alpha' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    await wrapper.find('[data-testid="sort-select"]').setValue('name')
    await flushPromises()

    const titles = wrapper.findAll('[data-testid="card-body"] span').map(el => el.text())
    expect(titles[0]).toBe('Alpha')
    expect(titles[1]).toBe('Zebra')
  })

  it('removes a card from the grid when it emits deleted', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    await wrapper.findComponent({ name: 'SpreadsheetCard' }).vm.$emit('deleted', 's1')
    await flushPromises()

    expect(wrapper.text()).not.toContain('Budget')
  })

  it('prepends a card to the grid when a card emits duplicated', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    const copy = makeSpreadsheet({ id: 's2', title: 'Budget (copy)' })
    await wrapper.findComponent({ name: 'SpreadsheetCard' }).vm.$emit('duplicated', copy)
    await flushPromises()

    expect(wrapper.text()).toContain('Budget (copy)')
  })

  it('updates a card title in place when it emits renamed', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    const renamed = makeSpreadsheet({ id: 's1', title: 'Budget 2027' })
    await wrapper.findComponent({ name: 'SpreadsheetCard' }).vm.$emit('renamed', renamed)
    await flushPromises()

    expect(wrapper.text()).toContain('Budget 2027')
    expect(wrapper.text()).not.toContain('Budget 2026')
  })

  it('shows an inline banner when a card emits an error', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    await wrapper.findComponent({ name: 'SpreadsheetCard' }).vm.$emit('error', new Error('boom'), 'Could not rename this spreadsheet. Please try again.')
    await flushPromises()

    expect(wrapper.find('[data-testid="action-error"]').text()).toContain('Could not rename this spreadsheet. Please try again.')
  })

  it('redirects to login instead of showing a banner when a card emits a 401 error', async () => {
    const { ApiError } = await vi.importActual<typeof import('../composables/useSheetApi')>('../composables/useSheetApi')
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' })])
    const wrapper = mount(IndexPage)
    await flushPromises()

    const authError = new ApiError('/api/spreadsheets/s1', 401)
    await wrapper.findComponent({ name: 'SpreadsheetCard' }).vm.$emit('error', authError, 'Could not rename this spreadsheet. Please try again.')
    await flushPromises()

    expect(wrapper.find('[data-testid="action-error"]').exists()).toBe(false)
    expect(mockClearToken).toHaveBeenCalled()
    expect(mockRedirectToLogin).toHaveBeenCalled()
  })

  it('opens a spreadsheet by navigating to its first sheet', async () => {
    mockListSpreadsheets.mockResolvedValue([makeSpreadsheet({ id: 's1', title: 'Budget' })])
    mockListSheets.mockResolvedValue([{ id: 'sheet-1', name: 'Sheet1', order: 0 }])
    const wrapper = mount(IndexPage)
    await flushPromises()

    await wrapper.findComponent({ name: 'SpreadsheetCard' }).vm.$emit('open', 's1')
    await flushPromises()

    expect(mockPush).toHaveBeenCalledWith('/sheets/sheet-1')
  })
})
