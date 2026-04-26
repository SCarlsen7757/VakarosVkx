/** Dispatch this after any action that changes notification counts. */
export function triggerNotificationRefresh() {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new CustomEvent("vakaros:notifications-changed"));
  }
}
