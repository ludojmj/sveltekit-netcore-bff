<script>
  // Create.svelte
  import { crud } from '#lib/const.js';
  import Form from '#lib/crud/Form.svelte';

  const crudTitle = crud.CREATE;
  const disabled = false;
</script>

<Form {crudTitle} {disabled} />
