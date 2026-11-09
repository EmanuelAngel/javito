<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Alert, AlertAction, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';
	import FilaDelDia from '$lib/components/fila-del-dia.svelte';
	import FiltroDeRutina from '$lib/components/filtro-de-rutina.svelte';
	import { diaLargo } from '$lib/fechas';
	import { habitosDeLaRutina } from '$lib/filtro-por-rutina';
	import { estadoDelDia } from '$lib/hoy.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// La pantalla arranca con lo que trajo el load y desde acá el estado lo maneja el módulo del día,
	// que pinta el toque al instante y revierte si el servidor dice que no.
	// svelte-ignore state_referenced_locally
	const estado = estadoDelDia(data.dia);

	// El filtro por rutina (HU-RUT-04) proyecta el mismo estado del día: marcar dentro del filtro
	// marca el hábito real. Cambiar de rutina sólo cambia `?rutinaId=` y el load trae su orden.
	const visibles = $derived(
		data.rutina === null ? estado.dia.habitos : habitosDeLaRutina(estado.dia.habitos, data.rutina)
	);

	function elegirRutina(rutinaId: number | null) {
		const consulta = rutinaId === null ? '' : `?rutinaId=${rutinaId}`;

		// eslint-disable-next-line svelte/no-navigation-without-resolve -- la ruta sí está resuelta; se le suma la consulta
		void goto(`${resolve('/(app)/hoy')}${consulta}`, { replace: true, reset: false });
	}
</script>

<svelte:head>
	<title>Javito · Hoy</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-3xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Hoy</h1>
		<p class="text-sm text-muted-foreground">{diaLargo(estado.dia.fecha)}</p>
	</header>

	{#if data.rutinas.length > 0}
		<FiltroDeRutina
			rutinas={data.rutinas}
			rutinaId={data.rutina?.id ?? null}
			alElegir={elegirRutina}
		/>
	{/if}

	{#if estado.aviso}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{estado.aviso}</AlertDescription>
			<AlertAction>
				<Button type="button" variant="outline" size="xs" onclick={() => estado.descartarAviso()}>
					Cerrar
				</Button>
			</AlertAction>
		</Alert>
	{/if}

	{#if estado.dia.habitos.length === 0}
		<Empty class="rounded-xl border border-border bg-card">
			<EmptyHeader>
				<EmptyTitle>Sin hábitos activos</EmptyTitle>
				<EmptyDescription>
					Todavía no tenés hábitos activos.
					<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
					<a
						href={resolve('/(app)/habitos')}
						class="font-medium text-primary underline-offset-4 hover:underline"
					>
						Crear uno en Hábitos
					</a>
				</EmptyDescription>
			</EmptyHeader>
		</Empty>
	{:else if visibles.length === 0}
		<Empty class="rounded-xl border border-border bg-card">
			<EmptyHeader>
				<EmptyTitle>Esta rutina no tiene hábitos activos</EmptyTitle>
				<EmptyDescription>
					<Button type="button" variant="link" onclick={() => elegirRutina(null)}>
						Ver todos los hábitos
					</Button>
				</EmptyDescription>
			</EmptyHeader>
		</Empty>
	{:else}
		<ul class="flex flex-col gap-3">
			{#each visibles as habito (habito.id)}
				<FilaDelDia
					{habito}
					fecha={estado.dia.fecha}
					alternar={estado.alternar}
					subir={estado.subir}
					bajar={estado.bajar}
				/>
			{/each}
		</ul>
	{/if}
</main>
