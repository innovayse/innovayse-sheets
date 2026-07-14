import { describe, it, expect } from 'vitest'
import { useUndoRedo } from './useUndoRedo'

function record(n: number) {
  return { row: 0, col: n, prevRawValue: `prev${n}`, prevFormatJson: null, nextRawValue: `next${n}`, nextFormatJson: null }
}

describe('useUndoRedo', () => {
  it('starts with canUndo and canRedo both false', () => {
    const { canUndo, canRedo } = useUndoRedo()
    expect(canUndo.value).toBe(false)
    expect(canRedo.value).toBe(false)
  })

  it('push makes canUndo true', () => {
    const { push, canUndo } = useUndoRedo()
    push(record(1))
    expect(canUndo.value).toBe(true)
  })

  it('undo returns the most recently pushed record and moves it to the redo stack', () => {
    const { push, undo, canUndo, canRedo } = useUndoRedo()
    push(record(1))
    push(record(2))

    const undone = undo()

    expect(undone).toEqual(record(2))
    expect(canRedo.value).toBe(true)
    expect(canUndo.value).toBe(true) // record(1) still on the undo stack
  })

  it('undo on an empty stack returns null', () => {
    const { undo } = useUndoRedo()
    expect(undo()).toBeNull()
  })

  it('redo returns the most recently undone record and moves it back to the undo stack', () => {
    const { push, undo, redo, canRedo, canUndo } = useUndoRedo()
    push(record(1))
    undo()

    const redone = redo()

    expect(redone).toEqual(record(1))
    expect(canRedo.value).toBe(false)
    expect(canUndo.value).toBe(true)
  })

  it('redo on an empty stack returns null', () => {
    const { redo } = useUndoRedo()
    expect(redo()).toBeNull()
  })

  it('pushing a new record clears the redo stack', () => {
    const { push, undo, redo, canRedo } = useUndoRedo()
    push(record(1))
    undo()
    expect(canRedo.value).toBe(true)

    push(record(2))

    expect(canRedo.value).toBe(false)
    expect(redo()).toBeNull()
  })

  it('caps the undo stack at 20 entries, evicting the oldest', () => {
    const { push, undo } = useUndoRedo()
    for (let i = 1; i <= 21; i++) {
      push(record(i))
    }

    // The 21st push should have evicted record(1); undoing 20 times
    // should give back records 21 down to 2, never record(1).
    const undone: number[] = []
    let next = undo()
    while (next !== null) {
      undone.push(next.col)
      next = undo()
    }

    expect(undone).toHaveLength(20)
    expect(undone).not.toContain(1)
    expect(undone[0]).toBe(21)
  })

  it('clear empties both stacks', () => {
    const { push, undo, clear, canUndo, canRedo } = useUndoRedo()
    push(record(1))
    push(record(2))
    undo()

    clear()

    expect(canUndo.value).toBe(false)
    expect(canRedo.value).toBe(false)
  })
})
