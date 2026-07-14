import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SheetToolbar from './SheetToolbar.vue'

describe('SheetToolbar', () => {
  it('disables all buttons when nothing is selected', () => {
    const wrapper = mount(SheetToolbar, { props: { selectedCell: null, cells: [], canUndo: false, canRedo: false } })
    expect(wrapper.find('[data-testid="toolbar-bold"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[data-testid="toolbar-italic"]').attributes('disabled')).toBeDefined()
  })

  it('clicking bold on an unformatted cell emits cell-commit with bold:true in formatJson', async () => {
    const cells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: null }]
    const wrapper = mount(SheetToolbar, { props: { selectedCell: { row: 0, col: 0 }, cells, canUndo: false, canRedo: false } })

    await wrapper.find('[data-testid="toolbar-bold"]').trigger('click')

    const emitted = wrapper.emitted('cell-commit')
    expect(emitted).toBeTruthy()
    const write = emitted![0][0] as any
    expect(write.row).toBe(0)
    expect(write.col).toBe(0)
    expect(JSON.parse(write.formatJson)).toEqual({ bold: true })
  })

  it('clicking bold on an already-bold cell toggles it off', async () => {
    const cells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: JSON.stringify({ bold: true }) }]
    const wrapper = mount(SheetToolbar, { props: { selectedCell: { row: 0, col: 0 }, cells, canUndo: false, canRedo: false } })

    await wrapper.find('[data-testid="toolbar-bold"]').trigger('click')

    const emitted = wrapper.emitted('cell-commit')
    const write = emitted![0][0] as any
    expect(JSON.parse(write.formatJson)).toEqual({ bold: false })
  })

  it('the bold button appears active when the selected cell is already bold', () => {
    const cells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: JSON.stringify({ bold: true }) }]
    const wrapper = mount(SheetToolbar, { props: { selectedCell: { row: 0, col: 0 }, cells, canUndo: false, canRedo: false } })

    expect(wrapper.find('[data-testid="toolbar-bold"]').classes()).toContain('bg-sky-100')
  })

  it('clicking an align button sets align in formatJson, preserving other existing format fields', async () => {
    const cells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: JSON.stringify({ bold: true }) }]
    const wrapper = mount(SheetToolbar, { props: { selectedCell: { row: 0, col: 0 }, cells, canUndo: false, canRedo: false } })

    await wrapper.find('[data-testid="toolbar-align-center"]').trigger('click')

    const emitted = wrapper.emitted('cell-commit')
    const write = emitted![0][0] as any
    expect(JSON.parse(write.formatJson)).toEqual({ bold: true, align: 'center' })
  })

  it('the text color input commits on change, not on every input event, to avoid write amplification', async () => {
    const cells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: null }]
    const wrapper = mount(SheetToolbar, { props: { selectedCell: { row: 0, col: 0 }, cells, canUndo: false, canRedo: false } })
    const input = wrapper.find('[data-testid="toolbar-text-color"]')

    await input.trigger('input')
    expect(wrapper.emitted('cell-commit')).toBeFalsy()

    await input.trigger('change')
    const emitted = wrapper.emitted('cell-commit')
    expect(emitted).toBeTruthy()
    expect(JSON.parse((emitted![0][0] as any).formatJson)).toEqual({ color: '#000000' })
  })

  it('undo button is disabled when canUndo is false, enabled when true', () => {
    const disabled = mount(SheetToolbar, { props: { selectedCell: null, cells: [], canUndo: false, canRedo: false } })
    expect(disabled.find('[data-testid="toolbar-undo"]').attributes('disabled')).toBeDefined()

    const enabled = mount(SheetToolbar, { props: { selectedCell: null, cells: [], canUndo: true, canRedo: false } })
    expect(enabled.find('[data-testid="toolbar-undo"]').attributes('disabled')).toBeUndefined()
  })

  it('redo button is disabled when canRedo is false, enabled when true', () => {
    const disabled = mount(SheetToolbar, { props: { selectedCell: null, cells: [], canUndo: false, canRedo: false } })
    expect(disabled.find('[data-testid="toolbar-redo"]').attributes('disabled')).toBeDefined()

    const enabled = mount(SheetToolbar, { props: { selectedCell: null, cells: [], canUndo: false, canRedo: true } })
    expect(enabled.find('[data-testid="toolbar-redo"]').attributes('disabled')).toBeUndefined()
  })

  it('clicking undo emits the undo event', async () => {
    const wrapper = mount(SheetToolbar, { props: { selectedCell: null, cells: [], canUndo: true, canRedo: false } })
    await wrapper.find('[data-testid="toolbar-undo"]').trigger('click')
    expect(wrapper.emitted('undo')).toBeTruthy()
  })

  it('clicking redo emits the redo event', async () => {
    const wrapper = mount(SheetToolbar, { props: { selectedCell: null, cells: [], canUndo: false, canRedo: true } })
    await wrapper.find('[data-testid="toolbar-redo"]').trigger('click')
    expect(wrapper.emitted('redo')).toBeTruthy()
  })
})
