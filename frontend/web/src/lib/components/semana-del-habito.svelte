<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import type { Semana } from '$lib/api/dias';
	import { estadosDeLaSemana } from '$lib/semana';

	let { semana, hoy }: { semana: Semana; hoy: string } = $props();

	const estados = $derived(estadosDeLaSemana(semana.dias, hoy));

	// Hoy se marca con una superficie suave, no con un anillo: el anillo peleaba con el borde de cada
	// círculo y a doce filas se leía como una alerta. Un día cumplido ya es el fuego más fuerte que
	// hay, así que conserva su relleno y no suma marca.
	function clases(estado: { cumplido: boolean; esHoy: boolean }): string {
		if (estado.cumplido) {
			return 'border-primary bg-primary text-primary-foreground';
		}

		return estado.esHoy
			? 'border-transparent bg-primary/15 text-foreground'
			: 'border-border text-muted-foreground';
	}
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
			class="flex size-6 items-center justify-center rounded-full border text-[0.65rem] font-medium {clases(
				estado
			)}"
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
