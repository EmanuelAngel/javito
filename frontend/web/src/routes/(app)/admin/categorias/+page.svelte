<script lang="ts">
	import { cambiarActivo, crear, editar, listar, type Categoria } from '$lib/api/categorias';
	import { ErrorDeApi } from '$lib/api/pedir';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as Table from '$lib/components/ui/table/index.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// La lista vive en estado propio: después de cada mutación se vuelve a pedir al servidor
	// para que la tabla refleje lo que quedó en la base, todo por AJAX y sin recargar. Es una
	// instantánea a propósito: el refresh lo maneja refrescar(), no la navegación.
	// svelte-ignore state_referenced_locally
	let categorias = $state<Categoria[]>(data.categorias);

	let busqueda = $state('');

	let enviando = $state(false);
	let idEnEdicion = $state<number | null>(null);
	let nombre = $state('');
	let descripcion = $state('');
	let mensajeError = $state('');
	let errorNombre = $state('');
	let errorDescripcion = $state('');

	let editando = $derived(idEnEdicion !== null);

	// S9: la búsqueda es en el cliente sobre el listado completo, que son pocas y no se pagina.
	const filtradas = $derived.by(() => {
		const aguja = busqueda.trim().toLowerCase();
		if (!aguja) return categorias;
		return categorias.filter((categoria) => categoria.nombre.toLowerCase().includes(aguja));
	});

	function limpiarErrores() {
		mensajeError = '';
		errorNombre = '';
		errorDescripcion = '';
	}

	function prepararAlta() {
		idEnEdicion = null;
		nombre = '';
		descripcion = '';
		limpiarErrores();
	}

	function prepararEdicion(categoria: Categoria) {
		idEnEdicion = categoria.id;
		nombre = categoria.nombre;
		descripcion = categoria.descripcion ?? '';
		limpiarErrores();
	}

	function mostrarError(error: unknown) {
		if (!(error instanceof ErrorDeApi)) {
			mensajeError = 'No se pudo contactar a la WebAPI. ¿Está levantada?';
			return;
		}

		// El 400 trae los errores por campo, con claves en PascalCase (convención de MVC).
		if (error.estado === 400) {
			errorNombre = error.errores?.['Nombre']?.[0] ?? '';
			errorDescripcion = error.errores?.['Descripcion']?.[0] ?? '';
			if (!errorNombre && !errorDescripcion) mensajeError = error.message;
		} else if (error.codigo === 'nombre_repetido') {
			errorNombre = error.message;
		} else {
			mensajeError = error.message;
		}
	}

	async function refrescar() {
		categorias = await listar();
	}

	async function guardar(evento: SubmitEvent) {
		evento.preventDefault();
		enviando = true;
		limpiarErrores();

		// Una descripción vacía viaja como null: para la API es "sin descripción".
		const datos = { nombre, descripcion: descripcion.trim() === '' ? null : descripcion };
		const id = idEnEdicion;

		try {
			if (id !== null) {
				await editar(id, datos);
			} else {
				await crear(datos);
			}

			prepararAlta();
			await refrescar();
		} catch (error) {
			mostrarError(error);
		} finally {
			enviando = false;
		}
	}

	async function alternarActivo(categoria: Categoria) {
		enviando = true;
		limpiarErrores();

		try {
			await cambiarActivo(categoria.id, !categoria.activo);
			await refrescar();
		} catch (error) {
			mostrarError(error);
		} finally {
			enviando = false;
		}
	}
</script>

<svelte:head>
	<title>Javito · Categorías</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Categorías</h1>

	<div class="flex flex-col gap-6 lg:flex-row">
		<form
			class="flex w-full flex-col gap-4 self-start rounded-xl border border-border bg-card p-6 lg:w-80"
			onsubmit={guardar}
		>
			<h2 class="text-lg font-semibold">{editando ? 'Editar categoría' : 'Nueva categoría'}</h2>

			<Field.Group>
				<Field.Field data-invalid={errorNombre !== ''}>
					<Field.Label for="categoria-nombre">Nombre</Field.Label>
					<Input
						id="categoria-nombre"
						bind:value={nombre}
						required
						maxlength={60}
						aria-invalid={errorNombre !== ''}
					/>
					<Field.Error errors={errorNombre ? [{ message: errorNombre }] : []} />
				</Field.Field>

				<Field.Field data-invalid={errorDescripcion !== ''}>
					<Field.Label for="categoria-descripcion">Descripción</Field.Label>
					<Input
						id="categoria-descripcion"
						bind:value={descripcion}
						maxlength={255}
						aria-invalid={errorDescripcion !== ''}
					/>
					<Field.Error errors={errorDescripcion ? [{ message: errorDescripcion }] : []} />
				</Field.Field>
			</Field.Group>

			{#if mensajeError}
				<Alert variant="destructive" role="alert">
					<AlertDescription>{mensajeError}</AlertDescription>
				</Alert>
			{/if}

			<div class="flex gap-2">
				<Button type="submit" disabled={enviando}>
					{editando ? 'Guardar cambios' : 'Crear categoría'}
				</Button>
				{#if editando}
					<Button type="button" variant="outline" onclick={prepararAlta}>Cancelar</Button>
				{/if}
			</div>
		</form>

		<section class="flex flex-1 flex-col gap-4">
			<Field.Field>
				<Field.Label for="buscar">Buscar por nombre</Field.Label>
				<Input id="buscar" type="search" bind:value={busqueda} placeholder="Filtrar por nombre…" />
			</Field.Field>

			{#if filtradas.length === 0}
				<Empty class="rounded-xl border border-border bg-card">
					<EmptyHeader>
						<EmptyTitle>Sin categorías</EmptyTitle>
						<EmptyDescription>No hay categorías que coincidan con la búsqueda.</EmptyDescription>
					</EmptyHeader>
				</Empty>
			{:else}
				<div class="overflow-x-auto rounded-xl border border-border bg-card">
					<Table.Root>
						<Table.Header>
							<Table.Row>
								<Table.Head>Nombre</Table.Head>
								<Table.Head>Descripción</Table.Head>
								<Table.Head>Estado</Table.Head>
								<Table.Head class="text-right">Acciones</Table.Head>
							</Table.Row>
						</Table.Header>
						<Table.Body>
							{#each filtradas as categoria (categoria.id)}
								<Table.Row>
									<Table.Cell>{categoria.nombre}</Table.Cell>
									<Table.Cell class="text-muted-foreground"
										>{categoria.descripcion ?? '—'}</Table.Cell
									>
									<Table.Cell>
										<Badge variant={categoria.activo ? 'secondary' : 'outline'}>
											{categoria.activo ? 'Activa' : 'Inactiva'}
										</Badge>
									</Table.Cell>
									<Table.Cell>
										<div class="flex justify-end gap-2">
											<Button
												type="button"
												variant="outline"
												size="sm"
												onclick={() => prepararEdicion(categoria)}
											>
												Editar
											</Button>
											<Button
												type="button"
												variant="outline"
												size="sm"
												disabled={enviando}
												onclick={() => alternarActivo(categoria)}
											>
												{categoria.activo ? 'Desactivar' : 'Reactivar'}
											</Button>
										</div>
									</Table.Cell>
								</Table.Row>
							{/each}
						</Table.Body>
					</Table.Root>
				</div>
			{/if}
		</section>
	</div>
</main>
