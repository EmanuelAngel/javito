<script lang="ts">
	import { resolve } from '$app/paths';
	import { publicar, type DatosDeArticulo } from '$lib/api/articulos';
	import { CAMPOS_POR_CODIGO_DE_ARTICULO, mapearErroresDeFormulario } from '$lib/api/errores';
	import ArticuloForm from '$lib/components/articulo-form.svelte';
	import { Alert, AlertAction, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let enviando = $state(false);
	let ultimoPublicado = $state<{ id: number; titulo: string } | null>(null);
	let mensajeError = $state('');
	let errores = $state<Record<string, string>>({});

	// Cada publicación vuelve a montar el formulario: así queda vacío, archivo incluido, sin que la
	// pantalla tenga que saber qué campos tiene (#63).
	let publicaciones = $state(0);

	async function guardar(datos: DatosDeArticulo, portada: File | null) {
		enviando = true;
		mensajeError = '';
		errores = {};
		ultimoPublicado = null;

		try {
			const creado = await publicar(datos, portada);

			ultimoPublicado = { id: creado.id, titulo: creado.titulo };
			publicaciones++;
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
	<title>Javito · Publicar artículo</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Publicar artículo</h1>

	{#if ultimoPublicado}
		<Alert role="status">
			<AlertDescription>
				Publicado «{ultimoPublicado.titulo}». El artículo nace activo y ya se puede leer.
			</AlertDescription>
			<!-- El que acaba de publicar tiene que poder ir a verlo sin escribir la URL. -->
			<AlertAction>
				<Button variant="outline" size="xs" href={resolve('/(app)/articulos')}>
					Ver en el catálogo
				</Button>
			</AlertAction>
		</Alert>
	{/if}

	{#if mensajeError}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{mensajeError}</AlertDescription>
		</Alert>
	{/if}

	{#key publicaciones}
		<ArticuloForm
			categorias={data.categorias}
			{enviando}
			{errores}
			textoDelBoton="Publicar artículo"
			alEnviar={guardar}
		/>
	{/key}
</main>
