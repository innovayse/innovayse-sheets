import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import FormulaBar from './FormulaBar.vue'

describe('FormulaBar', () => {
  const cells = [
    { row: 0, col: 1, rawValue: '=A1+1', computedValue: 6, textValue: null, error: null, formatJson: null }
  ]

  it('shows an empty reference and disabled input when nothing is selected', () => {
    const wrapper = mount(FormulaBar, { props: { selectedCell: null, cells: [] } })
    expect(wrapper.find('[data-testid="formula-bar-reference"]').text()).toBe('')
    expect(wrapper.find('[data-testid="formula-bar-input"]').attributes('disabled')).toBeDefined()
  })

  it('shows the selected cell\'s reference (e.g. B1) and raw value', () => {
    const wrapper = mount(FormulaBar, { props: { selectedCell: { row: 0, col: 1 }, cells } })
    expect(wrapper.find('[data-testid="formula-bar-reference"]').text()).toBe('B1')
    expect((wrapper.find('[data-testid="formula-bar-input"]').element as HTMLInputElement).value).toBe('=A1+1')
  })

  it('shows an empty raw value for a selected cell with no data yet', () => {
    const wrapper = mount(FormulaBar, { props: { selectedCell: { row: 5, col: 5 }, cells } })
    expect(wrapper.find('[data-testid="formula-bar-reference"]').text()).toBe('F6')
    expect((wrapper.find('[data-testid="formula-bar-input"]').element as HTMLInputElement).value).toBe('')
  })

  it('emits cell-commit with the new raw value when Enter is pressed', async () => {
    const wrapper = mount(FormulaBar, { props: { selectedCell: { row: 0, col: 1 }, cells } })
    const input = wrapper.find('[data-testid="formula-bar-input"]')
    await input.setValue('=2+2')
    await input.trigger('keydown.enter')

    const emitted = wrapper.emitted('cell-commit')
    expect(emitted).toBeTruthy()
    expect(emitted![0][0]).toEqual({ row: 0, col: 1, rawValue: '=2+2' })
  })
})
