<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import MinusIcon from '@lucide/svelte/icons/minus';
	import type { FilaDelHistorial } from '$lib/api/historial';
	import { filasDelHistorial } from '$lib/historial';

	// La tabla del historial (HU-HIS-01). No sabe pedir ni paginar: recibe las filas ya ordenadas por
	// el servidor y las pinta. El estado va con ícono y palabra, no sólo con color, igual que la
	// cuadrícula de la semana (HU-REG-04): quien no distingue colores tiene que poder leerlo.
	let { filas }: { filas: FilaDelHistorial[] } = $props();

	const mostradas = $derived(filasDelHistorial(filas));
</script>

<div class="overflow-x-auto rounded-xl border border-border bg-card">
	<table class="w-full border-collapse text-sm">
		<caption class="sr-only">
			Tus registros, del día más nuevo al más viejo: fecha, hábito, valor alcanzado, meta de ese día
			y si se completó.
		</caption>
		<thead>
			<tr class="border-b border-border text-left text-muted-foreground">
				<th scope="col" class="px-3 py-2 font-medium">Fecha</th>
				<th scope="col" class="px-3 py-2 font-medium">Hábito</th>
				<th scope="col" class="px-3 py-2 text-right font-medium">Valor</th>
				<th scope="col" class="px-3 py-2 text-right font-medium">Meta</th>
				<th scope="col" class="px-3 py-2 font-medium">Estado</th>
			</tr>
		</thead>
		<tbody>
			{#each mostradas as fila (fila.clave)}
				<tr class="border-b border-border/60 last:border-0">
					<td class="px-3 py-2 whitespace-nowrap">{fila.fecha}</td>
					<td class="px-3 py-2">{fila.habito}</td>
					<td class="px-3 py-2 text-right tabular-nums">{fila.valor}</td>
					<td class="px-3 py-2 text-right text-muted-foreground tabular-nums">{fila.meta}</td>
					<td class="px-3 py-2 whitespace-nowrap">
						<span
							class="inline-flex items-center gap-1.5 {fila.cumplido
								? 'text-primary'
								: 'text-muted-foreground'}"
						>
							{#if fila.cumplido}
								<CheckIcon class="size-4" aria-hidden="true" />
							{:else}
								<MinusIcon class="size-4" aria-hidden="true" />
							{/if}
							{fila.estado}
						</span>
					</td>
				</tr>
			{/each}
		</tbody>
	</table>
</div>
