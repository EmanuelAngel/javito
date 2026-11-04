<script lang="ts">
	import { goto } from '$app/navigation';
	import CambioDeTema from '$lib/components/cambio-de-tema.svelte';
	import { sesion, type Usuario } from '$lib/sesion.svelte';
	import { destinoSeguro } from '$lib/navegacion';
	import { ErrorDeApi, pedir } from '$lib/api/pedir';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import type { PageProps } from './$types';

	// El tipo del usuario sale de la sesión y no se repite acá: el contrato es uno solo.
	type RespuestaDeSesion = {
		token: string;
		usuario: Usuario;
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
			const iniciada = await pedir<RespuestaDeSesion>('/api/sesion', {
				method: 'POST',
				body: JSON.stringify({ email, password }),
				// El 401 del login es un error del formulario, no una sesión vencida: sin esto, el
				// deslogueo global de `pedir` se comería el mensaje.
				sinSesion: true
			});

			sesion.iniciar({ token: iniciada.token, usuario: iniciada.usuario });
			// El destino lo generó el guard y puede ser cualquier ruta dentro de (app): no hay una
			// estática a resolver, así que la regla `no-navigation-without-resolve` queda desactivada.
			// eslint-disable-next-line svelte/no-navigation-without-resolve
			await goto(destino);
		} catch (error) {
			if (error instanceof ErrorDeApi) {
				// El 400 trae los errores por campo; el resto, un mensaje para el formulario.
				if (error.estado === 400) {
					erroresPorCampo = error.errores ?? {};
					mensajeError = 'Revisá los datos del formulario.';
				} else {
					mensajeError = error.message;
				}
			} else {
				mensajeError = 'No se pudo contactar a la WebAPI. ¿Está levantada?';
			}
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
	<header class="flex items-start justify-between gap-4">
		<div class="flex flex-col gap-1">
			<h1 class="text-3xl font-semibold tracking-tight">Javito</h1>
			<p class="text-sm text-muted-foreground">Ingresá con tu cuenta para seguir.</p>
		</div>
		<!-- El login queda fuera de (app), así que el header del shell no llega: el control de tema
				sube acá para que se pueda cambiar el tema antes de entrar. -->
		<CambioDeTema />
	</header>

	<form class="flex flex-col gap-4 rounded-xl border border-border bg-card p-6" onsubmit={entrar}>
		<Field.Group>
			<Field.Field data-invalid={errorDeCampo('Email') !== ''}>
				<Field.Label for="sesion-email">Email</Field.Label>
				<Input
					id="sesion-email"
					type="email"
					bind:value={email}
					required
					autocomplete="email"
					placeholder="usuario@javito.local"
					aria-invalid={errorDeCampo('Email') !== ''}
				/>
				<Field.Error errors={errorDeCampo('Email') ? [{ message: errorDeCampo('Email') }] : []} />
			</Field.Field>

			<Field.Field data-invalid={errorDeCampo('Password') !== ''}>
				<Field.Label for="sesion-password">Contraseña</Field.Label>
				<Input
					id="sesion-password"
					type="password"
					bind:value={password}
					required
					autocomplete="current-password"
					aria-invalid={errorDeCampo('Password') !== ''}
				/>
				<Field.Error
					errors={errorDeCampo('Password') ? [{ message: errorDeCampo('Password') }] : []}
				/>
			</Field.Field>
		</Field.Group>

		{#if mensajeError}
			<Alert variant="destructive" role="alert">
				<AlertDescription>{mensajeError}</AlertDescription>
			</Alert>
		{/if}

		<Button type="submit" disabled={enviando}>
			{enviando ? 'Ingresando…' : 'Entrar'}
		</Button>
	</form>
</main>
