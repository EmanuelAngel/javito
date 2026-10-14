<script lang="ts">
	import { onMount } from 'svelte';
	import { API_URL as apiUrl } from '../lib/config';

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

<main>
	<h1>Javito</h1>
	<p class="subtitulo">Vínculo entre el SPA y la WebAPI desacoplada.</p>

	<section>
		{#if estado === 'cargando'}
			<p>Consultando <code>{apiUrl}/api/health</code>…</p>
		{:else if estado === 'conectado'}
			<p class="ok">Conectado a <strong>{servicio}</strong> en <code>{apiUrl}</code>.</p>
			<p class="nota">CORS y desacoplamiento verificados de punta a punta.</p>
		{:else}
			<p class="error">No se pudo consultar la WebAPI: {detalle}</p>
			<p class="nota">
				Levantá el backend con
				<code>dotnet run --project backend/Javito.Api --launch-profile http</code>.
			</p>
		{/if}

		<button onclick={consultarSalud}>Volver a consultar</button>
	</section>
</main>

<style>
	main {
		max-width: 32rem;
		margin: 0 auto;
		padding: 2rem 1.25rem;
		font-family: system-ui, sans-serif;
		line-height: 1.5;
	}

	h1 {
		margin: 0;
		font-size: 1.75rem;
	}

	.subtitulo {
		margin-top: 0.25rem;
		color: #52525b;
	}

	section {
		margin-top: 1.5rem;
		padding: 1.25rem;
		border: 1px solid #e4e4e7;
		border-radius: 0.75rem;
		background: #fff;
		box-shadow: 0 1px 2px rgb(0 0 0 / 5%);
	}

	.ok {
		color: #15803d;
	}

	.error {
		color: #b91c1c;
	}

	.nota {
		color: #52525b;
		font-size: 0.875rem;
	}

	code {
		padding: 0.125rem 0.375rem;
		border-radius: 0.25rem;
		background: #f4f4f5;
		font-size: 0.875em;
	}

	button {
		margin-top: 0.5rem;
		padding: 0.5rem 1rem;
		border: 1px solid #d4d4d8;
		border-radius: 0.5rem;
		background: #fafafa;
		font: inherit;
		cursor: pointer;
	}
</style>
