<script lang="ts">
	// La categoría como etiqueta, no como estado. Se dibuja con borde y sin relleno a propósito: los
	// estados de la fila (la frecuencia, la cuota, «Archivado») van con relleno, así el ojo separa lo
	// que la fila *es* de lo que la fila *está*. Sin `data-slot="badge"`: no es un `Badge` de shadcn,
	// y las pruebas de las listas buscan el primer badge para leer el progreso de la semana.
	let { nombre }: { nombre: string | null } = $props();
</script>

{#if nombre}
	<span
		data-slot="chip-categoria"
		class="inline-flex items-center rounded-full border border-border bg-background px-2 py-0.5 text-xs font-medium text-foreground"
	>
		{nombre}
	</span>
{:else}
	<span data-slot="chip-categoria" class="text-xs text-muted-foreground">Sin categoría</span>
{/if}
