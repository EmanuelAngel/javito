<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { mapearErroresDeFormulario } from '$lib/api/errores';
	import { CAMPOS_POR_CODIGO_DE_USUARIO, crear, type AltaDeUsuario } from '$lib/api/usuarios';
	import UsuarioForm from '$lib/components/usuario-form.svelte';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';

	// HU-USU-02: el alta de una cuenta desde administración. La pantalla hace el pedido y mapea los
	// errores del servidor; el formulario sólo pinta (y es el mismo que va a usar la edición, #86).
	let enviando = $state(false);
	let mensajeError = $state('');
	let errores = $state<Record<string, string>>({});

	async function guardar(datos: AltaDeUsuario) {
		enviando = true;
		mensajeError = '';
		errores = {};

		try {
			await crear(datos);

			// Vuelve al listado reemplazando la entrada del historial: el formulario de alta no tiene
			// por qué quedar detrás del "atrás" de una cuenta que ya existe. `reset: false` deja el
			// scroll y el foco como estaban.
			await goto(resolve('/(app)/admin/usuarios'), { replace: true, reset: false });
		} catch (error) {
			const mapeado = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_USUARIO);

			errores = mapeado.campos;
			mensajeError = mapeado.mensaje;
		} finally {
			enviando = false;
		}
	}
</script>

<svelte:head>
	<title>Javito · Nuevo usuario</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-2xl flex-col gap-6 px-5 py-10">
	<header class="flex flex-col gap-1">
		<h1 class="text-3xl font-semibold tracking-tight">Nuevo usuario</h1>
		<p class="text-sm text-muted-foreground">
			La cuenta nace activa con la contraseña que escribas acá. Se la comunicás vos: no hay envío de
			emails.
		</p>
	</header>

	{#if mensajeError}
		<Alert variant="destructive" role="alert">
			<AlertDescription>{mensajeError}</AlertDescription>
		</Alert>
	{/if}

	<UsuarioForm esAlta {enviando} {errores} textoDelBoton="Crear usuario" alEnviar={guardar} />

	<Button class="self-start" variant="ghost" href={resolve('/(app)/admin/usuarios')}>
		Volver al listado
	</Button>
</main>
