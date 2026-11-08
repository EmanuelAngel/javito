<script lang="ts">
	import type { EstadoDeRutinas } from '$lib/rutinas.svelte';

	// El mismo formulario da de alta (HU-RUT-01) y corrige (HU-RUT-05), como el de hábitos.
	let { rutinas }: { rutinas: EstadoDeRutinas } = $props();

	const editando = $derived(rutinas.estado.idEnEdicion !== null);

	function enviar(evento: SubmitEvent): void {
		evento.preventDefault();
		void (editando ? rutinas.guardarEdicion() : rutinas.crear());
	}
</script>

<form class="flex flex-col gap-2 rounded-xl border border-border bg-card p-4" onsubmit={enviar}>
	<h2 class="text-sm font-medium">{editando ? 'Editar rutina' : 'Nueva rutina'}</h2>

	<label class="text-xs text-muted-foreground" for="nombre-de-rutina">Nombre</label>
	<input
		id="nombre-de-rutina"
		class="rounded-lg border border-border bg-background px-3 py-2 text-sm"
		maxlength="60"
		required
		bind:value={rutinas.estado.formulario.nombre}
	/>
	{#if rutinas.estado.errores.campos.nombre}
		<p class="text-xs text-destructive">{rutinas.estado.errores.campos.nombre}</p>
	{/if}

	<label class="text-xs text-muted-foreground" for="descripcion-de-rutina">
		Descripción (opcional)
	</label>
	<textarea
		id="descripcion-de-rutina"
		class="rounded-lg border border-border bg-background px-3 py-2 text-sm"
		rows="2"
		maxlength="255"
		bind:value={rutinas.estado.formulario.descripcion}></textarea>

	{#if rutinas.estado.errores.mensaje}
		<p class="text-xs text-destructive">{rutinas.estado.errores.mensaje}</p>
	{/if}

	<button
		type="submit"
		disabled={rutinas.estado.enviando}
		class="rounded-lg bg-primary px-3 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
	>
		{editando ? 'Guardar cambios' : 'Crear rutina'}
	</button>

	{#if editando}
		<button
			type="button"
			class="rounded-lg border border-border px-3 py-2 text-sm transition-colors hover:bg-accent"
			onclick={rutinas.cancelar}
		>
			Cancelar
		</button>
	{/if}
</form>
