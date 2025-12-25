<script>
  // +error.svelte
  import Header from '$lib/common/Header.svelte';
  import Error from '$lib/common/Error.svelte';
</script>

<Header />
<Error msgErr="I got lost." hasReset={true} />
