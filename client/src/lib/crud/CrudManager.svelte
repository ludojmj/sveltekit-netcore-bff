<script>
  // CrudManager.svelte
  import { resolve } from '$app/paths';
  import { crud } from '#lib/const.js';
  import { isLoading } from '#lib/store.js';
  import { apiGetStuffListAsync, apiSearchStuffAsync, apiGotoPageAsync } from '#lib/api.js';
  import List from '#lib/crud/List.svelte';
  import Error from '#lib/common/Error.svelte';
  import Loading from '#lib/common/Loading.svelte';
  import Header from '#lib/common/Header.svelte';

  let { initStuff } = $props();

  let searchTerm = $state('');
  let stuff = $derived(initStuff);

  /** @param {HTMLElement} el */
  const init = (el) => {
    el.focus();
  };

  const handleResetAsync = async () => {
    searchTerm = '';
    stuff = await apiGetStuffListAsync();
  };

  /** @param {KeyboardEvent} event */
  const handleEscapeOrEnterAsync = async (event) => {
    if (event.code == 'Escape') {
      handleResetAsync();
    }

    if (event.code == 'Enter' || event.code == 'NumpadEnter') {
      handleSearchAsync();
    }
  };

  const handleSearchAsync = async () => {
    if (searchTerm) {
      stuff = await apiSearchStuffAsync(searchTerm);
      return;
    }

    handleResetAsync();
  };

  /** @param {MouseEvent} event */
  const handlePageAsync = async (event) => {
    searchTerm = '';
    const button = /** @type {HTMLButtonElement} */ (event.currentTarget);
    const currentPage = stuff.page ?? 0;
    const page = button.value === '+' ? currentPage + 1 : currentPage - 1;
    stuff = await apiGotoPageAsync(page);
  };
</script>

<Header />
{#if stuff.error}
  <Error msgErr={stuff.error} />
{:else if $isLoading || !stuff.datumList}
  <Loading />
{:else}
  <section class="box has-background-primary">
    <form
      onsubmit={(event) => {
        event.preventDefault();
        handleSearchAsync();
      }}
    >
      <div class="columns">
        <div class="column is-4">
          <div class="pagination">
            <ul class="pagination-list">
              <li>
                <button
                  class="button is-info"
                  value="-"
                  onclick={(event) => {
                    event.preventDefault();
                    handlePageAsync(event);
                  }}
                  disabled={!stuff.page || stuff.page === 1}
                >
                  &laquo;
                </button>
              </li>
              <li>
                <div class="input field">
                  Page {stuff.page ? stuff.page : 0}/{stuff.totalPages ? stuff.totalPages : 0}
                </div>
              </li>
              <li>
                <button
                  class="button is-info"
                  value="+"
                  onclick={(event) => {
                    event.preventDefault();
                    handlePageAsync(event);
                  }}
                  disabled={stuff.page === stuff.totalPages}
                >
                  &raquo;
                </button>
              </li>
            </ul>
          </div>
        </div>

        <div class="column is-6">
          <div class="field has-addons">
            <div class="control has-icons-right">
              <input
                bind:value={searchTerm}
                use:init
                onkeyup={(event) => {
                  event.preventDefault();
                  handleEscapeOrEnterAsync(event);
                }}
                class="input"
                type="search"
                placeholder="Filter"
                aria-label="Filter"
                maxLength="20"
              />
              <span class="icon is-right">
                <i
                  onclick={handleResetAsync}
                  onkeydown={handleResetAsync}
                  class="delete"
                  tabindex="0"
                  role="button"
                ></i>
              </span>
            </div>
            <div class="control">
              <button class="button is-info" type="submit">Search</button>
            </div>
          </div>
        </div>

        <div class="column is-2">
          <a href={resolve(/** @type {any} */ (`/${crud.CREATE}`))} class="button is-success">
            {crud.CREATE}
          </a>
        </div>
      </div>
    </form>
  </section>
  <section class="box has-background-primary">
    <List {stuff} />
  </section>
{/if}
