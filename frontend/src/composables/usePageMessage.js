import { onBeforeUnmount, ref } from "vue";

const EMPTY_PAGE_MESSAGE = { tone: "info", title: "", message: "" };

export function usePageMessage(duration = 5000) {
  const pageMessage = ref({ ...EMPTY_PAGE_MESSAGE });
  let timerId = null;

  const clearPageMessage = () => {
    if (timerId) {
      clearTimeout(timerId);
      timerId = null;
    }
    pageMessage.value = { ...EMPTY_PAGE_MESSAGE };
  };

  const showPageMessage = ({ tone = "info", title = "", message }) => {
    if (timerId) clearTimeout(timerId);
    pageMessage.value = { tone, title, message };
    timerId = setTimeout(() => {
      timerId = null;
      clearPageMessage();
    }, duration);
  };

  onBeforeUnmount(() => {
    if (timerId) clearTimeout(timerId);
  });

  return {
    pageMessage,
    clearPageMessage,
    showPageMessage,
  };
}
