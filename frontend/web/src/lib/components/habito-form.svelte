<script lang="ts">
	import type { EstadoDeHabitos } from '$lib/habitos.svelte';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';

	let { estado, crear }: EstadoDeHabitos = $props();

	// Meta y unidad aparecen nomás con un contador; veces, con una frecuencia semanal (S6/S9).
	const esContador = $derived(estado.formulario.tipo === 'contador');
	const esSemanal = $derived(estado.formulario.frecuencia === 'semanal');

	// El desplegable muestra las activas: una categoría archivada no admite hábitos nuevos.
	const categoriasActivas = $derived(estado.categorias.filter((categoria) => categoria.activo));

	function mensajeDe(campo: string): string {
		return estado.errores.campos[campo] ?? '';
	}

	// Field.Error recibe la lista de errores del campo: vacía, no renderiza nada.
	function erroresDe(campo: string) {
		const mensaje = mensajeDe(campo);
		return mensaje ? [{ message: mensaje }] : [];
	}

	async function guardar(evento: SubmitEvent): Promise<void> {
		evento.preventDefault();
		await crear();
	}
</script>

<form
	data-test="formulario-habito"
	class="flex w-full flex-col gap-4 self-start rounded-xl border border-border bg-card p-6 lg:w-80"
	onsubmit={guardar}
>
	<h2 class="text-lg font-semibold">Nuevo hábito</h2>

	<Field.Group>
		<Field.Field data-invalid={mensajeDe('titulo') !== ''}>
			<Field.Label for="habito-titulo">Título</Field.Label>
			<Input
				id="habito-titulo"
				data-test="campo-titulo"
				bind:value={estado.formulario.titulo}
				required
				maxlength={100}
				aria-invalid={mensajeDe('titulo') !== ''}
			/>
			<Field.Error errors={erroresDe('titulo')} />
		</Field.Field>

		<Field.Field data-invalid={mensajeDe('categoriaId') !== ''}>
			<Field.Label for="habito-categoria">Categoría</Field.Label>
			<NativeSelect
				id="habito-categoria"
				class="w-full"
				bind:value={estado.formulario.categoriaId}
				required
				aria-invalid={mensajeDe('categoriaId') !== ''}
			>
				<NativeSelectOption value="" disabled>Elegí una categoría…</NativeSelectOption>
				{#each categoriasActivas as categoria (categoria.id)}
					<NativeSelectOption value={categoria.id}>{categoria.nombre}</NativeSelectOption>
				{/each}
			</NativeSelect>
			<Field.Error errors={erroresDe('categoriaId')} />
		</Field.Field>

		<Field.Field>
			<Field.Label for="habito-tipo">Tipo</Field.Label>
			<NativeSelect id="habito-tipo" class="w-full" bind:value={estado.formulario.tipo}>
				<NativeSelectOption value="si_no">Sí o no</NativeSelectOption>
				<NativeSelectOption value="contador">Contador</NativeSelectOption>
				<NativeSelectOption value="abstinencia">Abstinencia</NativeSelectOption>
			</NativeSelect>
		</Field.Field>

		<Field.Field>
			<Field.Label for="habito-frecuencia">Frecuencia</Field.Label>
			<NativeSelect id="habito-frecuencia" class="w-full" bind:value={estado.formulario.frecuencia}>
				<NativeSelectOption value="diaria">Todos los días</NativeSelectOption>
				<NativeSelectOption value="semanal">Varias veces por semana</NativeSelectOption>
			</NativeSelect>
		</Field.Field>

		{#if esSemanal}
			<Field.Field data-invalid={mensajeDe('veces') !== ''}>
				<Field.Label for="habito-veces">¿Cuántas veces?</Field.Label>
				<Input
					id="habito-veces"
					data-test="campo-veces"
					type="number"
					bind:value={estado.formulario.veces}
					required
					min={1}
					max={6}
					step={1}
					aria-invalid={mensajeDe('veces') !== ''}
				/>
				<Field.Error errors={erroresDe('veces')} />
			</Field.Field>
		{/if}

		{#if esContador}
			<div class="grid grid-cols-2 gap-3">
				<Field.Field data-invalid={mensajeDe('meta') !== ''}>
					<Field.Label for="habito-meta">Meta</Field.Label>
					<Input
						id="habito-meta"
						data-test="campo-meta"
						type="number"
						bind:value={estado.formulario.meta}
						required
						min={1}
						step={1}
						aria-invalid={mensajeDe('meta') !== ''}
					/>
					<Field.Error errors={erroresDe('meta')} />
				</Field.Field>

				<Field.Field data-invalid={mensajeDe('unidad') !== ''}>
					<Field.Label for="habito-unidad">Unidad</Field.Label>
					<Input
						id="habito-unidad"
						data-test="campo-unidad"
						bind:value={estado.formulario.unidad}
						required
						maxlength={30}
						placeholder="páginas"
						aria-invalid={mensajeDe('unidad') !== ''}
					/>
					<Field.Error errors={erroresDe('unidad')} />
				</Field.Field>
			</div>
		{/if}

		<Field.Field data-invalid={mensajeDe('descripcion') !== ''}>
			<Field.Label for="habito-descripcion">Descripción</Field.Label>
			<Input
				id="habito-descripcion"
				bind:value={estado.formulario.descripcion}
				maxlength={255}
				aria-invalid={mensajeDe('descripcion') !== ''}
			/>
			<Field.Error errors={erroresDe('descripcion')} />
		</Field.Field>
	</Field.Group>

	{#if estado.errores.mensaje}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{estado.errores.mensaje}</AlertDescription>
		</Alert>
	{/if}

	<Button type="submit" disabled={estado.enviando}>Crear hábito</Button>
</form>
