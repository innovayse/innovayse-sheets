import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SheetTabs from './SheetTabs.vue'

describe('SheetTabs', () => {
  const sheets = [
    { id: 'sheet-1', name: 'Sheet1', order: 0 },
    { id: 'sheet-2', name: 'Sheet2', order: 1 },
    { id: 'sheet-3', name: 'Budget', order: 2 }
  ]

  it('renders one tab per sheet with its name', () => {
    const wrapper = mount(SheetTabs, { props: { sheets, currentSheetId: 'sheet-1' } })
    expect(wrapper.find('[data-testid="tab-sheet-1"]').text()).toBe('Sheet1')
    expect(wrapper.find('[data-testid="tab-sheet-2"]').text()).toBe('Sheet2')
    expect(wrapper.find('[data-testid="tab-sheet-3"]').text()).toBe('Budget')
  })

  it('marks the tab matching currentSheetId as active', () => {
    const wrapper = mount(SheetTabs, { props: { sheets, currentSheetId: 'sheet-2' } })
    expect(wrapper.find('[data-testid="tab-sheet-2"]').classes()).toContain('bg-sky-500')
    expect(wrapper.find('[data-testid="tab-sheet-1"]').classes()).not.toContain('bg-sky-500')
  })

  it('emits select-sheet with the clicked sheet\'s id', async () => {
    const wrapper = mount(SheetTabs, { props: { sheets, currentSheetId: 'sheet-1' } })
    await wrapper.find('[data-testid="tab-sheet-3"]').trigger('click')

    const emitted = wrapper.emitted('select-sheet')
    expect(emitted).toBeTruthy()
    expect(emitted![0][0]).toBe('sheet-3')
  })

  it('emits create-sheet when the + button is clicked', async () => {
    const wrapper = mount(SheetTabs, { props: { sheets, currentSheetId: 'sheet-1' } })
    await wrapper.find('[data-testid="add-sheet-button"]').trigger('click')

    expect(wrapper.emitted('create-sheet')).toBeTruthy()
  })

  it('renders only the + button when sheets is empty', () => {
    const wrapper = mount(SheetTabs, { props: { sheets: [], currentSheetId: '' } })
    expect(wrapper.find('[data-testid="add-sheet-button"]').exists()).toBe(true)
    expect(wrapper.findAll('[data-testid^="tab-"]').length).toBe(0)
  })
})
