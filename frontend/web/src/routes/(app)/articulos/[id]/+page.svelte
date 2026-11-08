<script lang="ts">
	import ArrowLeftIcon from '@lucide/svelte/icons/arrow-left';
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { urlDeArchivo } from '$lib/config';
	import DesactivarArticulo from '$lib/components/desactivar-articulo.svelte';
	import Markdown from '$lib/components/markdown.svelte';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';
	import { armarConsulta, leerEnteroPositivo, leerPagina } from '$lib/paginado.svelte';
	import { ROL_ADMINISTRADOR } from '$lib/roles';
	import type { PageProps } from './$types';

	// HU-ART-05: el artículo completo, con su Markdown renderizado, la portada en la cabecera, la
	// categoría y la fecha. El catálogo deja su página y su filtro en la query del enlace, y
	// «Volver» los devuelve tal cual: el estado está en la URL, no en memoria.
	let { data }: PageProps = $props();

	const volver = $derived(
		`${resolve('/(app)/articulos')}${armarConsulta(
			{ categoriaId: leerEnteroPositivo(page.url.searchParams.get('categoriaId')) },
			leerPagina(page.url.searchParams.get('pagina'))
		)}`
	);

	// Corregir es de Administración (HU-ART-02); el botón escondido es comodidad, el 403 lo pone la API.
	const editar = $derived(
		data.articulo && data.usuario.rol === ROL_ADMINISTRADOR
			? `${resolve('/(app)/articulos/[id]/editar', { id: String(data.articulo.id) })}${page.url.search}`
			: null
	);

	const categoria = $derived(
		data.categorias.find((actual) => actual.id === data.articulo?.categoriaId)?.nombre ??
			'Sin categoría'
	);

	// aaaa-mm-dd → dd/mm/aaaa cortando la cadena, igual que la tarjeta: es un día, no un instante.
	const dia = $derived(data.articulo?.fechaPublicacion.split('-').reverse().join('/') ?? '');
</script>

<svelte:head>
	<title>Javito · {data.articulo?.titulo ?? 'Artículo'}</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-3xl flex-col gap-6 px-5 py-10">
	<!-- Las dos URLs se armaron arriba con resolve() y la query del catálogo, que resolve() no tipa. -->
	<div class="flex items-center justify-between gap-2">
		<!-- eslint-disable svelte/no-navigation-without-resolve -->
		<Button variant="ghost" size="sm" href={volver}>
			<ArrowLeftIcon class="size-4" />
			Volver al catálogo
		</Button>

		{#if editar && data.articulo}
			<div class="flex items-center gap-2">
				<Button variant="outline" size="sm" href={editar}>
					<PencilIcon class="size-4" />
					Editar
				</Button>
				<!-- Retirar es de Administración (HU-ART-03). Al confirmar, vuelve al catálogo con su query. -->
				<DesactivarArticulo id={data.articulo.id} alDesactivar={() => goto(volver)} />
			</div>
		{/if}
		<!-- eslint-enable svelte/no-navigation-without-resolve -->
	</div>

	{#if data.articulo === null}
		<Empty class="rounded-xl border border-border bg-card">
			<EmptyHeader>
				<EmptyTitle>Este artículo no existe</EmptyTitle>
				<EmptyDescription
					>Puede que lo hayan dado de baja o que el enlace esté mal.</EmptyDescription
				>
			</EmptyHeader>
		</Empty>
	{:else}
		{@const articulo = data.articulo}
		<article class="flex flex-col gap-6">
			<header class="flex flex-col gap-4">
				{#if articulo.imagenPortadaUrl}
					<img
						src={urlDeArchivo(articulo.imagenPortadaUrl)}
						alt="Portada de {articulo.titulo}"
						class="aspect-video w-full rounded-xl object-cover"
					/>
				{/if}

				<h1 class="text-3xl font-semibold tracking-tight">{articulo.titulo}</h1>

				<div class="flex items-center gap-2 text-sm text-muted-foreground">
					<span class="rounded-full border border-border px-2 py-0.5">{categoria}</span>
					<time datetime={articulo.fechaPublicacion}>{dia}</time>
				</div>

				<p class="text-muted-foreground">{articulo.resumen}</p>
			</header>

			<Markdown contenido={articulo.contenidoMarkdown} />
		</article>
	{/if}
</main>
