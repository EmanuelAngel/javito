<script lang="ts">
	import ChevronLeftIcon from '@lucide/svelte/icons/chevron-left';
	import ChevronRightIcon from '@lucide/svelte/icons/chevron-right';
	import { Button } from '$lib/components/ui/button/index.js';

	// Paginador de cualquier catálogo: no sabe qué pagina. El catálogo de artículos lo estrena y los
	// de hábitos y usuarios lo van a reusar con sus propios números, así que acá no hay ni una
	// palabra del dominio. La pantalla decide qué significa cambiar de página; esto sólo avisa.
	let {
		pagina,
		totalDePaginas,
		alCambiar,
		etiqueta = 'Paginación'
	}: {
		pagina: number;
		totalDePaginas: number;
		/** Pedido de otra página. El que escucha valida el rango antes de navegar. */
		alCambiar: (pagina: number) => void;
		/** Nombre del grupo para lectores de pantalla: cada listado lo suyo. */
		etiqueta?: string;
	} = $props();

	// Los bordes se deshabilitan en vez de esconderse: si el botón desaparece, el foco salta y la
	// barra cambia de ancho con cada página. Deshabilitado, el recorrido se ve completo.
	const hayAnterior = $derived(pagina > 1);
	const haySiguiente = $derived(pagina < totalDePaginas);
</script>

<nav
	aria-label={etiqueta}
	class="flex items-center justify-between gap-3 rounded-xl border border-border bg-card px-3 py-2"
>
	<Button
		type="button"
		variant="outline"
		size="sm"
		disabled={!hayAnterior}
		onclick={() => alCambiar(pagina - 1)}
	>
		<ChevronLeftIcon aria-hidden="true" />
		Anterior
	</Button>

	<!-- `aria-live` para que cambiar de página se anuncie sin mover el foco: la lista de arriba
		 cambia entera y quien no ve la pantalla no tendría cómo enterarse. -->
	<p class="text-sm text-muted-foreground tabular-nums" aria-live="polite">
		Página {pagina} de {totalDePaginas}
	</p>

	<Button
		type="button"
		variant="outline"
		size="sm"
		disabled={!haySiguiente}
		onclick={() => alCambiar(pagina + 1)}
	>
		Siguiente
		<ChevronRightIcon aria-hidden="true" />
	</Button>
</nav>
