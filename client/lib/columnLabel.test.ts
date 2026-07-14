import { describe, it, expect } from 'vitest'
import { colIndexToLabel } from './columnLabel'

describe('colIndexToLabel', () => {
  it('converts single-letter columns', () => {
    expect(colIndexToLabel(0)).toBe('A')
    expect(colIndexToLabel(1)).toBe('B')
    expect(colIndexToLabel(25)).toBe('Z')
  })

  it('converts the Z to AA boundary', () => {
    expect(colIndexToLabel(26)).toBe('AA')
    expect(colIndexToLabel(27)).toBe('AB')
  })

  it('converts further multi-letter columns', () => {
    expect(colIndexToLabel(51)).toBe('AZ')
    expect(colIndexToLabel(52)).toBe('BA')
    expect(colIndexToLabel(701)).toBe('ZZ')
    expect(colIndexToLabel(702)).toBe('AAA')
  })
})
