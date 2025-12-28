<script>
  // +page.svelte
  import { onMount } from 'svelte';
  import { userInfo } from '$lib/store.js';
  import { apiGetUserInfoAsync, apiGetStuffListAsync } from '$lib/api.js';
  import { unregisterServiceWorker } from '$lib/tools.js';
  import Header from '$lib/common/Header.svelte';
  import CrudManager from '$lib/crud/CrudManager.svelte';
  import Error from '../lib/common/Error.svelte';

  let apiUserInfo = {};
  let initStuff = {};
  onMount(async () => {
    await unregisterServiceWorker();
    apiUserInfo = await apiGetUserInfoAsync();
    initStuff = await apiGetStuffListAsync();
  });
</script>

<Header />
{#if apiUserInfo.error}
  <Error msgErr={apiUserInfo.error} />
{:else}
  <CrudManager {initStuff} />
{/if}

<pre class="is-hidden">{JSON.stringify($userInfo, null, '\t')}</pre>
