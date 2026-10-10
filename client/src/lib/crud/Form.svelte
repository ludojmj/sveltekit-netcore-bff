<script>
  // CommonForm.svelte
  import { resolve } from '$app/paths';
  import { onMount } from 'svelte';
  import { page } from '$app/state';
  import { isLoading } from '#lib/store.js';
  import { crud } from '#lib/const.js';
  import { crudApiCallAsync, getItemAsync } from '#lib/api.js';
  import { handleEscape, handleFormError, setFocus } from '#lib/tools.js';
  import Error from '#lib/common/Error.svelte';
  import Loading from '#lib/common/Loading.svelte';
  import Header from '#lib/common/Header.svelte';

  let { crudTitle, disabled } = $props();

  const id = page.params.id ?? '';

  /** @type {{ id?: string, error?: string, label?: string, description?: string, otherInfo?: string, user?: { givenName: string } }} */
  let stuffDatum = $state({});
  let initialDatum = {};
  onMount(async () => {
    const itemObj = await getItemAsync(crudTitle, id);
    stuffDatum = itemObj.item;
    initialDatum = itemObj.initialItem;
  });

  let error = $state('');
  const handleSubmitAsync = async () => {
    error = handleFormError(crudTitle, stuffDatum, initialDatum);
    if (error) {
      return;
    }

    stuffDatum = await crudApiCallAsync(crudTitle, stuffDatum);
  };
</script>

{#if stuffDatum?.error}
  <Error msgErr={stuffDatum.error} />
{:else if $isLoading || !stuffDatum}
  <Loading />
{:else}
  <Header />
  <section
    class="panel {(crudTitle === crud.CREATE && 'is-success') ||
      (crudTitle === crud.READ && 'is-info') ||
      (crudTitle === crud.UPDATE && 'is-warning') ||
      (crudTitle === crud.DELETE && 'is-danger')}"
  >
    <p class="panel-heading">{crudTitle}</p>
    <form class="box" onsubmit={(event) => { event.preventDefault(); handleSubmitAsync(); }}>
      <label class="label" for="user">Owner:</label>
      <input
        onkeyup={(event) => { event.preventDefault(); handleEscape(event); }}
        class="input has-text-weight-bold"
        type="text"
        id="user"
        name="user"
        value={stuffDatum.user ? stuffDatum.user.givenName : 'Current user'}
        aria-label="User"
        disabled
      />

      <label class="label" for="label">Label:</label>
      <input
        onkeyup={(event) => { event.preventDefault(); handleEscape(event); }}
        bind:value={stuffDatum.label}
        use:setFocus
        minlength="3"
        maxlength="80"
        required
        class="input"
        type="text"
        maxLength="79"
        placeholder="Label"
        {disabled}
      />

      <label class="label" for="description">Description:</label>
      <input
        onkeyup={(event) => { event.preventDefault(); handleEscape(event); }}
        bind:value={stuffDatum.description}
        class="input"
        type="text"
        maxLength="79"
        placeholder="Description"
        {disabled}
      />

      <label class="label" for="otherInfo">Other info:</label>
      <textarea
        onkeyup={(event) => { event.preventDefault(); handleEscape(event); }}
        bind:value={stuffDatum.otherInfo}
        class="textarea"
        rows="5"
        maxLength="399"
        placeholder="Other info"
        {disabled}
      ></textarea>

      <footer>
        <a href={resolve('/')} class="button is-danger">Cancel</a>
        <button class="button is-success" type="submit">Confirm</button>
      </footer>

      {#if error}
        <div class="notification is-danger is-light">
          {error}
          <button class="delete" aria-label="close" onclick={() => (error = '')}></button>
        </div>
      {/if}
    </form>
  </section>
{/if}
