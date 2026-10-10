<script>
  // +layout.svelte
  import './variables.scss';
  import favicon from '#lib/assets/favicon.svg';

  let { children } = $props();
</script>

<svelte:head>
  <title>sveltekit-netcore-bff</title>
  <meta name="description" content="sveltekit-netcore-bff" />
  <meta name="keywords" content="SvelteKit .Net10 BFF" />
  <link rel="icon" href={favicon} />
</svelte:head>

<div class="container">
  {@render children()}
</div>
