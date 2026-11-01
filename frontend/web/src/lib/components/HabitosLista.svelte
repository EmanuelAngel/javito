<script lang="ts">
	import type { Categoria } from '$lib/api/categorias';
	import type { Habito } from '$lib/api/habitos';

	let {
		activos,
		archivados,
		categorias
	}: {
		activos: Habito[];
		archivados: Habito[];
		categorias: Categoria[];
	} = $props();

	// El nombre viaja por id: un hábito archivado puede apuntar a una categoría inactiva,
	// y aun así se muestra su nombre.
	function nombreDe(id: number | null): string {
		return categorias.find((categoria) => categoria.id === id)?.nombre ?? 'Sin categoría';
	}

	function frecuenciaDe(habito: Habito): string {
		if (habito.frecuencia.tipo === 'semanal') {
			const veces = habito.frecuencia.veces ?? 0;

			return `${veces} ${veces === 1 ? 'vez' : 'veces'} por semana`;
		}

		return 'Todos los días';
	}
</script>

<section class="flex flex-1 flex-col gap-6">
	<section class="flex flex-col gap-3">
		<h2 class="text-lg font-semibold">Activos</h2>

		{#if activos.length === 0}
			<p class="rounded-xl border border-border bg-card px-5 py-6 text-sm text-muted-foreground">
				Todavía no tenés hábitos activos. Creá el primero desde el formulario.
			</p>
		{:else}
			<ul class="flex flex-col gap-3">
				{#each activos as habito (habito.id)}
					<li data-test="fila-habito" class="rounded-xl border border-border bg-card px-5 py-4">
						<div class="flex min-w-0 flex-col gap-1">
							<span class="truncate font-medium">{habito.titulo}</span>
							<span class="text-sm text-muted-foreground">
								{nombreDe(habito.categoriaId)} · {frecuenciaDe(habito)}
								{#if habito.tipo === 'contador'}
									· meta {habito.meta} {habito.unidad}
								{/if}
							</span>
						</div>
					</li>
				{/each}
			</ul>
		{/if}
	</section>

	<!-- Los archivados van aparte (S9): sin ellos la vista activa se llena, con ellos en su
			propio bloque se ven sin ensuciar. El bloque sólo existe si hay archivados. -->
	{#if archivados.length > 0}
		<section data-test="seccion-archivados" class="flex flex-col gap-3">
			<h2 class="text-lg font-semibold">Archivados</h2>

			<ul class="flex flex-col gap-3">
				{#each archivados as habito (habito.id)}
					<li
						data-test="fila-archivado"
						class="rounded-xl border border-border bg-card px-5 py-4 opacity-70"
					>
						<div class="flex min-w-0 flex-col gap-1">
							<span class="truncate font-medium">{habito.titulo}</span>
							<span class="text-sm text-muted-foreground">
								{nombreDe(habito.categoriaId)} · {frecuenciaDe(habito)}
							</span>
						</div>
					</li>
				{/each}
			</ul>
		</section>
	{/if}
</section>
