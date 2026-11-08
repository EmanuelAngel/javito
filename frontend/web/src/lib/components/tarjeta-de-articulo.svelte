<script lang="ts">
	import ImageIcon from '@lucide/svelte/icons/image';
	import type { ArticuloDelCatalogo } from '$lib/api/articulos';
	import { urlDeArchivo } from '$lib/config';

	// Una tarjeta del catálogo (HU-ART-04). Muestra los cinco datos de la historia. La categoría llega
	// resuelta porque la API manda el id y el join no existe en el servidor: el SPA ya tiene el
	// catálogo completo para el filtro.
	let {
		articulo,
		categoria,
		href
	}: {
		articulo: ArticuloDelCatalogo;
		categoria: string;
		/** El detalle (HU-ART-05). Lo arma el catálogo, que sabe en qué página y filtro está. */
		href: string;
	} = $props();

	// aaaa-mm-dd → dd/mm/aaaa. Se corta la cadena en vez de pasar por `Date`: esto es un día, no un
	// instante, y construirlo como fecha local es el camino por el que se cuela el corrimiento de
	// huso que ya documenta `lib/fechas.ts`.
	const dia = $derived(articulo.fechaPublicacion.split('-').reverse().join('/'));
</script>

<!-- Toda la tarjeta lleva al detalle (HU-ART-05): es el destino acordado en `ux-brief.md`. El
	 `href` ya viene resuelto por el catálogo, con su query; acá sólo se usa. -->
<!-- eslint-disable svelte/no-navigation-without-resolve -->
<a
	{href}
	class="flex h-full flex-col overflow-hidden rounded-xl border border-border bg-card transition-colors hover:border-primary focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
>
	<!-- eslint-enable svelte/no-navigation-without-resolve -->
	{#if articulo.imagenPortadaUrl}
		<img
			src={urlDeArchivo(articulo.imagenPortadaUrl)}
			alt="Portada de {articulo.titulo}"
			class="aspect-video w-full object-cover"
		/>
	{:else}
		<!-- Sin portada el artículo se publica igual: el hueco es neutro y del mismo alto, así las
			 tarjetas de una misma grilla no quedan desparejas. -->
		<div
			role="img"
			aria-label="Sin portada"
			class="grid aspect-video w-full place-items-center bg-muted text-muted-foreground"
		>
			<ImageIcon class="size-6" aria-hidden="true" />
		</div>
	{/if}

	<div class="flex flex-1 flex-col gap-2 p-4">
		<h3 class="text-lg leading-tight font-semibold">{articulo.titulo}</h3>

		<p class="line-clamp-3 text-sm text-muted-foreground">{articulo.resumen}</p>

		<div class="mt-auto flex items-center gap-2 pt-2 text-xs text-muted-foreground">
			<span class="rounded-full border border-border px-2 py-0.5">{categoria}</span>
			<time datetime={articulo.fechaPublicacion}>{dia}</time>
		</div>
	</div>
</a>
