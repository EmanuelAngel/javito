<script lang="ts">
	import { cambiarActivo, crear, editar, listar, type Categoria } from '$lib/api/categorias';
	import { ErrorDeApi } from '$lib/api/pedir';
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

			<label class="flex flex-col gap-1.5">
				<span class="text-sm font-medium">Nombre</span>
				<input
					type="text"
					bind:value={nombre}
					required
					maxlength="60"
					class="rounded-lg border border-border bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
				/>
				{#if errorNombre}
					<span class="text-sm text-destructive">{errorNombre}</span>
				{/if}
			</label>

			<label class="flex flex-col gap-1.5">
				<span class="text-sm font-medium">Descripción</span>
				<input
					type="text"
					bind:value={descripcion}
					maxlength="255"
					class="rounded-lg border border-border bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
				/>
				{#if errorDescripcion}
					<span class="text-sm text-destructive">{errorDescripcion}</span>
				{/if}
			</label>

			{#if mensajeError}
				<p
					class="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive"
				>
					{mensajeError}
				</p>
			{/if}

			<div class="flex gap-2">
				<button
					type="submit"
					disabled={enviando}
					class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-50"
				>
					{editando ? 'Guardar cambios' : 'Crear categoría'}
				</button>
				{#if editando}
					<button
						type="button"
						onclick={prepararAlta}
						class="rounded-lg border border-border bg-background px-4 py-2 text-sm transition-colors hover:bg-accent"
					>
						Cancelar
					</button>
				{/if}
			</div>
		</form>

		<section class="flex flex-1 flex-col gap-4">
			<label class="flex flex-col gap-1.5">
				<span class="text-sm font-medium">Buscar por nombre</span>
				<input
					type="search"
					bind:value={busqueda}
					placeholder="Filtrar por nombre…"
					class="rounded-lg border border-border bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
				/>
			</label>

			<div class="overflow-x-auto rounded-xl border border-border bg-card">
				<table class="w-full text-sm">
					<thead class="border-b border-border text-left text-muted-foreground">
						<tr>
							<th class="px-4 py-3 font-medium">Nombre</th>
							<th class="px-4 py-3 font-medium">Descripción</th>
							<th class="px-4 py-3 font-medium">Estado</th>
							<th class="px-4 py-3 text-right font-medium">Acciones</th>
						</tr>
					</thead>
					<tbody>
						{#each filtradas as categoria (categoria.id)}
							<tr class="border-b border-border last:border-0">
								<td class="px-4 py-3">{categoria.nombre}</td>
								<td class="px-4 py-3 text-muted-foreground">{categoria.descripcion ?? '—'}</td>
								<td class="px-4 py-3">
									<span
										class="inline-flex rounded-full border border-border px-2 py-0.5 text-xs font-medium {categoria.activo
											? 'bg-primary/10 text-primary'
											: 'bg-muted text-muted-foreground'}"
									>
										{categoria.activo ? 'Activa' : 'Inactiva'}
									</span>
								</td>
								<td class="px-4 py-3">
									<div class="flex justify-end gap-2">
										<button
											type="button"
											onclick={() => prepararEdicion(categoria)}
											class="rounded-lg border border-border bg-background px-3 py-1.5 transition-colors hover:bg-accent"
										>
											Editar
										</button>
										<button
											type="button"
											disabled={enviando}
											onclick={() => alternarActivo(categoria)}
											class="rounded-lg border border-border bg-background px-3 py-1.5 transition-colors hover:bg-accent disabled:opacity-50"
										>
											{categoria.activo ? 'Desactivar' : 'Reactivar'}
										</button>
									</div>
								</td>
							</tr>
						{:else}
							<tr>
								<td colspan="4" class="px-4 py-6 text-center text-muted-foreground">
									No hay categorías que coincidan con la búsqueda.
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</section>
	</div>
</main>
