<script>
  // Delete.svelte
  import { crud } from '#lib/const.js';
  import Form from '#lib/crud/Form.svelte';

  const crudTitle = crud.DELETE;
  const disabled = true;
</script>

<Form {crudTitle} {disabled} />
