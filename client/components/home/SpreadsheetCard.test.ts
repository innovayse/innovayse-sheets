import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import SpreadsheetCard from './SpreadsheetCard.vue'
import type { Spreadsheet } from '../../composables/useSheetApi'

function makeSpreadsheet(overrides: Partial<Spreadsheet> = {}): Spreadsheet {
  return {
    id: 's1',
    title: 'Budget',
    createdAt: '2026-07-01T00:00:00Z',
    updatedAt: '2026-07-10T00:00:00Z',
    accessLevel: 'Owner',
    ...overrides
  }
}

function makeFakeApi(overrides: Partial<Record<string, any>> = {}) {
  return {
    renameSpreadsheet: vi.fn().mockResolvedValue(makeSpreadsheet({ title: 'Renamed' })),
    duplicateSpreadsheet: vi.fn().mockResolvedValue(makeSpreadsheet({ id: 's2', title: 'Budget (copy)' })),
    deleteSpreadsheet: vi.fn().mockResolvedValue(undefined),
    ...overrides
  }
}

describe('SpreadsheetCard', () => {
  it('displays the title and access level badge', () => {
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api: makeFakeApi() } })
    expect(wrapper.text()).toContain('Budget')
    expect(wrapper.text()).toContain('Owner')
  })

  it('emits open when the card body is clicked', async () => {
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api: makeFakeApi() } })
    await wrapper.find('[data-testid="card-body"]').trigger('click')
    expect(wrapper.emitted('open')).toEqual([['s1']])
  })

  it('hides Rename and Delete menu items for non-owner access levels', async () => {
    const wrapper = mount(SpreadsheetCard, {
      props: { spreadsheet: makeSpreadsheet({ accessLevel: 'Edit' }), api: makeFakeApi() }
    })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    expect(wrapper.find('[data-testid="card-menu-rename"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="card-menu-delete"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="card-menu-duplicate"]').exists()).toBe(true)
  })

  it('hides Duplicate for View-only access', async () => {
    const wrapper = mount(SpreadsheetCard, {
      props: { spreadsheet: makeSpreadsheet({ accessLevel: 'View' }), api: makeFakeApi() }
    })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    expect(wrapper.find('[data-testid="card-menu-duplicate"]').exists()).toBe(false)
  })

  it('renames on Enter and emits renamed with the updated spreadsheet', async () => {
    const api = makeFakeApi()
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api } })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    await wrapper.find('[data-testid="card-menu-rename"]').trigger('click')

    const input = wrapper.find('[data-testid="card-rename-input"]')
    await input.setValue('New Title')
    await input.trigger('keyup.enter')
    await flushPromises()

    expect(api.renameSpreadsheet).toHaveBeenCalledWith('s1', 'New Title')
    expect(wrapper.emitted('renamed')![0]).toEqual([makeSpreadsheet({ title: 'Renamed' })])
  })

  it('emits error and does not emit renamed when the rename API call fails', async () => {
    const api = makeFakeApi({ renameSpreadsheet: vi.fn().mockRejectedValue(new Error('boom')) })
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api } })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    await wrapper.find('[data-testid="card-menu-rename"]').trigger('click')

    const input = wrapper.find('[data-testid="card-rename-input"]')
    await input.setValue('New Title')
    await input.trigger('keyup.enter')
    await flushPromises()

    expect(wrapper.emitted('renamed')).toBeUndefined()
    const errorCall = wrapper.emitted('error')![0]
    expect(errorCall[1]).toBe('Could not rename this spreadsheet. Please try again.')
    expect(errorCall[0]).toBeDefined()
  })

  it('duplicates and emits duplicated with the new spreadsheet', async () => {
    const api = makeFakeApi()
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api } })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    await wrapper.find('[data-testid="card-menu-duplicate"]').trigger('click')
    await flushPromises()

    expect(api.duplicateSpreadsheet).toHaveBeenCalledWith('s1')
    expect(wrapper.emitted('duplicated')![0]).toEqual([makeSpreadsheet({ id: 's2', title: 'Budget (copy)' })])
  })

  it('emits error and does not emit duplicated when the duplicate API call fails', async () => {
    const api = makeFakeApi({ duplicateSpreadsheet: vi.fn().mockRejectedValue(new Error('boom')) })
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api } })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    await wrapper.find('[data-testid="card-menu-duplicate"]').trigger('click')
    await flushPromises()

    expect(wrapper.emitted('duplicated')).toBeUndefined()
    const errorCall = wrapper.emitted('error')![0]
    expect(errorCall[1]).toBe('Could not duplicate this spreadsheet. Please try again.')
    expect(errorCall[0]).toBeDefined()
  })

  it('emits error, does not emit deleted, and closes the confirm dialog when the delete API call fails', async () => {
    const api = makeFakeApi({ deleteSpreadsheet: vi.fn().mockRejectedValue(new Error('boom')) })
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api } })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    await wrapper.find('[data-testid="card-menu-delete"]').trigger('click')
    await wrapper.find('[data-testid="confirm-dialog-confirm"]').trigger('click')
    await flushPromises()

    expect(wrapper.emitted('deleted')).toBeUndefined()
    const errorCall = wrapper.emitted('error')![0]
    expect(errorCall[1]).toBe('Could not delete this spreadsheet. Please try again.')
    expect(errorCall[0]).toBeDefined()
    expect(wrapper.find('[data-testid="confirm-dialog"]').exists()).toBe(false)
  })

  it('opens a confirm dialog on Delete and only calls the API after confirming', async () => {
    const api = makeFakeApi()
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api } })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    await wrapper.find('[data-testid="card-menu-delete"]').trigger('click')

    expect(api.deleteSpreadsheet).not.toHaveBeenCalled()
    expect(wrapper.find('[data-testid="confirm-dialog"]').exists()).toBe(true)

    await wrapper.find('[data-testid="confirm-dialog-confirm"]').trigger('click')
    await flushPromises()

    expect(api.deleteSpreadsheet).toHaveBeenCalledWith('s1')
    expect(wrapper.emitted('deleted')).toEqual([['s1']])
  })

  it('does not call the API when delete is cancelled', async () => {
    const api = makeFakeApi()
    const wrapper = mount(SpreadsheetCard, { props: { spreadsheet: makeSpreadsheet(), api } })
    await wrapper.find('[data-testid="card-menu-button"]').trigger('click')
    await wrapper.find('[data-testid="card-menu-delete"]').trigger('click')
    await wrapper.find('[data-testid="confirm-dialog-cancel"]').trigger('click')

    expect(api.deleteSpreadsheet).not.toHaveBeenCalled()
    expect(wrapper.find('[data-testid="confirm-dialog"]').exists()).toBe(false)
  })
})
