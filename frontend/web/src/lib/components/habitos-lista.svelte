<script lang="ts">
	import type { Categoria } from '$lib/api/categorias';
	import type { Habito } from '$lib/api/habitos';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';

	let {
		activos,
		archivados,
		categorias,
		alEditar,
		alArchivar,
		alReactivar,
		idCambiandoActivo,
		error
	}: {
		activos: Habito[];
		archivados: Habito[];
		categorias: Categoria[];
		// La lista no edita: avisa qué fila se quiere corregir y el formulario hace el resto (S1).
		alEditar: (habito: Habito) => void;
		// Archivar y reactivar también los resuelve el estado de la pantalla (HU-HAB-04).
		alArchivar: (habito: Habito) => void;
		alReactivar: (habito: Habito) => void;
		idCambiandoActivo: number | null;
		error: string;
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
					<li
						data-test="fila-habito"
						class="flex items-center justify-between gap-3 rounded-xl border border-border bg-card px-5 py-4"
					>
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

						<div class="flex shrink-0 gap-2">
							<Button type="button" variant="outline" size="sm" onclick={() => alEditar(habito)}>
								Editar
							</Button>
							<!-- Archivar no borra nada (L9): por eso no pide confirmación y se deshace con Reactivar. -->
							<Button
								type="button"
								variant="ghost"
								size="sm"
								disabled={idCambiandoActivo === habito.id}
								onclick={() => alArchivar(habito)}
							>
								Archivar
							</Button>
						</div>
					</li>
				{/each}
			</ul>
		{/if}

		{#if error !== ''}
			<p role="alert" class="text-sm text-destructive">{error}</p>
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
						class="flex items-center justify-between gap-3 rounded-xl border border-border bg-card px-5 py-4 opacity-70"
					>
						<div class="flex min-w-0 flex-col gap-1">
							<span class="truncate font-medium">{habito.titulo}</span>
							<div class="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
								<span>{nombreDe(habito.categoriaId)}</span>
								<Badge variant="secondary">{frecuenciaDe(habito)}</Badge>
								<Badge variant="outline">Archivado</Badge>
							</div>
						</div>

						<!-- Un archivado también se edita (L5): lo que no puede es marcarse. -->
						<div class="flex shrink-0 gap-2">
							<Button type="button" variant="outline" size="sm" onclick={() => alEditar(habito)}>
								Editar
							</Button>
							<Button
								type="button"
								variant="outline"
								size="sm"
								disabled={idCambiandoActivo === habito.id}
								onclick={() => alReactivar(habito)}
							>
								Reactivar
							</Button>
						</div>
					</li>
				{/each}
			</ul>
		</section>
	{/if}
</section>
