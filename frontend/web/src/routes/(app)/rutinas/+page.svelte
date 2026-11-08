<script lang="ts">
	import BuscadorDeHabitos from '$lib/components/buscador-de-habitos.svelte';
	import RutinaDetalle from '$lib/components/rutina-detalle.svelte';
	import RutinaForm from '$lib/components/rutina-form.svelte';
	import RutinasLista from '$lib/components/rutinas-lista.svelte';
	import { estadoDeRutinas } from '$lib/rutinas.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// La pantalla arranca con lo que trajo el load y desde acá el estado lo maneja el módulo de
	// rutinas: crear suma a la lista, crear/elegir cambia el panel de la derecha y editar o desactivar
	// actualizan los dos.
	// svelte-ignore state_referenced_locally
	const rutinas = estadoDeRutinas(data);
</script>

<svelte:head>
	<title>Javito · Rutinas</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Rutinas</h1>

	<div class="flex flex-col gap-6 lg:flex-row">
		<section class="flex w-full flex-col gap-4 lg:w-80">
			<RutinaForm {rutinas} />

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
			<RutinaDetalle
				detalle={rutinas.estado.detalle}
				enviando={rutinas.estado.enviando}
				alQuitar={rutinas.quitar}
				alEditar={rutinas.prepararEdicion}
				alCambiarActivo={rutinas.cambiarActivo}
			/>

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
