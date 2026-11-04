<script lang="ts">
	import DOMPurify from 'dompurify';
	import { marked } from 'marked';
	import { browser } from '$app/env';

	/**
	 * Contenido en Markdown renderizado. Un solo componente para la vista previa del alta y para el
	 * detalle del artículo (HU-ART-01, HU-ART-05): si la previa usara otro camino, la previa mentiría.
	 */
	let { contenido, clase = '' }: { contenido: string; clase?: string } = $props();

	// Sanitizar no es una precaución, es la regla: el Markdown lo escribe Administración y se
	// renderiza en el navegador de cada lector. Sin DOMPurify, un `<img onerror>` guardado en un
	// artículo es un XSS almacenado que se dispara en todas las cuentas. `{@html}` nunca recibe la
	// salida de `marked` sin pasar por acá.
	const html = $derived(
		browser ? DOMPurify.sanitize(marked.parse(contenido, { async: false })) : ''
	);
</script>

<div class="prose max-w-none dark:prose-invert {clase}">
	<!-- La entrada de este `{@html}` acaba de pasar por DOMPurify.sanitize: el lint no lo puede ver a
	     través del `$derived` y lo marca igual. -->
	<!-- eslint-disable-next-line svelte/no-at-html-tags -->
	{@html html}
</div>
