// lib/store.js
import { writable } from 'svelte/store';

export const isLoading = writable(false);
/** @type {import('svelte/store').Writable<{id: string, name: string, givenName: string, familyName: string} | null>} */
export const userInfo = writable(null);
