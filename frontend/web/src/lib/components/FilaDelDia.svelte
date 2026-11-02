<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import MinusIcon from '@lucide/svelte/icons/minus';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import type { HabitoDelDia } from '$lib/api/dias';

	let {
		habito,
		alternar,
		subir,
		bajar
	}: {
		habito: HabitoDelDia;
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
		<span class="text-sm text-muted-foreground">
			{habito.categoria ?? 'Sin categoría'} · {progreso}
		</span>
	</div>

	{#if esContador}
		<div class="flex shrink-0 items-center gap-2">
			<button
				type="button"
				aria-label="Restar {habito.unidad ?? 'uno'} a {habito.titulo}"
				disabled={valor <= 0}
				onclick={() => bajar(habito)}
				class="grid size-9 place-items-center rounded-lg border border-border bg-background transition-colors hover:bg-accent disabled:opacity-40"
			>
				<MinusIcon class="size-4" />
			</button>

			<span
				class="min-w-20 text-center text-sm tabular-nums {hecho
					? 'text-primary'
					: 'text-muted-foreground'}"
			>
				{valor} / {meta}
				{#if habito.unidad}<span class="text-xs"> {habito.unidad}</span>{/if}
			</span>

			<button
				type="button"
				aria-label="Sumar {habito.unidad ?? 'uno'} a {habito.titulo}"
				onclick={() => subir(habito)}
				class="grid size-9 place-items-center rounded-lg border border-border bg-background transition-colors hover:bg-accent"
			>
				<PlusIcon class="size-4" />
			</button>
		</div>
	{:else}
		<button
			type="button"
			aria-pressed={hecho}
			onclick={() => alternar(habito)}
			class="flex shrink-0 items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-medium transition-colors {hecho
				? 'border-transparent bg-primary text-primary-foreground hover:bg-primary/90'
				: 'bg-background hover:bg-accent'}"
		>
			<CheckIcon class="size-4" />
			{etiqueta}
		</button>
	{/if}
</li>
