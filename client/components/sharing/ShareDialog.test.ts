import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ShareDialog from './ShareDialog.vue'

function makeFakeApi(overrides: Partial<Record<string, any>> = {}) {
  return {
    listShares: vi.fn().mockResolvedValue([{ userId: 'u1', role: 'View' }]),
    addShare: vi.fn().mockResolvedValue({ userId: 'u2', role: 'Edit' }),
    removeShare: vi.fn().mockResolvedValue(undefined),
    getLink: vi.fn().mockRejectedValue(new Error('no link yet')),
    createLink: vi.fn().mockResolvedValue({ token: 'abc123', role: 'View' }),
    revokeLink: vi.fn().mockResolvedValue(undefined),
    ...overrides
  }
}

describe('ShareDialog', () => {
  it('loads and displays existing shares on mount', async () => {
    const api = makeFakeApi()
    const wrapper = mount(ShareDialog, { props: { spreadsheetId: 'sheet-1', sharingApi: api } })
    await flushPromises()

    expect(api.listShares).toHaveBeenCalledWith('sheet-1')
    expect(wrapper.text()).toContain('u1')
  })

  it('adding a share calls addShare and refreshes the list', async () => {
    const api = makeFakeApi()
    const wrapper = mount(ShareDialog, { props: { spreadsheetId: 'sheet-1', sharingApi: api } })
    await flushPromises()

    await wrapper.find('[data-testid="share-identifier-input"]').setValue('new@example.com')
    await wrapper.find('[data-testid="share-role-select"]').setValue('Edit')
    await wrapper.find('[data-testid="share-add-button"]').trigger('click')
    await flushPromises()

    expect(api.addShare).toHaveBeenCalledWith('sheet-1', 'new@example.com', 'Edit')
    expect(api.listShares).toHaveBeenCalledTimes(2)
  })

  it('generating a link calls createLink and displays the token', async () => {
    const api = makeFakeApi()
    const wrapper = mount(ShareDialog, { props: { spreadsheetId: 'sheet-1', sharingApi: api } })
    await flushPromises()

    await wrapper.find('[data-testid="generate-link-button"]').trigger('click')
    await flushPromises()

    expect(api.createLink).toHaveBeenCalledWith('sheet-1', 'View')
    expect(wrapper.text()).toContain('abc123')
  })
})
