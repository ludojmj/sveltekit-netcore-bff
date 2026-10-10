<script>
  // CrudButtons.svelte
  import { resolve } from '$app/paths';
  import { crud } from '#lib/const.js';
  import { userInfo } from '#lib/store.js';

  let { stuffDatum } = $props();
</script>

<a href={resolve(/** @type {any} */ (`/${stuffDatum.id}/${crud.READ}`))} class="button is-small is-info"
  >{crud.READ}</a
>
{#if $userInfo && stuffDatum.user.id === $userInfo.id}
  <a
    href={resolve(/** @type {any} */ (`/${stuffDatum.id}/${crud.UPDATE}`))}
    class="button is-small is-warning">{crud.UPDATE}</a
  >
  <a href={resolve(/** @type {any} */ (`/${stuffDatum.id}/${crud.DELETE}`))} class="button is-small is-danger"
    >{crud.DELETE}</a
  >
{:else}
  <i class="is-size-7 has-background-danger-light">
    Owned by:
    {stuffDatum.user.givenName}
    {stuffDatum.user.familyName}
  </i>
{/if}
