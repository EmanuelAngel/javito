<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import HistorialVacio from '$lib/components/historial-vacio.svelte';
	import Paginador from '$lib/components/paginador.svelte';
	import TablaDeHistorial from '$lib/components/tabla-de-historial.svelte';
	import { Alert, AlertAction, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';
	import { Spinner } from '$lib/components/ui/spinner/index.js';
	import { estadoDelHistorial } from '$lib/historial.svelte';
	import { leerEnteroPositivo, leerPagina } from '$lib/paginado.svelte';

	// El estado vive en la URL —`?habitoId=`, `?desde=`, `?hasta=` y `?pagina=`—: es la única fuente y
	// todo cambio pasa por `navegar`, así un enlace copiado abre el mismo período (HU-HIS-01). Pedidos,
	// hábitos del filtro y navegación están en `historial.svelte.ts`.
	const pagina = $derived(leerPagina(page.url.searchParams.get('pagina')));
	const habitoId = $derived(leerEnteroPositivo(page.url.searchParams.get('habitoId')));
	const desde = $derived(page.url.searchParams.get('desde'));
	const hasta = $derived(page.url.searchParams.get('hasta'));

	const pantalla = estadoDelHistorial(resolve('/(app)/historial'));
	const { historial, habitos } = pantalla;

	const cargando = $derived(habitos.cargando || historial.estado.cargando);
	const mensajeError = $derived(habitos.error || historial.estado.mensajeError);
	const hayFiltros = $derived(habitoId !== null || desde !== null || hasta !== null);

	onMount(() => void pantalla.cargarHabitos());

	// Sigue los filtros y la página, y nada más: `cargar` sólo escribe estado y no se vuelve a disparar.
	$effect(() => void historial.cargar(pagina, { habitoId, desde, hasta }));

	function reintentar() {
		void pantalla.cargarHabitos();
		void historial.cargar(pagina, { habitoId, desde, hasta });
	}

	const irAPagina = (destino: number) =>
		pantalla.navegar({ habitoId, desde, hasta, pagina: destino });

	// Cambiar un filtro vuelve a la página 1: la 3 de todo el historial no es la 3 de un hábito.
	function elegirHabito(evento: Event) {
		const valor = (evento.currentTarget as HTMLSelectElement).value;

		pantalla.navegar({ habitoId: valor === '' ? null : Number(valor), desde, hasta, pagina: 1 });
	}

	function elegirFecha(campo: 'desde' | 'hasta', evento: Event) {
		const valor = (evento.currentTarget as HTMLInputElement).value || null;

		pantalla.navegar({
			habitoId,
			desde: campo === 'desde' ? valor : desde,
			hasta: campo === 'hasta' ? valor : hasta,
			pagina: 1
		});
	}

	const quitarFiltros = () =>
		pantalla.navegar({ habitoId: null, desde: null, hasta: null, pagina: 1 });
</script>

<svelte:head>
	<title>Javito · Historial</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Historial</h1>
		{#if historial.estado.pagina && !cargando}
			{@const total = historial.estado.pagina.totalDeRegistros}
			<p class="text-sm text-muted-foreground">
				{total}
				{total === 1 ? 'registro' : 'registros'}
				{hayFiltros ? 'con estos filtros' : 'en total'}
			</p>
		{/if}
	</header>

	<div class="flex flex-col gap-3 sm:flex-row sm:items-end">
		<div class="flex flex-col gap-1.5 sm:max-w-xs sm:flex-1">
			<label class="text-sm font-medium" for="historial-habito">Hábito</label>
			<NativeSelect
				id="historial-habito"
				class="w-full"
				value={habitoId === null ? '' : String(habitoId)}
				onchange={elegirHabito}
			>
				<NativeSelectOption value="">Todos los hábitos</NativeSelectOption>
				{#each habitos.lista as habito (habito.id)}
					<NativeSelectOption value={habito.id}>
						{habito.titulo}{habito.activo ? '' : ' (archivado)'}
					</NativeSelectOption>
				{/each}
			</NativeSelect>
		</div>

		<div class="flex flex-col gap-1.5">
			<label class="text-sm font-medium" for="historial-desde">Desde</label>
			<Input
				id="historial-desde"
				type="date"
				value={desde ?? ''}
				onchange={(evento) => elegirFecha('desde', evento)}
			/>
		</div>

		<div class="flex flex-col gap-1.5">
			<label class="text-sm font-medium" for="historial-hasta">Hasta</label>
			<Input
				id="historial-hasta"
				type="date"
				value={hasta ?? ''}
				onchange={(evento) => elegirFecha('hasta', evento)}
			/>
		</div>

		{#if hayFiltros}
			<Button type="button" variant="ghost" onclick={quitarFiltros}>Quitar filtros</Button>
		{/if}
	</div>

	{#if mensajeError}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{mensajeError}</AlertDescription>
			<AlertAction>
				<Button type="button" variant="outline" size="xs" onclick={reintentar}>Reintentar</Button>
			</AlertAction>
		</Alert>
	{/if}

	{#if cargando && historial.estado.pagina === null}
		<!-- Primera carga: nunca una pantalla en blanco mientras se pide. -->
		<div class="flex items-center gap-2 py-10 text-sm text-muted-foreground">
			<Spinner aria-label="Cargando el historial" />
			<span>Cargando el historial…</span>
		</div>
	{:else if historial.estado.pagina}
		{@const actual = historial.estado.pagina}
		<div class="flex flex-col gap-4" aria-busy={cargando}>
			{#if !historial.hayElementos}
				<HistorialVacio
					paginaInexistente={historial.paginaInexistente}
					filtrado={hayFiltros}
					pagina={actual.pagina}
					totalDePaginas={actual.totalDePaginas}
					alIrALaUltima={() => irAPagina(historial.ultimaPagina)}
					alLimpiarFiltros={quitarFiltros}
				/>
			{:else}
				<TablaDeHistorial filas={actual.elementos} />
				{#if actual.totalDePaginas > 1}
					<Paginador
						pagina={actual.pagina}
						totalDePaginas={actual.totalDePaginas}
						alCambiar={irAPagina}
						etiqueta="Paginación del historial"
					/>
				{/if}
			{/if}
		</div>
	{/if}
</main>
