<script lang="ts">
	import { onMount } from 'svelte';
	import MoonIcon from '@lucide/svelte/icons/moon';
	import SunIcon from '@lucide/svelte/icons/sun';
	import { toggleMode } from 'mode-watcher';
	import { API_URL as apiUrl } from '$lib/config';

	// Página de humo heredada de la raíz del scaffold: verifica el vínculo SPA-API y ahora
	// vive detrás del guard, dentro de (app). (S9 del odd/tasks/acc-02.md)
	type RespuestaDeSalud = { estado: string; servicio: string };

	let estado = $state<'cargando' | 'conectado' | 'error'>('cargando');
	let servicio = $state('');
	let detalle = $state('');

	async function consultarSalud() {
		estado = 'cargando';

		try {
			const respuesta = await fetch(`${apiUrl}/api/health`);

			if (!respuesta.ok) {
				throw new Error(`la WebAPI respondió ${respuesta.status}`);
			}

			const cuerpo = (await respuesta.json()) as RespuestaDeSalud;
			servicio = cuerpo.servicio;
			estado = 'conectado';
		} catch (error) {
			detalle = error instanceof Error ? error.message : String(error);
			estado = 'error';
		}
	}

	onMount(consultarSalud);
</script>

<svelte:head>
	<title>Javito · vínculo SPA-API</title>
</svelte:head>

<main class="mx-auto flex max-w-2xl flex-col gap-6 px-5 py-10">
	<header class="flex items-start justify-between gap-4">
		<div class="flex flex-col gap-1">
			<h1 class="text-3xl font-semibold tracking-tight">Diagnóstico</h1>
			<p class="text-sm text-muted-foreground">Vínculo entre el SPA y la WebAPI desacoplada.</p>
		</div>
		<button
			type="button"
			onclick={toggleMode}
			aria-label="Alternar entre tema claro y oscuro"
			class="relative grid size-9 shrink-0 place-items-center rounded-lg border border-border bg-card text-foreground hover:bg-accent"
		>
			<SunIcon
				class="col-start-1 row-start-1 size-4 scale-100 rotate-0 transition-transform dark:scale-0 dark:-rotate-90"
			/>
			<MoonIcon
				class="col-start-1 row-start-1 size-4 scale-0 rotate-90 transition-transform dark:scale-100 dark:rotate-0"
			/>
		</button>
	</header>

	<section class="flex flex-col gap-3 rounded-xl border border-border bg-card p-5">
		{#if estado === 'cargando'}
			<p>
				Consultando <code class="rounded bg-muted px-1.5 py-0.5 text-sm">{apiUrl}/api/health</code>…
			</p>
		{:else if estado === 'conectado'}
			<p>
				Conectado a <strong>{servicio}</strong> en
				<code class="rounded bg-muted px-1.5 py-0.5 text-sm">{apiUrl}</code>.
			</p>
			<p class="text-sm text-muted-foreground">
				CORS y desacoplamiento verificados de punta a punta.
			</p>
		{:else}
			<p class="text-destructive">No se pudo consultar la WebAPI: {detalle}</p>
			<p class="text-sm text-muted-foreground">
				Levantá el backend con
				<code class="rounded bg-muted px-1.5 py-0.5 text-sm"
					>dotnet run --project backend/Javito.Api --launch-profile http</code
				>.
			</p>
		{/if}

		<button
			type="button"
			onclick={consultarSalud}
			class="mt-1 w-fit rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90"
		>
			Volver a consultar
		</button>
	</section>
</main>
