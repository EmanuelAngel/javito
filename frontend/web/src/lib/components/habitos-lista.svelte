<script lang="ts">
	import type { Categoria } from '$lib/api/categorias';
	import type { Habito } from '$lib/api/habitos';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';

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
			<Empty class="rounded-xl border border-border bg-card">
				<EmptyHeader>
					<EmptyTitle>Sin hábitos activos</EmptyTitle>
					<EmptyDescription>
						Todavía no tenés hábitos activos. Creá el primero desde el formulario.
					</EmptyDescription>
				</EmptyHeader>
			</Empty>
		{:else}
			<ul class="flex flex-col gap-3">
				{#each activos as habito (habito.id)}
					<li data-test="fila-habito" class="rounded-xl border border-border bg-card px-5 py-4">
						<div class="flex min-w-0 flex-col gap-1">
							<span class="truncate font-medium">{habito.titulo}</span>
							<div class="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
								<span>{nombreDe(habito.categoriaId)}</span>
								<Badge variant="secondary">{frecuenciaDe(habito)}</Badge>
								{#if habito.tipo === 'contador'}
									<Badge variant="outline">meta {habito.meta} {habito.unidad}</Badge>
								{/if}
							</div>
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
							<div class="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
								<span>{nombreDe(habito.categoriaId)}</span>
								<Badge variant="secondary">{frecuenciaDe(habito)}</Badge>
								<Badge variant="outline">Archivado</Badge>
							</div>
						</div>
					</li>
				{/each}
			</ul>
		</section>
	{/if}
</section>
