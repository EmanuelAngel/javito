<script lang="ts">
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';

	// Los tres vacíos del catálogo de artículos (#63): la página pedida no existe, no hay artículos
	// en la categoría elegida, o no hay ninguno todavía. Cada uno dice algo distinto a propósito.
	type Props = {
		paginaInexistente: boolean;
		filtrado: boolean;
		pagina: number;
		totalDePaginas: number;
		alIrALaUltima: () => void;
	};

	let { paginaInexistente, filtrado, pagina, totalDePaginas, alIrALaUltima }: Props = $props();
</script>

<Empty class="rounded-xl border border-border bg-card">
	<EmptyHeader>
		{#if paginaInexistente}
			<EmptyTitle>Esta página no existe</EmptyTitle>
			<EmptyDescription>
				El catálogo no llega a la página {pagina}: hay
				{totalDePaginas}
				{totalDePaginas === 1 ? 'página' : 'páginas'}.
			</EmptyDescription>
		{:else if filtrado}
			<EmptyTitle>No hay artículos en esta categoría</EmptyTitle>
			<EmptyDescription>Probá con otra categoría o volvé a «Todas las categorías».</EmptyDescription
			>
		{:else}
			<EmptyTitle>Todavía no hay artículos</EmptyTitle>
			<EmptyDescription>
				Cuando se publique el primero va a aparecer acá, del más nuevo al más viejo.
			</EmptyDescription>
		{/if}
	</EmptyHeader>

	{#if paginaInexistente}
		<Button type="button" variant="outline" onclick={alIrALaUltima}>Ir a la última página</Button>
	{/if}
</Empty>
