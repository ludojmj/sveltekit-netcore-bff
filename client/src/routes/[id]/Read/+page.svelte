<script>
  // Read.svelte
  import { crud } from '#lib/const.js';
  import Form from '#lib/crud/Form.svelte';

  const crudTitle = crud.READ;
  const disabled = true;
</script>

<Form {crudTitle} {disabled} />
