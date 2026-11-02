<script lang="ts">
	import FilaDelDia from '$lib/components/FilaDelDia.svelte';
	import { diaLargo } from '$lib/fechas';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();
</script>

<svelte:head>
	<title>Javito · Hoy</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-3xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Hoy</h1>
		<p class="text-sm text-muted-foreground">{diaLargo(data.dia.fecha)}</p>
	</header>

	{#if data.dia.habitos.length === 0}
		<p class="rounded-xl border border-border bg-card px-5 py-6 text-sm text-muted-foreground">
			Todavía no tenés hábitos activos.
		</p>
	{:else}
		<ul class="flex flex-col gap-3">
			{#each data.dia.habitos as habito (habito.id)}
				<FilaDelDia {habito} />
			{/each}
		</ul>
	{/if}
</main>
