// tools.js
import { goto } from '$app/navigation';
import { crud } from '$lib/const.js';

export const setFocus = (elt) => {
  elt.focus();
};

export const handleEscape = (event) => {
  if (event.code == 'Escape') {
    goto('/');
  }
};

export const handleFormError = (title, item, initialItem) => {
  return title !== crud.READ && title !== crud.DELETE && JSON.stringify(item) === JSON.stringify(initialItem)
    ? 'No significant changes...'
    : '';
};

export async function unregisterServiceWorker() {
  if (!('serviceWorker' in navigator)) {
    return false;
  }

  try {
    const registrations = await navigator.serviceWorker.getRegistrations();
    let unregisteredCount = 0;

    for (const registration of registrations) {
      if (registration.active?.scriptURL.includes('/OidcServiceWorker.js')) {
        const success = await registration.unregister();
        if (success) {
          unregisteredCount++;
        }
      }
    }

    return unregisteredCount > 0;
  } catch {
    return false;
  }
}
