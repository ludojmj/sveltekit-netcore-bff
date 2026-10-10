<script>
  // Error.svelte
  import { userInfo } from '#lib/store.js';
  import { apiErrMsg } from '#lib/const.js';
  import { apiLogin } from '#lib/api.js';
  import Header from '#lib/common/Header.svelte';

  let { msgErr } = $props();

  $effect(() => {
    if (msgErr === apiErrMsg.unauthorized) $userInfo = null;
  });
</script>

<Header />
{#if msgErr}
  <div class="notification is-danger is-light">
    {msgErr}
    <button class="delete" aria-label="close" onclick={apiLogin}></button>
  </div>
{/if}
