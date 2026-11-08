<script lang="ts">
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';

	// Los vacíos del historial: la página pedida no existe, el filtro no deja nada, o todavía no hay
	// registros. Dicen cosas distintas a propósito, como los del catálogo.
	type Props = {
		paginaInexistente: boolean;
		filtrado: boolean;
		pagina: number;
		totalDePaginas: number;
		alIrALaUltima: () => void;
		alLimpiarFiltros: () => void;
	};

	let {
		paginaInexistente,
		filtrado,
		pagina,
		totalDePaginas,
		alIrALaUltima,
		alLimpiarFiltros
	}: Props = $props();
</script>

<Empty class="rounded-xl border border-border bg-card">
	<EmptyHeader>
		{#if paginaInexistente}
			<EmptyTitle>Esta página no existe</EmptyTitle>
			<EmptyDescription>
				El historial no llega a la página {pagina}: hay
				{totalDePaginas}
				{totalDePaginas === 1 ? 'página' : 'páginas'}.
			</EmptyDescription>
		{:else if filtrado}
			<EmptyTitle>No hay registros con esos filtros</EmptyTitle>
			<EmptyDescription>
				Probá con otro hábito, con otro rango de fechas, o mirá todo el historial.
			</EmptyDescription>
		{:else}
			<EmptyTitle>Todavía no hay registros</EmptyTitle>
			<EmptyDescription>
				Cuando marques un hábito en la pantalla del día, va a aparecer acá.
			</EmptyDescription>
		{/if}
	</EmptyHeader>

	{#if paginaInexistente}
		<Button type="button" variant="outline" onclick={alIrALaUltima}>Ir a la última página</Button>
	{:else if filtrado}
		<Button type="button" variant="outline" onclick={alLimpiarFiltros}>Quitar los filtros</Button>
	{/if}
</Empty>
