<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import {
		estadoDeLaAdministracion,
		leerEstado,
		type FiltrosDeLaPantalla
	} from '$lib/administracion-de-usuarios.svelte';
	import FiltrosDeUsuarios from '$lib/components/filtros-de-usuarios.svelte';
	import TablaDeUsuarios from '$lib/components/tabla-de-usuarios.svelte';
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

	// HU-USU-01: el listado de cuentas del administrador. La página y los filtros viven en la URL, así
	// que volver conserva dónde se estaba; el pedido y el estado están en el módulo.
	const pagina = $derived(leerPagina(page.url.searchParams.get('pagina')));
	const filtros = $derived<FiltrosDeLaPantalla>({
		q: page.url.searchParams.get('q') ?? '',
		activo: page.url.searchParams.get('activo')
	});
	const estado = $derived(leerEstado(filtros.activo));

	const pantalla = estadoDeLaAdministracion(resolve('/(app)/admin/usuarios'));
	const { listado } = pantalla;

	$effect(() => void listado.cargar(pagina, filtros));
</script>

<svelte:head>
	<title>Javito · Usuarios</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Usuarios</h1>
		<p class="text-sm text-muted-foreground">
			Todas las cuentas, activas y desactivadas, con la más nueva primero.
		</p>
	</header>

	<FiltrosDeUsuarios
		q={filtros.q}
		{estado}
		alBuscar={(texto) => pantalla.navegar(1, { q: texto, activo: filtros.activo })}
		alFiltrar={(activo) => pantalla.navegar(1, { q: filtros.q, activo })}
	/>

	{#if listado.estado.mensajeError}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{listado.estado.mensajeError}</AlertDescription>
			<AlertAction>
				<Button
					type="button"
					variant="outline"
					size="xs"
					onclick={() => void listado.cargar(pagina, filtros)}>Reintentar</Button
				>
			</AlertAction>
		</Alert>
	{/if}

	{#if listado.estado.cargando && listado.estado.pagina === null}
		<div class="flex items-center gap-2 py-10 text-sm text-muted-foreground">
			<Spinner aria-label="Cargando usuarios" />
			<span>Cargando usuarios…</span>
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
						onclick={() => pantalla.navegar(listado.ultimaPagina, filtros)}
					>
						Ir a la última página
					</Button>
				</Empty>
			{:else if !listado.hayElementos}
				<Empty class="rounded-xl border border-border bg-card">
					<EmptyHeader>
						<EmptyTitle>No hay cuentas que coincidan</EmptyTitle>
						<EmptyDescription>Probá con otro nombre, otro email u otro estado.</EmptyDescription>
					</EmptyHeader>
				</Empty>
			{:else}
				<TablaDeUsuarios
					{actual}
					alCambiarDePagina={(destino) => pantalla.navegar(destino, filtros)}
				/>
			{/if}
		</div>
	{/if}
</main>
