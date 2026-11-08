<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { estadoDeLaAdministracion } from '$lib/administracion-de-articulos.svelte';
	import TablaDeAdministracionDeArticulos from '$lib/components/tabla-de-administracion-de-articulos.svelte';
	import { Alert, AlertAction, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';
	import { Spinner } from '$lib/components/ui/spinner/index.js';
	import { leerPagina } from '$lib/paginado.svelte';

	// HU-ART-03: acá se ven los artículos retirados y se reactivan. La página vive en la URL, como en
	// el catálogo; pedidos y estado están en `administracion-de-articulos`.
	const pagina = $derived(leerPagina(page.url.searchParams.get('pagina')));

	const pantalla = estadoDeLaAdministracion(resolve('/(app)/admin/articulos'));
	const { listado, categorias, reactivacion } = pantalla;

	const mensajeError = $derived(categorias.error || listado.estado.mensajeError);

	onMount(() => void pantalla.cargarCategorias());

	$effect(() => void listado.cargar(pagina, null));

	function reintentar() {
		void pantalla.cargarCategorias();
		void listado.cargar(pagina, null);
	}
</script>

<svelte:head>
	<title>Javito · Administrar artículos</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Administrar artículos</h1>
		<p class="text-sm text-muted-foreground">
			Todos los artículos, activos e inactivos. Los inactivos no aparecen en el catálogo.
		</p>
	</header>

	{#if mensajeError}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{mensajeError}</AlertDescription>
			<AlertAction>
				<Button type="button" variant="outline" size="xs" onclick={reintentar}>Reintentar</Button>
			</AlertAction>
		</Alert>
	{/if}

	{#if reactivacion.error}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{reactivacion.error}</AlertDescription>
		</Alert>
	{/if}

	{#if listado.estado.cargando && listado.estado.pagina === null}
		<div class="flex items-center gap-2 py-10 text-sm text-muted-foreground">
			<Spinner aria-label="Cargando artículos" />
			<span>Cargando artículos…</span>
		</div>
	{:else if listado.estado.pagina}
		{@const actual = listado.estado.pagina}
		<div class="flex flex-col gap-4" aria-busy={listado.estado.cargando}>
			{#if listado.paginaInexistente}
				<Empty class="rounded-xl border border-border bg-card">
					<EmptyHeader>
						<EmptyTitle>Esta página no existe</EmptyTitle>
						<EmptyDescription>
							El listado no llega a la página {actual.pagina}: hay {actual.totalDePaginas}
							{actual.totalDePaginas === 1 ? 'página' : 'páginas'}.
						</EmptyDescription>
					</EmptyHeader>
					<Button
						type="button"
						variant="outline"
						onclick={() => pantalla.navegar(listado.ultimaPagina)}
					>
						Ir a la última página
					</Button>
				</Empty>
			{:else if !listado.hayElementos}
				<Empty class="rounded-xl border border-border bg-card">
					<EmptyHeader>
						<EmptyTitle>Todavía no hay artículos</EmptyTitle>
						<EmptyDescription>Cuando se publique el primero va a aparecer acá.</EmptyDescription>
					</EmptyHeader>
				</Empty>
			{:else}
				<TablaDeAdministracionDeArticulos
					{actual}
					categoriaDe={pantalla.nombreDe}
					reactivando={reactivacion.enCurso}
					alReactivar={(id) => void pantalla.reactivar(id)}
					alCambiarDePagina={pantalla.navegar}
				/>
			{/if}
		</div>
	{/if}
</main>
