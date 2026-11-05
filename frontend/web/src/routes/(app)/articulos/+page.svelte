<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { listar, type ArticuloDelCatalogo, type Pagina } from '$lib/api/articulos';
	import { listar as listarCategorias, type Categoria } from '$lib/api/categorias';
	import { ErrorDeApi } from '$lib/api/pedir';
	import Paginador from '$lib/components/paginador.svelte';
	import TarjetaDeArticulo from '$lib/components/tarjeta-de-articulo.svelte';
	import { Alert, AlertAction, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';
	import { Spinner } from '$lib/components/ui/spinner/index.js';

	// El estado de la pantalla vive en la URL, no en variables locales: así el detalle puede ofrecer
	// «Volver al catálogo» conservando página y filtro (HU-ART-05), el catálogo se puede enlazar tal
	// como se está viendo, y recargar no pierde nada. `page.url.searchParams` es la única fuente; todo
	// cambio de estado pasa por `goto`.
	const pagina = $derived(leerPagina(page.url.searchParams.get('pagina')));
	const categoriaId = $derived(leerCategoriaId(page.url.searchParams.get('categoriaId')));

	/** La ruta base, resuelta una vez. La query la arma el estado y se le agrega abajo. */
	const rutaDelCatalogo = resolve('/(app)/articulos');

	/**
	 * Una `pagina` que no es un entero positivo se lee como la primera. El servidor rechazaría
	 * `?pagina=0` con un 400 de validación, y una URL escrita a mano no debería romper la pantalla.
	 */
	function leerPagina(valor: string | null): number {
		const numero = Number(valor);

		return Number.isInteger(numero) && numero > 0 ? numero : 1;
	}

	/** Igual con el filtro: sin `categoriaId` válido se muestran todas las categorías. */
	function leerCategoriaId(valor: string | null): number | null {
		const numero = Number(valor);

		return Number.isInteger(numero) && numero > 0 ? numero : null;
	}

	/**
	 * La query del catálogo: sólo los dos parámetros que la pantalla usa, en un orden fijo para que
	 * la misma vista dé siempre la misma URL. La primera página y «todas las categorías» son la
	 * ausencia del parámetro, no un valor más.
	 */
	function consulta(destino: { categoriaId: number | null; pagina: number }): string {
		const partes: string[] = [];

		if (destino.categoriaId !== null) partes.push(`categoriaId=${destino.categoriaId}`);
		if (destino.pagina > 1) partes.push(`pagina=${destino.pagina}`);

		return partes.length === 0 ? '' : `?${partes.join('&')}`;
	}

	let categorias = $state<Categoria[]>([]);
	let catalogo = $state<Pagina<ArticuloDelCatalogo> | null>(null);
	let cargandoCategorias = $state(true);
	let cargandoCatalogo = $state(true);
	let mensajeError = $state('');

	const cargando = $derived(cargandoCategorias || cargandoCatalogo);

	const nombresDeCategoria = $derived(new Map(categorias.map((c) => [c.id, c.nombre])));

	function nombreDeCategoriaDe(id: number): string {
		return nombresDeCategoria.get(id) ?? 'Sin categoría';
	}

	function mensajeDeError(error: unknown): string {
		return error instanceof ErrorDeApi ? error.message : 'No se pudo cargar el catálogo.';
	}

	// La lista de categorías no depende de la página ni del filtro: se pide una vez al montar. El
	// filtro sólo ofrece las activas, que son las únicas que clasifican algo nuevo (HU-CAT-04).
	onMount(() => {
		void cargarCategorias();
	});

	async function cargarCategorias() {
		cargandoCategorias = true;

		try {
			categorias = await listarCategorias(true);
		} catch (error) {
			mensajeError = mensajeDeError(error);
		} finally {
			cargandoCategorias = false;
		}
	}

	// Cada cambio de URL dispara una petición nueva sin esperar a la anterior: el turno descarta la
	// respuesta que llegó tarde, para que dos clics seguidos no dejen la página vieja en pantalla.
	let turno = 0;

	async function cargarCatalogo(paginaPedida: number, categoriaPedida: number | null) {
		const propio = ++turno;

		cargandoCatalogo = true;
		mensajeError = '';

		try {
			const respuesta = await listar({ pagina: paginaPedida, categoriaId: categoriaPedida });

			if (propio === turno) catalogo = respuesta;
		} catch (error) {
			if (propio !== turno) return;

			catalogo = null;
			mensajeError = mensajeDeError(error);
		} finally {
			if (propio === turno) cargandoCatalogo = false;
		}
	}

	// Se leen las dos dependencias antes de pedir nada: el effect sigue página y filtro, y nada más.
	// `cargarCatalogo` sólo escribe estado, así que no puede volver a dispararse a sí mismo.
	$effect(() => {
		const paginaPedida = pagina;
		const categoriaPedida = categoriaId;

		void cargarCatalogo(paginaPedida, categoriaPedida);
	});

	function reintentar() {
		void cargarCategorias();
		void cargarCatalogo(pagina, categoriaId);
	}

	/**
	 * Navega con la query pedida. Se reemplaza la entrada del historial para no llenarlo de páginas:
	 * el «atrás» del navegador tiene que salir del catálogo, no recorrer las seis que se miraron. Y
	 * no se resetea ni el scroll ni el foco, porque la pantalla no cambia de lugar: sólo la lista.
	 *
	 * SvelteKit 3 reemplazó `keepFocus`/`noScroll` por `reset` (y `replaceState` por `replace`).
	 */
	function navegar(destino: { categoriaId: number | null; pagina: number }) {
		// `resolve()` no puede tipar esta URL: se le agrega una query armada con el estado actual.
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		goto(`${rutaDelCatalogo}${consulta(destino)}`, {
			replace: true,
			reset: false
		});
	}

	function irAPagina(destino: number) {
		navegar({ categoriaId, pagina: destino });
	}

	function elegirCategoria(evento: Event) {
		const valor = (evento.currentTarget as HTMLSelectElement).value;

		// Cambiar el filtro vuelve a la página 1: la página 3 de todo el catálogo no es la página 3
		// de una categoría que tiene dos.
		navegar({ categoriaId: valor === '' ? null : Number(valor), pagina: 1 });
	}

	const hayElementos = $derived((catalogo?.elementos.length ?? 0) > 0);

	/**
	 * La página pedida quedó más allá del final —un enlace viejo, o alguien borró artículos mientras
	 * la lista estaba abierta—. El servidor devuelve vacío y la metadata honesta; la pantalla lo
	 * dice en vez de mentir con un «no hay artículos».
	 */
	const paginaInexistente = $derived(
		catalogo !== null && !hayElementos && catalogo.totalDeRegistros > 0
	);

	/** La última página que existe, para el atajo que sale de una página pasada del final. */
	const ultimaPagina = $derived(catalogo?.totalDePaginas ?? 1);
</script>

<svelte:head>
	<title>Javito · Artículos</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Artículos</h1>
		{#if catalogo && !cargando}
			<p class="text-sm text-muted-foreground">
				{catalogo.totalDeRegistros}
				{catalogo.totalDeRegistros === 1 ? 'artículo' : 'artículos'}
				{categoriaId === null ? 'en total' : 'en esta categoría'}
			</p>
		{/if}
	</header>

	<!-- El filtro es un desplegable con las activas: las categorías nunca se buscan ni se paginan
		 (ux-brief.md), y el catálogo no cambia mientras la pantalla está abierta. -->
	<div class="flex flex-col gap-1.5 sm:max-w-xs">
		<label class="text-sm font-medium" for="catalogo-categoria">Categoría</label>
		<NativeSelect
			id="catalogo-categoria"
			class="w-full"
			value={categoriaId === null ? '' : String(categoriaId)}
			onchange={elegirCategoria}
		>
			<NativeSelectOption value="">Todas las categorías</NativeSelectOption>
			{#each categorias as categoria (categoria.id)}
				<NativeSelectOption value={categoria.id}>{categoria.nombre}</NativeSelectOption>
			{/each}
		</NativeSelect>
	</div>

	{#if mensajeError}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{mensajeError}</AlertDescription>
			<AlertAction>
				<Button type="button" variant="outline" size="xs" onclick={reintentar}>Reintentar</Button>
			</AlertAction>
		</Alert>
	{/if}

	{#if cargando && catalogo === null}
		<!-- Primera carga: nunca una pantalla en blanco mientras se pide. -->
		<div class="flex items-center gap-2 py-10 text-sm text-muted-foreground">
			<Spinner aria-label="Cargando artículos" />
			<span>Cargando artículos…</span>
		</div>
	{:else if catalogo === null}
		<!-- Sin catálogo y sin error no hay nada que contar: el Alert de arriba ya lo dijo todo. -->
	{:else}
		<div class="flex flex-col gap-4" aria-busy={cargando}>
			{#if paginaInexistente}
				<Empty class="rounded-xl border border-border bg-card">
					<EmptyHeader>
						<EmptyTitle>Esta página no existe</EmptyTitle>
						<EmptyDescription>
							El catálogo no llega a la página {catalogo.pagina}: hay
							{catalogo.totalDePaginas}
							{catalogo.totalDePaginas === 1 ? 'página' : 'páginas'}.
						</EmptyDescription>
					</EmptyHeader>
					<Button type="button" variant="outline" onclick={() => irAPagina(ultimaPagina)}>
						Ir a la última página
					</Button>
				</Empty>
			{:else if !hayElementos}
				<Empty class="rounded-xl border border-border bg-card">
					<EmptyHeader>
						<EmptyTitle>
							{categoriaId === null
								? 'Todavía no hay artículos'
								: 'No hay artículos en esta categoría'}
						</EmptyTitle>
						<EmptyDescription>
							{categoriaId === null
								? 'Cuando se publique el primero va a aparecer acá, del más nuevo al más viejo.'
								: 'Probá con otra categoría o volvé a «Todas las categorías».'}
						</EmptyDescription>
					</EmptyHeader>
				</Empty>
			{:else}
				{#if cargando}
					<!-- Cambio de página o de filtro: la lista anterior queda en pantalla mientras
						 llega la nueva, y esto es lo que dice que algo está pasando. -->
					<div class="flex items-center gap-2 text-sm text-muted-foreground">
						<Spinner aria-label="Cargando artículos" />
						<span>Cargando artículos…</span>
					</div>
				{/if}

				<ul class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
					{#each catalogo.elementos as articulo (articulo.id)}
						<li>
							<TarjetaDeArticulo {articulo} categoria={nombreDeCategoriaDe(articulo.categoriaId)} />
						</li>
					{/each}
				</ul>

				{#if catalogo.totalDePaginas > 1}
					<Paginador
						pagina={catalogo.pagina}
						totalDePaginas={catalogo.totalDePaginas}
						alCambiar={irAPagina}
					/>
				{/if}
			{/if}
		</div>
	{/if}
</main>
