<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import MinusIcon from '@lucide/svelte/icons/minus';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import type { HabitoDelDia } from '$lib/api/dias';
	import ChipDeCategoria from '$lib/components/chip-de-categoria.svelte';
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
			? `${habito.semana.cumplidos} / ${habito.semana.objetivo} esta semana`
			: 'Todos los días'
	);

	// La cuota es un piso, no un tope: pasarse (4 / 3) sigue siendo cuota cumplida y no se recorta.
	const cuotaCumplida = $derived(habito.semana.cumplidos >= habito.semana.objetivo);

	const etiqueta = $derived(habito.tipo === 'abstinencia' ? 'Hoy me mantuve' : 'Hecho');
</script>

<!--
	En angosto la tarjeta se apila: el texto arriba a todo el ancho y la acción abajo, también a todo
	el ancho. Con la fila compartida, el bloque de acción —que no encoge— le robaba el ancho al título
	(lo truncaba) y a la semana (la aplastaba). Desde `sm` vuelve a ser una sola fila.
-->
<li
	class="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-5 sm:flex-row sm:items-center sm:justify-between sm:gap-5 sm:px-6"
>
	<div class="flex min-w-0 flex-col gap-3">
		<!-- Sin `truncate`: los títulos del seed son largos y a 390px tienen que envolver, no cortarse. -->
		<span class="leading-snug font-medium text-pretty">{habito.titulo}</span>
		<div class="flex flex-wrap items-center gap-x-2 gap-y-2 text-sm text-muted-foreground">
			<ChipDeCategoria nombre={habito.categoria} />
			{#if habito.frecuencia.tipo === 'semanal'}
				<!-- Cumplida se distingue por el relleno y por la tilde, no sólo por el color (HU #75). -->
				{#if cuotaCumplida}
					<!-- Mismo patrón que la cuadrícula semanal: un `span` genérico no expone su etiqueta a
						 todos los lectores de pantalla; con `role="img"` sí, y la tilde es la señal visual. -->
					<Badge role="img" aria-label="{progreso}, cuota cumplida">
						<CheckIcon aria-hidden="true" />
						{progreso}
					</Badge>
				{:else}
					<Badge variant="secondary">{progreso}</Badge>
				{/if}
			{:else}
				<span>{progreso}</span>
			{/if}
		</div>

		<!-- La semana de la tarjeta: siete días con su marca, de lunes a domingo (HU-REG-04). -->
		<SemanaDelHabito semana={habito.semana} hoy={fecha} />
	</div>

	{#if esContador}
		<div class="flex items-center justify-between gap-2 sm:shrink-0 sm:justify-start">
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
			variant={hecho ? 'secondary' : 'outline'}
			aria-pressed={hecho}
			class={[
				'w-full sm:w-auto',
				// Completado no es una acción a repetir: tinte suave en vez del relleno primario, que a
				// doce filas llenas se come la pantalla.
				hecho ? 'bg-primary/15 text-foreground hover:bg-primary/20' : ''
			]}
			onclick={() => alternar(habito)}
		>
			<CheckIcon />
			{etiqueta}
		</Button>
	{/if}
</li>
