<script lang="ts">
	import { resolve } from '$app/paths';
	import type { ArticuloDelCatalogo, Pagina } from '$lib/api/articulos';
	import Paginador from '$lib/components/paginador.svelte';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Table from '$lib/components/ui/table/index.js';

	// La página del listado de administración (S25): una fila por artículo, activo o no, con su estado
	// a la vista y «Reactivar» en los retirados. No sabe de pedidos: avisa y la pantalla decide.
	type Props = {
		actual: Pagina<ArticuloDelCatalogo>;
		categoriaDe: (categoriaId: number) => string;
		/** El id que se está reactivando, para deshabilitar sólo ese botón. */
		reactivando: number | null;
		alReactivar: (id: number) => void;
		alCambiarDePagina: (pagina: number) => void;
	};

	let { actual, categoriaDe, reactivando, alReactivar, alCambiarDePagina }: Props = $props();

	// aaaa-mm-dd → dd/mm/aaaa cortando la cadena, igual que la tarjeta: es un día, no un instante.
	const dia = (fecha: string) => fecha.split('-').reverse().join('/');
</script>

<div class="overflow-x-auto rounded-xl border border-border bg-card">
	<Table.Root>
		<Table.Header>
			<Table.Row>
				<Table.Head>Título</Table.Head>
				<Table.Head>Categoría</Table.Head>
				<Table.Head>Publicado</Table.Head>
				<Table.Head>Estado</Table.Head>
				<Table.Head class="text-right">Acciones</Table.Head>
			</Table.Row>
		</Table.Header>
		<Table.Body>
			{#each actual.elementos as articulo (articulo.id)}
				<Table.Row>
					<Table.Cell class="max-w-xs whitespace-normal">
						<!-- Un retirado no se abre por URL (S3): sin enlace, para no ofrecer un 404. -->
						{#if articulo.activo}
							<a
								href={resolve('/(app)/articulos/[id]', { id: String(articulo.id) })}
								class="font-medium underline-offset-4 hover:underline"
							>
								{articulo.titulo}
							</a>
						{:else}
							<span class="font-medium text-muted-foreground">{articulo.titulo}</span>
						{/if}
					</Table.Cell>
					<Table.Cell class="text-muted-foreground">{categoriaDe(articulo.categoriaId)}</Table.Cell>
					<Table.Cell class="text-muted-foreground tabular-nums">
						<time datetime={articulo.fechaPublicacion}>{dia(articulo.fechaPublicacion)}</time>
					</Table.Cell>
					<Table.Cell>
						<Badge variant={articulo.activo ? 'secondary' : 'outline'}>
							{articulo.activo ? 'Activo' : 'Inactivo'}
						</Badge>
					</Table.Cell>
					<Table.Cell>
						<div class="flex justify-end">
							{#if !articulo.activo}
								<Button
									type="button"
									variant="outline"
									size="sm"
									disabled={reactivando === articulo.id}
									aria-label="Reactivar {articulo.titulo}"
									onclick={() => alReactivar(articulo.id)}
								>
									Reactivar
								</Button>
							{/if}
						</div>
					</Table.Cell>
				</Table.Row>
			{/each}
		</Table.Body>
	</Table.Root>
</div>

{#if actual.totalDePaginas > 1}
	<Paginador
		pagina={actual.pagina}
		totalDePaginas={actual.totalDePaginas}
		alCambiar={alCambiarDePagina}
		etiqueta="Paginación de artículos"
	/>
{/if}
