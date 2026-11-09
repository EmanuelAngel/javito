<script lang="ts">
	import { goto } from '$app/navigation';
	import CambioDeTema from '$lib/components/cambio-de-tema.svelte';
	import { sesion } from '$lib/sesion.svelte';
	import { rutaDeLogin } from '$lib/navegacion';
	import {
		CAMPOS_POR_CODIGO_DE_REGISTRO,
		LONGITUD_MINIMA_DE_LA_CONTRASENA,
		registrar
	} from '$lib/api/cuentas';
	import { mapearErroresDeFormulario } from '$lib/api/errores';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let nombre = $state('');
	let email = $state('');
	let password = $state('');
	let enviando = $state(false);
	let mensajeError = $state('');
	let erroresPorCampo = $state<Record<string, string>>({});

	async function crearCuenta(evento: SubmitEvent) {
		evento.preventDefault();
		enviando = true;
		mensajeError = '';
		erroresPorCampo = {};

		try {
			// La respuesta ya es una sesión: se entra sin pasar por el login.
			sesion.iniciar(await registrar({ nombre, email, password }));
			// El destino sale validado del load y puede ser cualquier ruta de (app).
			// eslint-disable-next-line svelte/no-navigation-without-resolve
			await goto(data.destino);
		} catch (error) {
			const { campos, mensaje } = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_REGISTRO);

			erroresPorCampo = campos;
			mensajeError = Object.keys(campos).length > 0 ? 'Revisá los datos del formulario.' : mensaje;
		} finally {
			enviando = false;
		}
	}

	function errores(campo: string) {
		return erroresPorCampo[campo] ? [{ message: erroresPorCampo[campo] }] : [];
	}
</script>

<svelte:head>
	<title>Javito · Crear cuenta</title>
</svelte:head>

<main class="mx-auto flex min-h-dvh max-w-sm flex-col justify-center gap-6 px-5">
	<header class="flex items-start justify-between gap-4">
		<div class="flex flex-col gap-1">
			<h1 class="text-3xl font-semibold tracking-tight">Javito</h1>
			<p class="text-sm text-muted-foreground">Creá tu cuenta para guardar tus hábitos.</p>
		</div>
		<CambioDeTema />
	</header>

	<form
		class="flex flex-col gap-4 rounded-xl border border-border bg-card p-6"
		onsubmit={crearCuenta}
	>
		<Field.Group>
			<Field.Field data-invalid={!!erroresPorCampo.nombre}>
				<Field.Label for="registro-nombre">Nombre</Field.Label>
				<Input
					id="registro-nombre"
					bind:value={nombre}
					required
					maxlength={100}
					autocomplete="name"
					aria-invalid={!!erroresPorCampo.nombre}
				/>
				<Field.Error errors={errores('nombre')} />
			</Field.Field>

			<Field.Field data-invalid={!!erroresPorCampo.email}>
				<Field.Label for="registro-email">Email</Field.Label>
				<Input
					id="registro-email"
					type="email"
					bind:value={email}
					required
					maxlength={150}
					autocomplete="email"
					aria-invalid={!!erroresPorCampo.email}
				/>
				<Field.Error errors={errores('email')} />
			</Field.Field>

			<Field.Field data-invalid={!!erroresPorCampo.password}>
				<Field.Label for="registro-password">Contraseña</Field.Label>
				<Input
					id="registro-password"
					type="password"
					bind:value={password}
					required
					minlength={LONGITUD_MINIMA_DE_LA_CONTRASENA}
					maxlength={72}
					autocomplete="new-password"
					aria-invalid={!!erroresPorCampo.password}
				/>
				<Field.Description>Entre 8 y 72 caracteres.</Field.Description>
				<Field.Error errors={errores('password')} />
			</Field.Field>
		</Field.Group>

		{#if mensajeError}
			<Alert variant="destructive" role="alert">
				<AlertDescription>{mensajeError}</AlertDescription>
			</Alert>
		{/if}

		<Button type="submit" disabled={enviando}>
			{enviando ? 'Creando cuenta…' : 'Crear cuenta'}
		</Button>
	</form>

	<p class="text-center text-sm text-muted-foreground">
		¿Ya tenés cuenta?
		<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
		<a class="text-foreground underline" href={rutaDeLogin(data.destino)}>Ingresá</a>
	</p>
</main>
