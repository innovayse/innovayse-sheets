import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SheetGrid from './SheetGrid.vue'

describe('SheetGrid', () => {
  const cells = [
    { row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: null },
    { row: 0, col: 1, rawValue: '=A1+1', computedValue: 6, textValue: null, error: null, formatJson: null }
  ]

  it('renders computed values for populated cells', () => {
    const wrapper = mount(SheetGrid, { props: { cells, rows: 3, cols: 3, selectedCell: null } })
    expect(wrapper.text()).toContain('5')
    expect(wrapper.text()).toContain('6')
  })

  it('renders the error code for a cell with an error', () => {
    const errorCells = [{ row: 0, col: 0, rawValue: '=10/0', computedValue: null, textValue: null, error: '#DIV/0!', formatJson: null }]
    const wrapper = mount(SheetGrid, { props: { cells: errorCells, rows: 1, cols: 1, selectedCell: null } })
    expect(wrapper.text()).toContain('#DIV/0!')
  })

  it('emits cell-commit with the raw formula text when a cell is edited and blurred', async () => {
    const wrapper = mount(SheetGrid, { props: { cells: [], rows: 2, cols: 2, selectedCell: null } })
    const firstCellInput = wrapper.find('[data-testid="cell-0-0"]')
    await firstCellInput.setValue('=1+1')
    await firstCellInput.trigger('blur')

    const emitted = wrapper.emitted('cell-commit')
    expect(emitted).toBeTruthy()
    expect(emitted![0][0]).toEqual({ row: 0, col: 0, rawValue: '=1+1' })
  })

  it('renders column headers A, B, C... and row headers 1, 2, 3...', () => {
    const wrapper = mount(SheetGrid, { props: { cells: [], rows: 2, cols: 3, selectedCell: null } })
    expect(wrapper.find('[data-testid="col-header-0"]').text()).toBe('A')
    expect(wrapper.find('[data-testid="col-header-1"]').text()).toBe('B')
    expect(wrapper.find('[data-testid="col-header-2"]').text()).toBe('C')
    expect(wrapper.find('[data-testid="row-header-0"]').text()).toBe('1')
    expect(wrapper.find('[data-testid="row-header-1"]').text()).toBe('2')
  })

  it('emits cell-select when a cell is clicked', async () => {
    const wrapper = mount(SheetGrid, { props: { cells: [], rows: 2, cols: 2, selectedCell: null } })
    await wrapper.find('[data-testid="cell-0-1"]').trigger('focus')

    const emitted = wrapper.emitted('cell-select')
    expect(emitted).toBeTruthy()
    expect(emitted![0][0]).toEqual({ row: 0, col: 1 })
  })

  it('applies bold/italic/color styling from a cell\'s formatJson', () => {
    const styledCells = [{
      row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null,
      formatJson: JSON.stringify({ bold: true, italic: true, color: '#ff0000', backgroundColor: '#eeeeee', align: 'center' })
    }]
    const wrapper = mount(SheetGrid, { props: { cells: styledCells, rows: 1, cols: 1, selectedCell: null } })
    const input = wrapper.find('[data-testid="cell-0-0"]')
    expect(input.attributes('style')).toContain('font-weight: bold')
    expect(input.attributes('style')).toContain('font-style: italic')
    expect(input.attributes('style')).toContain('color: #ff0000')
    expect(input.attributes('style')).toContain('background-color: #eeeeee')
    expect(input.attributes('style')).toContain('text-align: center')
  })

  it('does not throw on malformed formatJson and applies no styling', () => {
    const brokenCells = [{ row: 0, col: 0, rawValue: '5', computedValue: 5, textValue: null, error: null, formatJson: '{not valid json' }]
    const wrapper = mount(SheetGrid, { props: { cells: brokenCells, rows: 1, cols: 1, selectedCell: null } })
    expect(wrapper.find('[data-testid="cell-0-0"]').exists()).toBe(true)
  })

  it('highlights the selected cell', () => {
    const wrapper = mount(SheetGrid, { props: { cells: [], rows: 2, cols: 2, selectedCell: { row: 0, col: 1 } } })
    expect(wrapper.find('[data-testid="cell-0-1"]').classes()).toContain('ring-2')
    expect(wrapper.find('[data-testid="cell-0-0"]').classes()).not.toContain('ring-2')
  })
})
