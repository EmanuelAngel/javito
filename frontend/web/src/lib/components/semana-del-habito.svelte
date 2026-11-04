<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import type { Semana } from '$lib/api/dias';
	import { estadosDeLaSemana } from '$lib/semana';

	let { semana, hoy }: { semana: Semana; hoy: string } = $props();

	const estados = $derived(estadosDeLaSemana(semana.dias, hoy));
</script>

<!--
	Los siete días van como grupo con etiqueta, no como lista anidada: la tarjeta ya es un `li` del
	listado del día y un `ul` adentro sumaría siete `listitem` más a esa lista.
-->
<div class="flex items-center gap-1.5" role="group" aria-label="La semana de este hábito">
	{#each estados as estado (estado.fecha)}
		<!--
			Cumplido se distingue por la tilde y el relleno, no sólo por el color; hoy lleva el anillo
			que lo separa de los días anteriores. Sin marca queda en neutro: nunca en rojo.
		-->
		<span
			class="flex size-6 items-center justify-center rounded-full border text-[0.65rem] font-medium {estado.cumplido
				? 'border-primary bg-primary text-primary-foreground'
				: 'border-border text-muted-foreground'} {estado.esHoy
				? 'ring-2 ring-ring ring-offset-2 ring-offset-card'
				: ''}"
			role="img"
			aria-label={estado.etiqueta}
		>
			{#if estado.cumplido}
				<CheckIcon class="size-3.5" aria-hidden="true" />
			{:else}
				{estado.inicial}
			{/if}
		</span>
	{/each}
</div>
