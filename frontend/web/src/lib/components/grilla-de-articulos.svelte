<script lang="ts">
	import type { ArticuloDelCatalogo, Pagina } from '$lib/api/articulos';
	import Paginador from '$lib/components/paginador.svelte';
	import TarjetaDeArticulo from '$lib/components/tarjeta-de-articulo.svelte';
	import { Spinner } from '$lib/components/ui/spinner/index.js';

	// La página del catálogo con artículos: las tarjetas y, si hay más de una página, el paginador.
	type Props = {
		actual: Pagina<ArticuloDelCatalogo>;
		cargando: boolean;
		categoriaDe: (categoriaId: number) => string;
		alCambiarDePagina: (pagina: number) => void;
	};

	let { actual, cargando, categoriaDe, alCambiarDePagina }: Props = $props();
</script>

{#if cargando}
	<!-- Cambio de página o de filtro: la lista anterior queda mientras llega la nueva. -->
	<div class="flex items-center gap-2 text-sm text-muted-foreground">
		<Spinner aria-label="Cargando artículos" />
		<span>Cargando artículos…</span>
	</div>
{/if}

<ul class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
	{#each actual.elementos as articulo (articulo.id)}
		<li>
			<TarjetaDeArticulo {articulo} categoria={categoriaDe(articulo.categoriaId)} />
		</li>
	{/each}
</ul>

{#if actual.totalDePaginas > 1}
	<Paginador
		pagina={actual.pagina}
		totalDePaginas={actual.totalDePaginas}
		alCambiar={alCambiarDePagina}
	/>
{/if}
