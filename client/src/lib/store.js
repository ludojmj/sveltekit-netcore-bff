// lib/store.js
import { writable } from 'svelte/store';

export const isLoading = writable(false);
export const userInfo = writable(null);
