import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ConfirmDialog from './ConfirmDialog.vue'

describe('ConfirmDialog', () => {
  it('renders nothing when open is false', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { open: false, title: 'Delete sheet', message: 'Are you sure?' }
    })
    expect(wrapper.find('[data-testid="confirm-dialog"]').exists()).toBe(false)
  })

  it('renders the title and message when open', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { open: true, title: 'Delete sheet', message: 'Are you sure?' }
    })
    expect(wrapper.text()).toContain('Delete sheet')
    expect(wrapper.text()).toContain('Are you sure?')
  })

  it('emits confirm when the confirm button is clicked', async () => {
    const wrapper = mount(ConfirmDialog, {
      props: { open: true, title: 'Delete sheet', message: 'Are you sure?' }
    })
    await wrapper.find('[data-testid="confirm-dialog-confirm"]').trigger('click')
    expect(wrapper.emitted('confirm')).toHaveLength(1)
  })

  it('emits cancel when the cancel button is clicked', async () => {
    const wrapper = mount(ConfirmDialog, {
      props: { open: true, title: 'Delete sheet', message: 'Are you sure?' }
    })
    await wrapper.find('[data-testid="confirm-dialog-cancel"]').trigger('click')
    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })
})
