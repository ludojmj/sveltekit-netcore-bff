<script>
  // +page.svelte
  import { onMount } from 'svelte';
  import { userInfo } from '#lib/store.js';
  import { apiGetUserInfoAsync, apiGetStuffListAsync } from '#lib/api.js';
  import { unregisterServiceWorker } from '#lib/tools.js';
  import CrudManager from '#lib/crud/CrudManager.svelte';
  import Error from '#lib/common/Error.svelte';

  let apiUserInfo = $state(/** @type {{ error?: string }} */ ({}));
  let initStuff = $state(
    /** @type {{ error?: string, page?: number, totalPages?: number, datumList?: Array<{ id: string, label: string, description: string, createdAt?: string | null, updatedAt?: string | null, user: { id: string, givenName: string, familyName: string } }> }} */ ({})
  );
  onMount(async () => {
    await unregisterServiceWorker();
    apiUserInfo = await apiGetUserInfoAsync();
    initStuff = await apiGetStuffListAsync();
  });
</script>

{#if $userInfo}
  <CrudManager {initStuff} />
{:else}
  <Error msgErr={apiUserInfo.error ?? ''} />
{/if}

<pre class="is-hidden">{JSON.stringify($userInfo, null, '\t')}</pre>
