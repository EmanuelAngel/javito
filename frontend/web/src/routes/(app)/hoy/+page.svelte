<script lang="ts">
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();
</script>

<svelte:head>
	<title>Javito · Hoy</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-4 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Hoy</h1>
	<p class="text-muted-foreground">
		Hola, <strong>{data.usuario.nombre}</strong>. Los hábitos del día llegan con REG-01.
	</p>
</main>
