<script lang="ts">
	import { resolve } from '$app/paths';
	import {
		FORMATOS_DE_LA_PORTADA,
		TAMANO_MAXIMO_DE_LA_PORTADA,
		publicar,
		type DatosDeArticulo
	} from '$lib/api/articulos';
	import { CAMPOS_POR_CODIGO_DE_ARTICULO, mapearErroresDeFormulario } from '$lib/api/errores';
	import Markdown from '$lib/components/markdown.svelte';
	import { Alert, AlertAction, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';
	import { Textarea } from '$lib/components/ui/textarea/index.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	/** Lo que el editor muestra mientras está vacío: el Markdown, no una descripción de él. */
	const EJEMPLO_DE_MARKDOWN =
		'# Un título\n\nTexto con **negritas** y listas:\n\n- primero\n- segundo';

	// El catálogo de categorías no cambia mientras el formulario está abierto: es una instantánea
	// del load, no estado reactivo que haya que seguir.
	// svelte-ignore state_referenced_locally
	const categorias = data.categorias;

	let titulo = $state('');
	let resumen = $state('');
	let contenido = $state('');
	let categoriaId = $state('');
	let portada = $state<File | null>(null);
	let entradaDePortada = $state<HTMLInputElement | null>(null);
	let enviando = $state(false);
	let ultimoPublicado = $state<{ id: number; titulo: string } | null>(null);
	let mensajeError = $state('');
	let errores = $state<Record<string, string>>({});

	function limpiarErrores() {
		mensajeError = '';
		errores = {};
	}

	function mostrarError(error: unknown) {
		const mapeado = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_ARTICULO);

		errores = mapeado.campos;
		mensajeError = mapeado.mensaje;
	}

	function elegirPortada(evento: Event) {
		const entrada = evento.currentTarget as HTMLInputElement;
		const archivo = entrada.files?.[0] ?? null;

		limpiarErrores();

		// El servidor aplica el mismo tope, pero avisar antes ahorra subir 20 MB al pedo.
		if (archivo && archivo.size > TAMANO_MAXIMO_DE_LA_PORTADA) {
			portada = null;
			entrada.value = '';
			errores = { portada: 'La portada no puede superar los 5 MB.' };
			return;
		}

		portada = archivo;
	}

	function reiniciar() {
		titulo = '';
		resumen = '';
		contenido = '';
		categoriaId = '';
		portada = null;
		limpiarErrores();

		if (entradaDePortada) entradaDePortada.value = '';
	}

	async function guardar(evento: SubmitEvent) {
		evento.preventDefault();
		enviando = true;
		limpiarErrores();
		ultimoPublicado = null;

		const datos: DatosDeArticulo = {
			titulo,
			resumen,
			contenidoMarkdown: contenido,
			categoriaId: Number(categoriaId)
		};

		try {
			const creado = await publicar(datos, portada);

			ultimoPublicado = { id: creado.id, titulo: creado.titulo };
			reiniciar();
		} catch (error) {
			mostrarError(error);
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
			<!-- El catálogo ya existe: el que acaba de publicar tiene que poder ir a verlo sin
				 escribir la URL. -->
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

	<!-- Escribir a la izquierda y ver el resultado a la derecha: la vista previa usa el mismo
		 renderizador que el detalle, así no puede mentir. En pantalla angosta se apilan. -->
	<div class="grid gap-6 lg:grid-cols-2">
		<form
			class="flex flex-col gap-4 rounded-xl border border-border bg-card p-6"
			onsubmit={guardar}
		>
			<Field.Group>
				<Field.Field data-invalid={errores['titulo'] !== undefined}>
					<Field.Label for="articulo-titulo">Título</Field.Label>
					<Input id="articulo-titulo" bind:value={titulo} required maxlength={150} />
					<Field.Error errors={errores['titulo'] ? [{ message: errores['titulo'] }] : []} />
				</Field.Field>

				<Field.Field data-invalid={errores['resumen'] !== undefined}>
					<Field.Label for="articulo-resumen">Resumen</Field.Label>
					<Textarea id="articulo-resumen" bind:value={resumen} required maxlength={300} rows={2} />
					<Field.Error errors={errores['resumen'] ? [{ message: errores['resumen'] }] : []} />
				</Field.Field>

				<Field.Field data-invalid={errores['categoriaId'] !== undefined}>
					<Field.Label for="articulo-categoria">Categoría</Field.Label>
					<NativeSelect
						id="articulo-categoria"
						class="w-full"
						bind:value={categoriaId}
						required
						aria-invalid={errores['categoriaId'] !== undefined}
					>
						<NativeSelectOption value="" disabled>Elegí una categoría…</NativeSelectOption>
						{#each categorias as categoria (categoria.id)}
							<NativeSelectOption value={categoria.id}>{categoria.nombre}</NativeSelectOption>
						{/each}
					</NativeSelect>
					<Field.Error
						errors={errores['categoriaId'] ? [{ message: errores['categoriaId'] }] : []}
					/>
				</Field.Field>

				<Field.Field data-invalid={errores['portada'] !== undefined}>
					<Field.Label for="articulo-portada">Portada (opcional)</Field.Label>
					<Input
						id="articulo-portada"
						bind:ref={entradaDePortada}
						type="file"
						accept={FORMATOS_DE_LA_PORTADA}
						onchange={elegirPortada}
						aria-invalid={errores['portada'] !== undefined}
					/>
					<Field.Description>
						JPEG, PNG o WebP de hasta 5 MB. Sin portada el artículo se publica igual.
					</Field.Description>
					<Field.Error errors={errores['portada'] ? [{ message: errores['portada'] }] : []} />
				</Field.Field>

				<Field.Field data-invalid={errores['contenidoMarkdown'] !== undefined}>
					<Field.Label for="articulo-contenido">Contenido (Markdown)</Field.Label>
					<Textarea
						id="articulo-contenido"
						bind:value={contenido}
						required
						rows={16}
						class="font-mono text-sm"
						placeholder={EJEMPLO_DE_MARKDOWN}
					/>
					<Field.Error
						errors={errores['contenidoMarkdown'] ? [{ message: errores['contenidoMarkdown'] }] : []}
					/>
				</Field.Field>
			</Field.Group>

			<Button type="submit" disabled={enviando}>Publicar artículo</Button>
		</form>

		<section class="flex flex-col gap-3 self-start lg:sticky lg:top-6">
			<h2 class="text-lg font-semibold">Vista previa</h2>

			<div class="rounded-xl border border-border bg-card p-6">
				{#if contenido.trim() === ''}
					<p class="text-sm text-muted-foreground">
						Lo que escribas en el contenido aparece acá, tal como lo va a ver el lector.
					</p>
				{:else}
					<Markdown {contenido} />
				{/if}
			</div>
		</section>
	</div>
</main>
