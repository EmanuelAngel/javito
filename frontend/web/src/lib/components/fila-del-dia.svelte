<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import MinusIcon from '@lucide/svelte/icons/minus';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import type { HabitoDelDia } from '$lib/api/dias';
	import SemanaDelHabito from '$lib/components/semana-del-habito.svelte';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';

	let {
		habito,
		fecha,
		alternar,
		subir,
		bajar
	}: {
		habito: HabitoDelDia;
		fecha: string;
		alternar: (habito: HabitoDelDia) => void;
		subir: (habito: HabitoDelDia) => void;
		bajar: (habito: HabitoDelDia) => void;
	} = $props();

	// Un contador se maneja con + y −; un sí/no y una abstinencia, con un solo toque.
	const esContador = $derived(habito.tipo === 'contador');
	const hecho = $derived(habito.registro?.completado ?? false);
	const valor = $derived(habito.registro?.valor ?? 0);
	const meta = $derived(habito.meta ?? 1);

	const progreso = $derived(
		habito.frecuencia.tipo === 'semanal'
			? `${habito.semana.cumplidos} de ${habito.semana.objetivo} esta semana`
			: 'Todos los días'
	);

	const etiqueta = $derived(habito.tipo === 'abstinencia' ? 'Hoy me mantuve' : 'Hecho');
</script>

<li
	class="flex items-center justify-between gap-4 rounded-xl border border-border bg-card px-5 py-4"
>
	<div class="flex min-w-0 flex-col gap-1">
		<span class="truncate font-medium">{habito.titulo}</span>
		<div class="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
			<span>{habito.categoria ?? 'Sin categoría'}</span>
			{#if habito.frecuencia.tipo === 'semanal'}
				<Badge variant="secondary">{progreso}</Badge>
			{:else}
				<span>{progreso}</span>
			{/if}
		</div>

		<!-- La semana de la tarjeta: siete días con su marca, de lunes a domingo (HU-REG-04). -->
		<SemanaDelHabito semana={habito.semana} hoy={fecha} />
	</div>

	{#if esContador}
		<div class="flex shrink-0 items-center gap-2">
			<Button
				type="button"
				variant="outline"
				size="icon-lg"
				aria-label="Restar {habito.unidad ?? 'uno'} a {habito.titulo}"
				disabled={valor <= 0}
				onclick={() => bajar(habito)}
			>
				<MinusIcon />
			</Button>

			<span
				class="min-w-20 text-center text-sm tabular-nums {hecho
					? 'text-primary'
					: 'text-muted-foreground'}"
			>
				{valor} / {meta}
				{#if habito.unidad}<span class="text-xs"> {habito.unidad}</span>{/if}
			</span>

			<Button
				type="button"
				variant="outline"
				size="icon-lg"
				aria-label="Sumar {habito.unidad ?? 'uno'} a {habito.titulo}"
				onclick={() => subir(habito)}
			>
				<PlusIcon />
			</Button>
		</div>
	{:else}
		<Button
			type="button"
			variant={hecho ? 'default' : 'outline'}
			aria-pressed={hecho}
			onclick={() => alternar(habito)}
		>
			<CheckIcon />
			{etiqueta}
		</Button>
	{/if}
</li>
