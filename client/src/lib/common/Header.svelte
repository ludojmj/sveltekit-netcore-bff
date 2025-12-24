<script>
  // Header.svelte
  import { userInfo } from '$lib/store.js';
  import Logo from '$lib/common/Logo.svelte';
  import Logout from '$lib/common/Logout.svelte';
</script>

<div class="columns is-tablet">
  <div class="column">
    <a data-sveltekit-reload href="/" class="navbar-item">
      <Logo />
    </a>
  </div>

  <div class="column">
    <h1 class="title">sveltekit-netcore-identity</h1>
  </div>

  {#if $userInfo}
    <div class="column has-text-right">
      <Logout />
    </div>
  {/if}
</div>
