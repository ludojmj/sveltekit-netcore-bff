<script>
  // Error.svelte
  import { userInfo } from '$lib/store.js';
  import { apiErrMsg } from '$lib/const.js';
  import { apiLogin } from '$lib/api.js';
  export let msgErr;

  $: if (msgErr === apiErrMsg.unauthorized) $userInfo = null;
</script>

{#if msgErr}
  <div class="notification is-danger is-light">
    {msgErr}
    <button class="delete" aria-label="close" on:click={apiLogin}></button>
  </div>
{/if}
