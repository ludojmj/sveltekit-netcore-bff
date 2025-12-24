<script>
  // Logout.svelte
  import { userInfo } from '$lib/store.js';
  import { apiLogoutAsync } from '$lib/api.js';
  import Loading from '$lib/common/Loading.svelte';
</script>

<button class="button is-danger" on:click={apiLogoutAsync}>
  Logout &nbsp;
  {#if $userInfo}
    <span class="tag is-rounded">
      {$userInfo.name}
    </span>
  {:else}
    <Loading />
  {/if}
</button>
