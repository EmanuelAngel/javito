<script lang="ts">
	import { goto } from '$app/navigation';
	import { sesion } from '$lib/sesion.svelte';
	import { destinoSeguro } from '$lib/navegacion';
	import { API_URL as apiUrl } from '$lib/config';
	import type { PageProps } from './$types';

	type RespuestaDeSesion = {
		token: string;
		usuario: { id: number; nombre: string; email: string; rol: string };
	};

	let { data }: PageProps = $props();

	let email = $state('');
	let password = $state('');
	let enviando = $state(false);
	let mensajeError = $state('');
	let erroresPorCampo = $state<Record<string, string[]>>({});

	// ?destino= lo puso el guard: después de entrar se vuelve ahí. Ya sale validado del load,
	// pero el destino no se usa si no pasa por destinoSeguro.
	let destino = $derived(destinoSeguro(data.destino));

	async function entrar(evento: SubmitEvent) {
		evento.preventDefault();
		enviando = true;
		mensajeError = '';
		erroresPorCampo = {};

		try {
			// fetch crudo, sin pedir: el 401 del login es un error del formulario, no una
			// sesión vencida. Si usara pedir, el deslogueo global se comería el mensaje.
			const respuesta = await fetch(`${apiUrl}/api/sesion`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ email, password })
			});
			const cuerpo = (await respuesta.json().catch(() => null)) as
				| (RespuestaDeSesion & {
						title?: string;
						detail?: string;
						errors?: Record<string, string[]>;
				  })
				| null;

			if (respuesta.ok && cuerpo?.token && cuerpo?.usuario) {
				sesion.iniciar({ token: cuerpo.token, usuario: cuerpo.usuario });
				// El destino lo generó el guard y puede ser cualquier ruta dentro de (app): no hay una
				// estática a resolver, así que la regla `no-navigation-without-resolve` queda desactivada.
				// eslint-disable-next-line svelte/no-navigation-without-resolve
				await goto(destino);
				return;
			}

			if (respuesta.status === 400) {
				erroresPorCampo = cuerpo?.errors ?? {};
				mensajeError = cuerpo?.detail ?? 'Revisá los datos del formulario.';
			} else if (respuesta.status === 401) {
				mensajeError = cuerpo?.detail ?? 'El email o la contraseña no son correctos.';
			} else {
				mensajeError = `La WebAPI respondió ${respuesta.status}.`;
			}
		} catch {
			mensajeError = 'No se pudo contactar a la WebAPI. ¿Está levantada?';
		} finally {
			enviando = false;
		}
	}

	// El backend interpola las claves por clase: Email, Password (convención de MVC).
	function errorDeCampo(campo: 'Email' | 'Password'): string {
		return erroresPorCampo[campo]?.[0] ?? '';
	}
</script>

<svelte:head>
	<title>Javito · Ingresar</title>
</svelte:head>

<main class="mx-auto flex min-h-dvh max-w-sm flex-col justify-center gap-6 px-5">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Javito</h1>
		<p class="text-sm text-muted-foreground">Ingresá con tu cuenta para seguir.</p>
	</header>

	<form class="flex flex-col gap-4 rounded-xl border border-border bg-card p-6" onsubmit={entrar}>
		<label class="flex flex-col gap-1.5">
			<span class="text-sm font-medium">Email</span>
			<input
				type="email"
				bind:value={email}
				required
				autocomplete="email"
				placeholder="usuario@javito.local"
				class="rounded-lg border border-border bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
			/>
			{#if errorDeCampo('Email')}
				<span class="text-sm text-destructive">{errorDeCampo('Email')}</span>
			{/if}
		</label>

		<label class="flex flex-col gap-1.5">
			<span class="text-sm font-medium">Contraseña</span>
			<input
				type="password"
				bind:value={password}
				required
				autocomplete="current-password"
				class="rounded-lg border border-border bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
			/>
			{#if errorDeCampo('Password')}
				<span class="text-sm text-destructive">{errorDeCampo('Password')}</span>
			{/if}
		</label>

		{#if mensajeError}
			<p
				class="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive"
			>
				{mensajeError}
			</p>
		{/if}

		<button
			type="submit"
			disabled={enviando}
			class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-50"
		>
			{enviando ? 'Ingresando…' : 'Entrar'}
		</button>
	</form>
</main>
