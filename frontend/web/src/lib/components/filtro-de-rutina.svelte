<script lang="ts">
	import type { Rutina } from '$lib/api/rutinas';

	// El selector de rutina de la pantalla del día (HU-RUT-04). Sólo lee el control: la pantalla es la
	// dueña de la URL. Llegan sólo las rutinas activas; `null` es «todos».
	type Props = {
		rutinas: Rutina[];
		rutinaId: number | null;
		alElegir: (rutinaId: number | null) => void;
	};

	let { rutinas, rutinaId, alElegir }: Props = $props();

	// El punto de color es un detalle, no un dato: rota por posición para que cada rutina tenga el suyo
	// y la fila no sea una tira gris. La activa se rellena con ese mismo acento.
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
</script>

<div data-slot="filtro-de-rutina" class="flex flex-col gap-2">
	<span class="text-xs font-medium text-muted-foreground">Rutina</span>

	<!-- `py-1` y no `pb-1`: `overflow-x-auto` también recorta en vertical, y con el padding sólo abajo
	     el anillo de la píldora activa se cortaba contra el borde de arriba. -->
	<div
		role="group"
		aria-label="Filtrar el día por rutina"
		class="-mx-1 flex [scrollbar-width:none] gap-2 overflow-x-auto px-1 py-1"
	>
		<button
			type="button"
			aria-pressed={rutinaId === null}
			class="{BASE} {rutinaId === null ? ENCENDIDA_TODOS : APAGADA}"
			onclick={() => alElegir(null)}
		>
			<span
				class="size-1.5 rounded-full {rutinaId === null ? 'bg-primary' : 'bg-muted-foreground/50'}"
			></span>
			Todos
		</button>

		{#each rutinas as rutina, indice (rutina.id)}
			{@const tinte = TINTES[indice % TINTES.length]}
			<button
				type="button"
				aria-pressed={rutinaId === rutina.id}
				class="{BASE} {rutinaId === rutina.id ? tinte.activo : APAGADA}"
				onclick={() => alElegir(rutina.id)}
			>
				<span class="size-1.5 rounded-full {tinte.punto}"></span>
				{rutina.nombre}
			</button>
		{/each}
	</div>
</div>
