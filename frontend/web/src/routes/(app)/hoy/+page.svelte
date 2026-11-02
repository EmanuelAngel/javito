<script lang="ts">
	import { resolve } from '$app/paths';
	import FilaDelDia from '$lib/components/FilaDelDia.svelte';
	import { diaLargo } from '$lib/fechas';
	import { estadoDelDia } from '$lib/hoy.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// La pantalla arranca con lo que trajo el load y desde acá el estado lo maneja el módulo del día,
	// que pinta el toque al instante y revierte si el servidor dice que no.
	// svelte-ignore state_referenced_locally
	const estado = estadoDelDia(data.dia);
</script>

<svelte:head>
	<title>Javito · Hoy</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-3xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Hoy</h1>
		<p class="text-sm text-muted-foreground">{diaLargo(estado.dia.fecha)}</p>
	</header>

	{#if estado.aviso}
		<p
			role="alert"
			class="flex items-center justify-between gap-4 rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive"
		>
			{estado.aviso}
			<button
				type="button"
				onclick={() => estado.descartarAviso()}
				class="rounded-lg border border-destructive/30 px-3 py-1 text-xs transition-colors hover:bg-destructive/10"
			>
				Cerrar
			</button>
		</p>
	{/if}

	{#if estado.dia.habitos.length === 0}
		<p class="rounded-xl border border-border bg-card px-5 py-6 text-sm text-muted-foreground">
			Todavía no tenés hábitos activos.
			<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
			<a
				href={resolve('/(app)/habitos')}
				class="font-medium text-primary underline-offset-4 hover:underline"
			>
				Crear uno en Hábitos
			</a>
		</p>
	{:else}
		<ul class="flex flex-col gap-3">
			{#each estado.dia.habitos as habito (habito.id)}
				<FilaDelDia {habito} alternar={estado.alternar} subir={estado.subir} bajar={estado.bajar} />
			{/each}
		</ul>
	{/if}
</main>
