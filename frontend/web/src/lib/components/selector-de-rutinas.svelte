<script lang="ts">
	import type { Rutina } from '$lib/api/rutinas';

	// El selector de rutina, uno solo para las dos pantallas que lo usan (hallazgos #2 y #14): /hoy
	// filtra el día con él y /rutinas elige qué rutina mira. Antes eran dos controles distintos y el de
	// /rutinas usaba un `+` rotado que se leía como una ×. Sólo lee el control: cada pantalla es la
	// dueña de qué hace con la elección.
	type Props = {
		rutinas: Rutina[];
		seleccionada: number | null;
		alElegir: (rutinaId: number | null) => void;
		// En /hoy «Todos» es una opción más; en /rutinas hay que elegir una para ver sus hábitos.
		conTodos?: boolean;
		etiqueta?: string;
		class?: string;
	};

	let {
		rutinas,
		seleccionada,
		alElegir,
		conTodos = false,
		etiqueta = 'Rutina',
		class: className
	}: Props = $props();

	// El punto de color es un detalle, no un dato: cada rutina toma un acento por su id, así archivar
	// una no le corre el color a las demás. La activa se rellena con ese mismo acento.
	//
	// El texto va en el acento mezclado con el `foreground` del tema: el acento puro no llega a AA sobre
	// su propio tinte en Dawn (gold 2.16:1, iris 3.65:1), y cada uno tiene su punto de mezcla máximo.
	const TINTES = [
		{
			punto: 'bg-accent-love',
			activo:
				'border-transparent bg-accent-love/15 text-[color-mix(in_oklab,var(--accent-love)_45%,var(--foreground))] ring-1 ring-accent-love/30'
		},
		{
			punto: 'bg-accent-iris',
			activo:
				'border-transparent bg-accent-iris/15 text-[color-mix(in_oklab,var(--accent-iris)_40%,var(--foreground))] ring-1 ring-accent-iris/30'
		},
		{
			punto: 'bg-accent-foam',
			activo:
				'border-transparent bg-accent-foam/15 text-[color-mix(in_oklab,var(--accent-foam)_35%,var(--foreground))] ring-1 ring-accent-foam/30'
		},
		{
			punto: 'bg-accent-gold',
			activo:
				'border-transparent bg-accent-gold/15 text-[color-mix(in_oklab,var(--accent-gold)_25%,var(--foreground))] ring-1 ring-accent-gold/30'
		}
	];

	const APAGADA = 'border-border bg-background text-muted-foreground hover:text-foreground';
	const ENCENDIDA_TODOS = 'border-transparent bg-primary/15 text-foreground ring-1 ring-primary/30';
	const BASE =
		'inline-flex shrink-0 items-center gap-2 rounded-full border px-3.5 py-2 text-sm font-medium transition active:scale-95';

	const activas = $derived(rutinas.filter((rutina) => rutina.activo));
	const inactivas = $derived(rutinas.filter((rutina) => !rutina.activo));

	function tinteDe(id: number) {
		return TINTES[id % TINTES.length];
	}
</script>

<div data-slot="selector-de-rutinas" class="flex flex-col gap-2 {className ?? ''}">
	<div class="flex flex-col gap-2">
		<span class="text-xs font-medium text-muted-foreground">{etiqueta}</span>

		<!-- `py-1` y no `pb-1`: `overflow-x-auto` recorta también en vertical, y con el padding sólo
		     abajo el anillo de la píldora activa se cortaba contra el borde de arriba. -->
		<div
			role="group"
			aria-label="Elegir rutina"
			class="-mx-1 flex [scrollbar-width:none] gap-2 overflow-x-auto px-1 py-1"
		>
			{#if conTodos}
				<button
					type="button"
					aria-pressed={seleccionada === null}
					class="{BASE} {seleccionada === null ? ENCENDIDA_TODOS : APAGADA}"
					onclick={() => alElegir(null)}
				>
					<span
						class="size-1.5 rounded-full {seleccionada === null
							? 'bg-primary'
							: 'bg-muted-foreground/50'}"
					></span>
					Todos
				</button>
			{/if}

			{#each activas as rutina (rutina.id)}
				{@const tinte = tinteDe(rutina.id)}
				<button
					type="button"
					aria-pressed={seleccionada === rutina.id}
					class="{BASE} {seleccionada === rutina.id ? tinte.activo : APAGADA}"
					onclick={() => alElegir(rutina.id)}
				>
					<span class="size-1.5 rounded-full {tinte.punto}"></span>
					{rutina.nombre}
				</button>
			{/each}
		</div>
	</div>

	<!-- Las inactivas siguen acá y no en un desplegable: son las que se eligen para reactivarlas
	     (HU-RUT-05), así que esconderlas dejaría la reactivación sin puerta de entrada. -->
	{#if inactivas.length > 0}
		<div class="flex flex-col gap-2">
			<span class="text-xs font-medium text-muted-foreground">Inactivas</span>
			<div
				role="group"
				aria-label="Elegir una rutina inactiva"
				class="-mx-1 flex [scrollbar-width:none] gap-2 overflow-x-auto px-1 py-1"
			>
				{#each inactivas as rutina (rutina.id)}
					{@const tinte = tinteDe(rutina.id)}
					<button
						type="button"
						aria-pressed={seleccionada === rutina.id}
						class="{BASE} {seleccionada === rutina.id ? tinte.activo : APAGADA} opacity-70"
						onclick={() => alElegir(rutina.id)}
					>
						<span class="size-1.5 rounded-full {tinte.punto}"></span>
						{rutina.nombre}
					</button>
				{/each}
			</div>
		</div>
	{/if}
</div>
