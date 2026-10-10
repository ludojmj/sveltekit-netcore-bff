<script>
  // FormWrapper.svelte
  import { crud } from '#lib/const.js';

  let { crudCmd, children } = $props();

  let localStyle = $derived(
    crudCmd === crud.READ
      ? 'is-info'
      : crudCmd === crud.UPDATE
        ? 'is-warning'
        : crudCmd === crud.DELETE
          ? 'is-danger'
          : 'is-success'
  );
</script>

<section class="panel {localStyle}">
  <p class="panel-heading">
    {crudCmd}
  </p>
  {@render children()}
</section>
