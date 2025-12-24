<script>
  // Error.svelte
  import { userInfo } from '$lib/store.js';
  import { apiErrMsg } from '$lib/const.js';
  import { apiLogin } from '$lib/api.js';
  export let msgErr, hasReset;

  $: if (msgErr === apiErrMsg.unauthorized) $userInfo = null;
</script>

{#if msgErr}
  <div class="notification is-danger is-light">
    {msgErr}
    {#if hasReset}
      <button class="delete" aria-label="close" on:click={apiLogin}></button>
    {:else}
      <a data-sveltekit-reload href="/" class="delete" aria-label="close"></a>
    {/if}
  </div>
{/if}
