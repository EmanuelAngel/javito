<script lang="ts">
	import type { Categoria } from '$lib/api/categorias';
	import type { Habito } from '$lib/api/habitos';
	import ChipDeCategoria from '$lib/components/chip-de-categoria.svelte';
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

	// El nombre viaja por id: un hábito archivado puede apuntar a una categoría inactiva, y aun así se
	// muestra su nombre. La ausencia la dibuja el chip, que es el único lugar que decide ese texto.
	function categoriaDe(id: number | null): string | null {
		return categorias.find((categoria) => categoria.id === id)?.nombre ?? null;
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
						class="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-5 sm:flex-row sm:items-center sm:justify-between sm:gap-5 sm:px-6"
					>
						<div class="flex min-w-0 flex-col gap-2.5">
							<span class="leading-snug font-medium text-pretty">{habito.titulo}</span>
							<div
								class="flex flex-wrap items-center gap-x-2 gap-y-2 text-sm text-muted-foreground"
							>
								<ChipDeCategoria nombre={categoriaDe(habito.categoriaId)} />
								<Badge variant="secondary">{frecuenciaDe(habito)}</Badge>
								{#if habito.tipo === 'contador'}
									<Badge variant="outline">meta {habito.meta} {habito.unidad}</Badge>
								{/if}
							</div>
						</div>

						<!-- En angosto las dos acciones se reparten el ancho: son de dedo, no de mouse. -->
						<div class="flex gap-2 sm:shrink-0">
							<Button
								type="button"
								variant="outline"
								size="sm"
								class="flex-1 sm:flex-none"
								onclick={() => alEditar(habito)}
							>
								Editar
							</Button>
							<!-- Archivar no borra nada (L9): por eso no pide confirmación y se deshace con
								Reactivar. Va con el mismo borde que Editar: al compartir el ancho en el celular,
								un control sin contorno se lee como texto suelto y no invita a tocarlo. -->
							<Button
								type="button"
								variant="outline"
								size="sm"
								class="flex-1 sm:flex-none"
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
						class="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-5 opacity-70 sm:flex-row sm:items-center sm:justify-between sm:gap-5 sm:px-6"
					>
						<div class="flex min-w-0 flex-col gap-2.5">
							<span class="leading-snug font-medium text-pretty">{habito.titulo}</span>
							<div
								class="flex flex-wrap items-center gap-x-2 gap-y-2 text-sm text-muted-foreground"
							>
								<ChipDeCategoria nombre={categoriaDe(habito.categoriaId)} />
								<Badge variant="secondary">{frecuenciaDe(habito)}</Badge>
								<Badge variant="outline">Archivado</Badge>
							</div>
						</div>

						<!-- Un archivado también se edita (L5): lo que no puede es marcarse. -->
						<div class="flex gap-2 sm:shrink-0">
							<Button
								type="button"
								variant="outline"
								size="sm"
								class="flex-1 sm:flex-none"
								onclick={() => alEditar(habito)}
							>
								Editar
							</Button>
							<Button
								type="button"
								variant="outline"
								size="sm"
								class="flex-1 sm:flex-none"
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
