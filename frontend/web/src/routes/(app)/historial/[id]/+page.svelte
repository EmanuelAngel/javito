<script lang="ts">
	import ArrowLeftIcon from '@lucide/svelte/icons/arrow-left';
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import CuadriculaDelHabito from '$lib/components/cuadricula-del-habito.svelte';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Spinner } from '$lib/components/ui/spinner/index.js';
	import { totalesDelDetalle } from '$lib/cuadricula';
	import { estadoDelDetalle } from '$lib/historial-del-habito.svelte';
	import { leerEnteroPositivo } from '$lib/paginado.svelte';
	import type { PageProps } from './$types';

	// El detalle histórico de un hábito (HU-HIS-02). El período vive en la URL —`?dias=`—, como el
	// resto del módulo: un enlace copiado abre el mismo período. El pedido y su estado están en
	// `historial-del-habito.svelte.ts`; acá sólo se compone la pantalla.
	let { params }: PageProps = $props();

	/** Los únicos períodos del detalle: el mismo trío que valida la WebAPI (L8). */
	const PERIODOS: number[] = [30, 60, 365];

	/** Sin un `?dias=` de la lista, el mes: una URL escrita a mano no rompe la pantalla. */
	function leerPeriodo(valor: string | null): number {
		const numero = Number(valor);

		return PERIODOS.includes(numero) ? numero : 30;
	}

	const dias = $derived(leerPeriodo(page.url.searchParams.get('dias')));
	// Un id ilegible pide el 0: la API contesta el mismo 404 que un hábito ajeno y no se inventa otro.
	const habitoId = $derived(leerEnteroPositivo(params.id) ?? 0);
	const pantalla = estadoDelDetalle();
	const detalle = $derived(pantalla.estado.detalle);
	const totales = $derived(detalle === null ? null : totalesDelDetalle(detalle));

	// La query la arma la pantalla; `resolve()` sólo tipa la ruta base.
	const editar = $derived(`${resolve('/(app)/habitos')}?editar=${habitoId}`);

	// Sigue el hábito y el período, y nada más: `cargar` sólo escribe estado y no se vuelve a disparar.
	$effect(() => void pantalla.cargar(habitoId, dias));

	/** El período es una lectura de la ventana: reemplaza la entrada y no mueve scroll ni foco. */
	function elegirPeriodo(elegido: number): void {
		// `resolve()` no puede tipar esta URL con query: es la ruta del detalle más el período.
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		void goto(`${resolve('/(app)/historial/[id]', { id: params.id })}?dias=${elegido}`, {
			replace: true,
			reset: false
		});
	}

	// aaaa-mm-dd → dd/mm/aaaa cortando la cadena, igual que la tarjeta: es un día, no un instante.
	const diaCorto = (iso: string) => iso.split('-').reverse().join('/');
</script>

<svelte:head>
	<title>Javito · {detalle?.habito ?? 'Historial'}</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-3xl flex-col gap-6 px-5 py-10">
	<div class="flex flex-wrap items-center justify-between gap-2">
		<Button variant="ghost" size="sm" href={resolve('/(app)/historial')}>
			<ArrowLeftIcon class="size-4" />
			Volver al historial
		</Button>

		<!-- La ruta se armó arriba con la query del detalle; resolve() sólo tipa la base. -->
		<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
		<Button size="sm" href={editar}>
			<PencilIcon class="size-4" />
			Editar hábito
		</Button>
	</div>

	<div class="flex flex-col gap-2">
		<span class="text-sm font-medium">Período</span>
		<div class="flex flex-wrap gap-2">
			{#each PERIODOS as periodo (periodo)}
				<Button
					type="button"
					size="sm"
					variant={periodo === dias ? 'default' : 'outline'}
					aria-pressed={periodo === dias}
					onclick={() => elegirPeriodo(periodo)}
				>
					{periodo} días
				</Button>
			{/each}
		</div>
	</div>

	{#if pantalla.estado.mensajeError}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{pantalla.estado.mensajeError}</AlertDescription>
		</Alert>
	{/if}

	{#if pantalla.estado.cargando && detalle === null}
		<!-- Primera carga: nunca una pantalla en blanco mientras se pide. -->
		<div class="flex items-center gap-2 py-10 text-sm text-muted-foreground">
			<Spinner aria-label="Cargando el hábito" />
			<span>Cargando el hábito…</span>
		</div>
	{:else if detalle && totales}
		<article
			class="flex flex-col gap-5 rounded-xl border border-border bg-card p-6"
			aria-busy={pantalla.estado.cargando}
		>
			<header class="flex flex-col gap-1">
				<h1 class="text-3xl font-semibold tracking-tight">{detalle.habito}</h1>
				<p class="text-sm text-muted-foreground">
					Del {diaCorto(detalle.desde)} al {diaCorto(detalle.hasta)}: {detalle.dias} días.
				</p>
			</header>

			<div class="flex flex-wrap items-baseline gap-x-3 gap-y-1">
				{#if totales.porcentaje}
					<p class="text-4xl font-semibold tracking-tight tabular-nums">{totales.porcentaje}</p>
				{/if}
				<p class="text-sm">{totales.total}</p>
			</div>

			<CuadriculaDelHabito dias={detalle.cuadricula} />
		</article>
	{/if}
</main>
