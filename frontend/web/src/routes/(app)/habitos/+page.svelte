<script lang="ts">
	import HabitoForm from '$lib/components/HabitoForm.svelte';
	import HabitosLista from '$lib/components/HabitosLista.svelte';
	import { estadoDeHabitos } from '$lib/habitos.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// La pantalla arranca con lo que trajo el load y desde acá el estado lo maneja el módulo de
	// hábitos: el alta suma a la lista y los errores por campo vuelven al formulario.
	// svelte-ignore state_referenced_locally
	const habitos = estadoDeHabitos(data);
</script>

<svelte:head>
	<title>Javito · Hábitos</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Hábitos</h1>

	<div class="flex flex-col gap-6 lg:flex-row">
		<HabitoForm estado={habitos.estado} crear={habitos.crear} />

		<HabitosLista
			activos={habitos.estado.activos}
			archivados={habitos.estado.archivados}
			categorias={habitos.estado.categorias}
		/>
	</div>
</main>
