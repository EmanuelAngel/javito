<script lang="ts">
	import {
		FORMATOS_DE_LA_PORTADA,
		TAMANO_MAXIMO_DE_LA_PORTADA,
		type DatosDeArticulo
	} from '$lib/api/articulos';
	import type { Categoria } from '$lib/api/categorias';
	import Markdown from '$lib/components/markdown.svelte';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';
	import { Textarea } from '$lib/components/ui/textarea/index.js';

	// Campos, portada, errores por campo y vista previa del artículo (#63). El alta lo usa vacío y la
	// edición (ART-02) lo precarga con `inicial`: agregar un campo toca sólo este archivo. Para
	// vaciarlo después de publicar, la pantalla lo vuelve a montar con un `{#key}`.
	type Props = {
		categorias: Categoria[];
		inicial?: DatosDeArticulo;
		enviando: boolean;
		/** Mensaje por campo que devolvió el servidor, con la clave del campo del formulario. */
		errores: Record<string, string>;
		textoDelBoton: string;
		alEnviar: (datos: DatosDeArticulo, portada: File | null) => void;
	};

	let { categorias, inicial, enviando, errores, textoDelBoton, alEnviar }: Props = $props();

	/** Lo que el editor muestra mientras está vacío: el Markdown, no una descripción de él. */
	const EJEMPLO_DE_MARKDOWN =
		'# Un título\n\nTexto con **negritas** y listas:\n\n- primero\n- segundo';

	// Los valores iniciales son una instantánea: el formulario no sigue al padre mientras se escribe.
	// svelte-ignore state_referenced_locally
	const valores = $state({
		titulo: inicial?.titulo ?? '',
		resumen: inicial?.resumen ?? '',
		contenido: inicial?.contenidoMarkdown ?? '',
		categoriaId: inicial ? String(inicial.categoriaId) : ''
	});

	let portada = $state<File | null>(null);
	let errorDePortada = $state('');

	/** El error del cliente (el tope de 5 MB) gana sobre el del servidor, que es de un envío anterior. */
	function errorDe(campo: string): string | undefined {
		return campo === 'portada' && errorDePortada !== '' ? errorDePortada : errores[campo];
	}

	function mensajes(campo: string) {
		const mensaje = errorDe(campo);

		return mensaje ? [{ message: mensaje }] : [];
	}

	function elegirPortada(evento: Event) {
		const entrada = evento.currentTarget as HTMLInputElement;
		const archivo = entrada.files?.[0] ?? null;

		errorDePortada = '';

		// El servidor aplica el mismo tope, pero avisar antes ahorra subir 20 MB al pedo.
		if (archivo && archivo.size > TAMANO_MAXIMO_DE_LA_PORTADA) {
			portada = null;
			entrada.value = '';
			errorDePortada = 'La portada no puede superar los 5 MB.';
			return;
		}

		portada = archivo;
	}

	function enviar(evento: SubmitEvent) {
		evento.preventDefault();
		errorDePortada = '';
		alEnviar(
			{
				titulo: valores.titulo,
				resumen: valores.resumen,
				contenidoMarkdown: valores.contenido,
				categoriaId: Number(valores.categoriaId)
			},
			portada
		);
	}
</script>

<!-- Escribir a la izquierda y ver el resultado a la derecha: la vista previa usa el mismo
	 renderizador que el detalle, así no puede mentir. En pantalla angosta se apilan. -->
<div class="grid gap-6 lg:grid-cols-2">
	<form class="flex flex-col gap-4 rounded-xl border border-border bg-card p-6" onsubmit={enviar}>
		<Field.Group>
			<Field.Field data-invalid={errorDe('titulo') !== undefined}>
				<Field.Label for="articulo-titulo">Título</Field.Label>
				<Input id="articulo-titulo" bind:value={valores.titulo} required maxlength={150} />
				<Field.Error errors={mensajes('titulo')} />
			</Field.Field>

			<Field.Field data-invalid={errorDe('resumen') !== undefined}>
				<Field.Label for="articulo-resumen">Resumen</Field.Label>
				<Textarea
					id="articulo-resumen"
					bind:value={valores.resumen}
					required
					maxlength={300}
					rows={2}
				/>
				<Field.Error errors={mensajes('resumen')} />
			</Field.Field>

			<Field.Field data-invalid={errorDe('categoriaId') !== undefined}>
				<Field.Label for="articulo-categoria">Categoría</Field.Label>
				<NativeSelect
					id="articulo-categoria"
					class="w-full"
					bind:value={valores.categoriaId}
					required
					aria-invalid={errorDe('categoriaId') !== undefined}
				>
					<NativeSelectOption value="" disabled>Elegí una categoría…</NativeSelectOption>
					{#each categorias as categoria (categoria.id)}
						<NativeSelectOption value={String(categoria.id)}>{categoria.nombre}</NativeSelectOption>
					{/each}
				</NativeSelect>
				<Field.Error errors={mensajes('categoriaId')} />
			</Field.Field>

			<Field.Field data-invalid={errorDe('portada') !== undefined}>
				<Field.Label for="articulo-portada">Portada (opcional)</Field.Label>
				<Input
					id="articulo-portada"
					type="file"
					accept={FORMATOS_DE_LA_PORTADA}
					onchange={elegirPortada}
					aria-invalid={errorDe('portada') !== undefined}
				/>
				<Field.Description>
					JPEG, PNG o WebP de hasta 5 MB. Sin portada el artículo se publica igual.
				</Field.Description>
				<Field.Error errors={mensajes('portada')} />
			</Field.Field>

			<Field.Field data-invalid={errorDe('contenidoMarkdown') !== undefined}>
				<Field.Label for="articulo-contenido">Contenido (Markdown)</Field.Label>
				<Textarea
					id="articulo-contenido"
					bind:value={valores.contenido}
					required
					rows={16}
					class="font-mono text-sm"
					placeholder={EJEMPLO_DE_MARKDOWN}
				/>
				<Field.Error errors={mensajes('contenidoMarkdown')} />
			</Field.Field>
		</Field.Group>

		<Button type="submit" disabled={enviando}>{textoDelBoton}</Button>
	</form>

	<section class="flex flex-col gap-3 self-start lg:sticky lg:top-6">
		<h2 class="text-lg font-semibold">Vista previa</h2>

		<div class="rounded-xl border border-border bg-card p-6">
			{#if valores.contenido.trim() === ''}
				<p class="text-sm text-muted-foreground">
					Lo que escribas en el contenido aparece acá, tal como lo va a ver el lector.
				</p>
			{:else}
				<Markdown contenido={valores.contenido} />
			{/if}
		</div>
	</section>
</div>
