<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { estadoDelCatalogo } from '$lib/catalogo-de-articulos.svelte';
	import CatalogoVacio from '$lib/components/catalogo-vacio.svelte';
	import GrillaDeArticulos from '$lib/components/grilla-de-articulos.svelte';
	import { Alert, AlertAction, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';
	import { Spinner } from '$lib/components/ui/spinner/index.js';
	import { leerEnteroPositivo, leerPagina } from '$lib/paginado.svelte';

	// El estado vive en la URL (HU-ART-05: volver al catálogo conserva página y filtro): es la única
	// fuente, y todo cambio pasa por `navegar`. Pedidos y estado están en `catalogo-de-articulos`.
	const pagina = $derived(leerPagina(page.url.searchParams.get('pagina')));
	const categoriaId = $derived(leerEnteroPositivo(page.url.searchParams.get('categoriaId')));

	const pantalla = estadoDelCatalogo(resolve('/(app)/articulos'));
	const { catalogo, categorias } = pantalla;

	const cargando = $derived(categorias.cargando || catalogo.estado.cargando);
	const mensajeError = $derived(categorias.error || catalogo.estado.mensajeError);

	onMount(() => void pantalla.cargarCategorias());

	// Sigue página y filtro, y nada más: `cargar` sólo escribe estado y no se vuelve a disparar.
	$effect(() => void catalogo.cargar(pagina, categoriaId));

	function reintentar() {
		void pantalla.cargarCategorias();
		void catalogo.cargar(pagina, categoriaId);
	}

	const irAPagina = (destino: number) => pantalla.navegar({ categoriaId, pagina: destino });

	// Cambiar el filtro vuelve a la página 1: la 3 de todo el catálogo no es la 3 de una categoría.
	function elegirCategoria(evento: Event) {
		const valor = (evento.currentTarget as HTMLSelectElement).value;

		pantalla.navegar({ categoriaId: valor === '' ? null : Number(valor), pagina: 1 });
	}
</script>

<svelte:head>
	<title>Javito · Artículos</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Artículos</h1>
		{#if catalogo.estado.pagina && !cargando}
			{@const total = catalogo.estado.pagina.totalDeRegistros}
			<p class="text-sm text-muted-foreground">
				{total}
				{total === 1 ? 'artículo' : 'artículos'}
				{categoriaId === null ? 'en total' : 'en esta categoría'}
			</p>
		{/if}
	</header>

	<!-- Un desplegable con las activas: las categorías nunca se buscan ni se paginan (ux-brief.md). -->
	<div class="flex flex-col gap-1.5 sm:max-w-xs">
		<label class="text-sm font-medium" for="catalogo-categoria">Categoría</label>
		<NativeSelect
			id="catalogo-categoria"
			class="w-full"
			value={categoriaId === null ? '' : String(categoriaId)}
			onchange={elegirCategoria}
		>
			<NativeSelectOption value="">Todas las categorías</NativeSelectOption>
			{#each categorias.lista as categoria (categoria.id)}
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

	{#if cargando && catalogo.estado.pagina === null}
		<!-- Primera carga: nunca una pantalla en blanco mientras se pide. -->
		<div class="flex items-center gap-2 py-10 text-sm text-muted-foreground">
			<Spinner aria-label="Cargando artículos" />
			<span>Cargando artículos…</span>
		</div>
	{:else if catalogo.estado.pagina}
		{@const actual = catalogo.estado.pagina}
		<div class="flex flex-col gap-4" aria-busy={cargando}>
			{#if !catalogo.hayElementos}
				<CatalogoVacio
					paginaInexistente={catalogo.paginaInexistente}
					filtrado={categoriaId !== null}
					pagina={actual.pagina}
					totalDePaginas={actual.totalDePaginas}
					alIrALaUltima={() => irAPagina(catalogo.ultimaPagina)}
				/>
			{:else}
				<GrillaDeArticulos
					{actual}
					{cargando}
					categoriaDe={pantalla.nombreDe}
					detalleDe={(id) => pantalla.detalleDe(id, { categoriaId, pagina })}
					alCambiarDePagina={irAPagina}
				/>
			{/if}
		</div>
	{/if}
</main>
