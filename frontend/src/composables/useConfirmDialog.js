import { ref } from 'vue'

export function useConfirmDialog() {
  const confirmOpen = ref(false)
  const confirmTitle = ref('Are you sure?')
  const confirmMessage = ref('')
  const confirmButton = ref('Confirm')
  const confirmTone = ref('danger')
  const pendingAction = ref(async () => {})

  const openConfirm = ({ title, message, confirmText, tone, action }) => {
    confirmTitle.value = title
    confirmMessage.value = message
    confirmButton.value = confirmText
    confirmTone.value = tone
    pendingAction.value = action
    confirmOpen.value = true
  }

  const runConfirm = async () => {
    await pendingAction.value()
    confirmOpen.value = false
  }

  return {
    confirmOpen,
    confirmTitle,
    confirmMessage,
    confirmButton,
    confirmTone,
    openConfirm,
    runConfirm
  }
}
