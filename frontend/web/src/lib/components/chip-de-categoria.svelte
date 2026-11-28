<script lang="ts" module>
	// El color de la categoría es una convención de la interfaz, no un dato: sale del nombre y vive
	// acá. Los cinco del seed tienen acento propio y elegido —cuerpo cálido, mente violeta, agua para
	// lo que se toma, oro para el oficio, pino para lo contemplativo—. «rose» queda libre a propósito:
	// es el que ya sobraba en la paleta.
	//
	// El texto no va en el acento puro porque en Dawn no llega a AA (gold 2.16:1, iris 3.65:1). Cada
	// acento se mezcla con el `foreground` del tema hasta el máximo que todavía pasa 4.5:1 sobre su
	// propio tinte: así el chip se lee en su color y no en un gris. La mezcla va hacia el foreground y
	// no hacia negro para que el tema oscuro funcione con la misma regla.
	//
	// Las clases van escritas enteras: Tailwind las lee del archivo, y una plantilla
	// `bg-accent-${nombre}` no generaría nada.
	type Acento = { fondo: string; texto: string };

	const ACENTOS: Record<string, Acento> = {
		cuerpo: {
			fondo: 'bg-accent-love/15',
			texto: 'text-[color-mix(in_oklab,var(--accent-love)_45%,var(--foreground))]'
		},
		mente: {
			fondo: 'bg-accent-iris/15',
			texto: 'text-[color-mix(in_oklab,var(--accent-iris)_40%,var(--foreground))]'
		},
		alimentación: {
			fondo: 'bg-accent-foam/15',
			texto: 'text-[color-mix(in_oklab,var(--accent-foam)_35%,var(--foreground))]'
		},
		creatividad: {
			fondo: 'bg-accent-gold/15',
			texto: 'text-[color-mix(in_oklab,var(--accent-gold)_25%,var(--foreground))]'
		},
		espiritualidad: {
			fondo: 'bg-accent-pine/15',
			texto: 'text-[color-mix(in_oklab,var(--accent-pine)_85%,var(--foreground))]'
		}
	};

	// Una categoría nueva cae en el hash del nombre: hereda un acento estable sin que nadie lo configure.
	const DE_RESPALDO: Acento[] = [
		ACENTOS.cuerpo,
		ACENTOS.mente,
		ACENTOS.espiritualidad,
		ACENTOS.alimentación,
		ACENTOS.creatividad,
		{
			fondo: 'bg-accent-rose/15',
			texto: 'text-[color-mix(in_oklab,var(--accent-rose)_25%,var(--foreground))]'
		}
	];

	export function acentoDe(nombre: string): Acento {
		const clave = nombre.trim().toLowerCase();
		const elegido = ACENTOS[clave];

		if (elegido) {
			return elegido;
		}

		let hash = 0;
		for (const caracter of clave) {
			hash = (hash * 31 + caracter.codePointAt(0)!) % 997;
		}

		return DE_RESPALDO[hash % DE_RESPALDO.length];
	}
</script>

<script lang="ts">
	// La categoría como etiqueta, no como estado: fondo suave del acento y texto en el acento
	// entibiecito. Sin `data-slot="badge"`: no es un `Badge` de shadcn, y las pruebas de las listas
	// buscan el primer badge para leer el progreso de la semana.
	let { nombre }: { nombre: string | null } = $props();

	const acento = $derived(nombre === null ? null : acentoDe(nombre));
</script>

{#if acento !== null && nombre !== null}
	<span
		data-slot="chip-categoria"
		class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium {acento.fondo} {acento.texto}"
	>
		{nombre}
	</span>
{:else}
	<span data-slot="chip-categoria" class="text-xs text-muted-foreground">Sin categoría</span>
{/if}
