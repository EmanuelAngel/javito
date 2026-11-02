<script lang="ts">
	import type { EstadoDeHabitos } from '$lib/habitos.svelte';

	let { estado, crear }: EstadoDeHabitos = $props();

	// Meta y unidad aparecen nomás con un contador; veces, con una frecuencia semanal (S6/S9).
	const esContador = $derived(estado.formulario.tipo === 'contador');
	const esSemanal = $derived(estado.formulario.frecuencia === 'semanal');

	// El desplegable muestra las activas: una categoría archivada no admite hábitos nuevos.
	const categoriasActivas = $derived(estado.categorias.filter((categoria) => categoria.activo));

	function mensajeDe(campo: string): string {
		return estado.errores.campos[campo] ?? '';
	}

	async function guardar(evento: SubmitEvent): Promise<void> {
		evento.preventDefault();
		await crear();
	}

	const claseDeCampo =
		'rounded-lg border border-border bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring';
</script>

<form
	data-test="formulario-habito"
	class="flex w-full flex-col gap-4 self-start rounded-xl border border-border bg-card p-6 lg:w-80"
	onsubmit={guardar}
>
	<h2 class="text-lg font-semibold">Nuevo hábito</h2>

	<label class="flex flex-col gap-1.5">
		<span class="text-sm font-medium">Título</span>
		<input
			data-test="campo-titulo"
			type="text"
			bind:value={estado.formulario.titulo}
			required
			maxlength="100"
			class={claseDeCampo}
		/>
		{#if mensajeDe('titulo')}
			<span class="text-sm text-destructive">{mensajeDe('titulo')}</span>
		{/if}
	</label>

	<label class="flex flex-col gap-1.5">
		<span class="text-sm font-medium">Categoría</span>
		<select bind:value={estado.formulario.categoriaId} required class={claseDeCampo}>
			<option value="" disabled>Elegí una categoría…</option>
			{#each categoriasActivas as categoria (categoria.id)}
				<option value={categoria.id}>{categoria.nombre}</option>
			{/each}
		</select>
		{#if mensajeDe('categoriaId')}
			<span class="text-sm text-destructive">{mensajeDe('categoriaId')}</span>
		{/if}
	</label>

	<label class="flex flex-col gap-1.5">
		<span class="text-sm font-medium">Tipo</span>
		<select bind:value={estado.formulario.tipo} class={claseDeCampo}>
			<option value="si_no">Sí o no</option>
			<option value="contador">Contador</option>
			<option value="abstinencia">Abstinencia</option>
		</select>
	</label>

	<label class="flex flex-col gap-1.5">
		<span class="text-sm font-medium">Frecuencia</span>
		<select bind:value={estado.formulario.frecuencia} class={claseDeCampo}>
			<option value="diaria">Todos los días</option>
			<option value="semanal">Varias veces por semana</option>
		</select>
	</label>

	{#if esSemanal}
		<label class="flex flex-col gap-1.5">
			<span class="text-sm font-medium">¿Cuántas veces?</span>
			<input
				data-test="campo-veces"
				type="number"
				bind:value={estado.formulario.veces}
				required
				min="1"
				max="6"
				step="1"
				class={claseDeCampo}
			/>
			{#if mensajeDe('veces')}
				<span class="text-sm text-destructive">{mensajeDe('veces')}</span>
			{/if}
		</label>
	{/if}

	{#if esContador}
		<div class="grid grid-cols-2 gap-3">
			<label class="flex flex-col gap-1.5">
				<span class="text-sm font-medium">Meta</span>
				<input
					data-test="campo-meta"
					type="number"
					bind:value={estado.formulario.meta}
					required
					min="1"
					step="1"
					class={claseDeCampo}
				/>
			</label>

			<label class="flex flex-col gap-1.5">
				<span class="text-sm font-medium">Unidad</span>
				<input
					data-test="campo-unidad"
					type="text"
					bind:value={estado.formulario.unidad}
					required
					maxlength="30"
					placeholder="páginas"
					class={claseDeCampo}
				/>
			</label>
		</div>

		{#if mensajeDe('meta')}
			<span class="text-sm text-destructive">{mensajeDe('meta')}</span>
		{/if}
		{#if mensajeDe('unidad')}
			<span class="text-sm text-destructive">{mensajeDe('unidad')}</span>
		{/if}
	{/if}

	<label class="flex flex-col gap-1.5">
		<span class="text-sm font-medium">Descripción</span>
		<input
			type="text"
			bind:value={estado.formulario.descripcion}
			maxlength="255"
			class={claseDeCampo}
		/>
		{#if mensajeDe('descripcion')}
			<span class="text-sm text-destructive">{mensajeDe('descripcion')}</span>
		{/if}
	</label>

	{#if estado.errores.mensaje}
		<p
			class="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive"
		>
			{estado.errores.mensaje}
		</p>
	{/if}

	<button
		type="submit"
		disabled={estado.enviando}
		class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-50"
	>
		Crear hábito
	</button>
</form>
