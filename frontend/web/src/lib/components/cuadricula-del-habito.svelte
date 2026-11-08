<script lang="ts">
	import type { DiaDelHistorial } from '$lib/api/historial';
	import { cuadriculaDeDias, ESTADOS_DE_LA_CUADRICULA } from '$lib/cuadricula';

	// La cuadrícula del detalle (HU-HIS-02, S6/S8): los días del período, siete por fila. No sabe
	// pedir ni elegir el período: recibe los días ya decididos por el servidor y los pinta.
	let { dias }: { dias: DiaDelHistorial[] } = $props();

	const celdas = $derived(cuadriculaDeDias(dias));
</script>

<!--
	Los días van como grupo con etiqueta y una imagen por celda, no como lista: es el mismo molde que
	la semana de la tarjeta (`semana-del-habito.svelte`), para no sumar un `listitem` por día.
-->
<div class="flex flex-col gap-3">
	<div class="inline-grid grid-cols-7 gap-1" role="group" aria-label="Los días del período">
		<!--
			Cumplido se distingue por la tilde y el relleno; vacío y neutro, por su marca. El color
			acompaña, nunca decide: la etiqueta y la leyenda dicen la palabra (S8). Nada en rojo.
		-->
		{#each celdas as celda (celda.clave)}
			<span
				class="flex size-6 items-center justify-center rounded-md border text-xs font-medium {celda.estado ===
				'cumplido'
					? 'border-primary bg-primary text-primary-foreground'
					: 'border-border text-muted-foreground'}"
				role="img"
				aria-label={celda.etiqueta}
			>
				{celda.marca}
			</span>
		{/each}
	</div>

	<ul class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
		{#each ESTADOS_DE_LA_CUADRICULA as estado (estado.estado)}
			<li class="flex items-center gap-1.5">
				<span
					class="flex size-4 items-center justify-center rounded border text-[0.65rem] {estado.estado ===
					'cumplido'
						? 'border-primary bg-primary text-primary-foreground'
						: 'border-border'}"
					aria-hidden="true"
				>
					{estado.marca}
				</span>
				{estado.palabra}
			</li>
		{/each}
	</ul>
</div>
