<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { editar, type DatosDeArticulo } from '$lib/api/articulos';
	import { CAMPOS_POR_CODIGO_DE_ARTICULO, mapearErroresDeFormulario } from '$lib/api/errores';
	import ArticuloForm from '$lib/components/articulo-form.svelte';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';
	import type { PageProps } from './$types';

	// HU-ART-02: el mismo formulario del alta, precargado. Guardar vuelve al detalle con la query del
	// catálogo que trajo el enlace, así «Volver al catálogo» sigue llevando a la misma página.
	let { data }: PageProps = $props();

	let enviando = $state(false);
	let mensajeError = $state('');
	let errores = $state<Record<string, string>>({});

	async function guardar(id: number, datos: DatosDeArticulo, portada: File | null) {
		enviando = true;
		mensajeError = '';
		errores = {};

		try {
			await editar(id, datos, portada);
			const detalle = resolve('/(app)/articulos/[id]', { id: String(id) });

			// eslint-disable-next-line svelte/no-navigation-without-resolve -- la ruta ya está resuelta; se le suma la query.
			await goto(`${detalle}${page.url.search}`);
		} catch (error) {
			const mapeado = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_ARTICULO);

			errores = mapeado.campos;
			mensajeError = mapeado.mensaje;
		} finally {
			enviando = false;
		}
	}
</script>

<svelte:head>
	<title>Javito · Editar artículo</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Editar artículo</h1>

	{#if data.articulo === null}
		<Empty class="rounded-xl border border-border bg-card">
			<EmptyHeader>
				<EmptyTitle>Este artículo no existe</EmptyTitle>
				<EmptyDescription>
					Puede que lo hayan dado de baja o que el enlace esté mal. Un artículo dado de baja se
					reactiva antes de editarlo.
				</EmptyDescription>
			</EmptyHeader>
		</Empty>
	{:else}
		{@const articulo = data.articulo}

		{#if mensajeError}
			<Alert variant="destructive" role="alert">
				<AlertDescription>{mensajeError}</AlertDescription>
			</Alert>
		{/if}

		<ArticuloForm
			categorias={data.categorias}
			inicial={articulo}
			portadaActual={articulo.imagenPortadaUrl}
			{enviando}
			{errores}
			textoDelBoton="Guardar cambios"
			alEnviar={(datos, portada) => guardar(articulo.id, datos, portada)}
		/>
	{/if}
</main>
