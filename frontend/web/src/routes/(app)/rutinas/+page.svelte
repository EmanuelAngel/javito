<script lang="ts">
	import BuscadorDeHabitos from '$lib/components/buscador-de-habitos.svelte';
	import RutinaDetalle from '$lib/components/rutina-detalle.svelte';
	import RutinasLista from '$lib/components/rutinas-lista.svelte';
	import { estadoDeRutinas } from '$lib/rutinas.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// La pantalla arranca con lo que trajo el load y desde acá el estado lo maneja el módulo de
	// rutinas: crear suma a la lista y crear/elegir cambia el panel de la derecha.
	// svelte-ignore state_referenced_locally
	const rutinas = estadoDeRutinas(data);

	function crear(evento: SubmitEvent): void {
		evento.preventDefault();
		void rutinas.crear();
	}
</script>

<svelte:head>
	<title>Javito · Rutinas</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Rutinas</h1>

	<div class="flex flex-col gap-6 lg:flex-row">
		<section class="flex w-full flex-col gap-4 lg:w-80">
			<form
				class="flex flex-col gap-2 rounded-xl border border-border bg-card p-4"
				onsubmit={crear}
			>
				<h2 class="text-sm font-medium">Nueva rutina</h2>

				<label class="text-xs text-muted-foreground" for="nombre-de-rutina">Nombre</label>
				<input
					id="nombre-de-rutina"
					class="rounded-lg border border-border bg-background px-3 py-2 text-sm"
					maxlength="60"
					required
					bind:value={rutinas.estado.formulario.nombre}
				/>
				{#if rutinas.estado.errores.campos.nombre}
					<p class="text-xs text-destructive">{rutinas.estado.errores.campos.nombre}</p>
				{/if}

				<label class="text-xs text-muted-foreground" for="descripcion-de-rutina">
					Descripción (opcional)
				</label>
				<textarea
					id="descripcion-de-rutina"
					class="rounded-lg border border-border bg-background px-3 py-2 text-sm"
					rows="2"
					maxlength="255"
					bind:value={rutinas.estado.formulario.descripcion}></textarea>

				{#if rutinas.estado.errores.mensaje}
					<p class="text-xs text-destructive">{rutinas.estado.errores.mensaje}</p>
				{/if}

				<button
					type="submit"
					disabled={rutinas.estado.enviando}
					class="rounded-lg bg-primary px-3 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
				>
					Crear rutina
				</button>
			</form>

			{#if rutinas.estado.rutinas.length === 0}
				<p class="text-sm text-muted-foreground">
					Todavía no tenés rutinas. Creá una para agrupar hábitos.
				</p>
			{:else}
				<RutinasLista
					rutinas={rutinas.estado.rutinas}
					seleccionada={rutinas.estado.detalle?.id ?? null}
					alElegir={rutinas.elegir}
				/>
			{/if}
		</section>

		<section class="flex flex-1 flex-col gap-6">
			<RutinaDetalle detalle={rutinas.estado.detalle} alQuitar={rutinas.quitar} />

			{#if rutinas.estado.detalle}
				<BuscadorDeHabitos
					texto={rutinas.estado.busqueda.texto}
					resultados={rutinas.estado.busqueda.resultados}
					buscando={rutinas.estado.busqueda.buscando}
					mensaje={rutinas.estado.busqueda.mensaje}
					alEscribir={rutinas.buscar}
					alAgregar={rutinas.agregar}
				/>
			{/if}
		</section>
	</div>
</main>
