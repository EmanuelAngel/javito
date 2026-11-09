<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { CAMPOS_POR_CODIGO_DE_USUARIO, editar } from '$lib/api/usuarios';
	import { mapearErroresDeFormulario } from '$lib/api/errores';
	import UsuarioForm from '$lib/components/usuario-form.svelte';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Empty,
		EmptyDescription,
		EmptyHeader,
		EmptyTitle
	} from '$lib/components/ui/empty/index.js';
	import type { PageProps } from './$types';
	import type { AltaDeUsuario } from '$lib/api/usuarios';

	// HU-USU-03: el mismo formulario del alta, precargado y sin contraseña ni rol. Edita los datos de
	// la cuenta —nombre y email—; el rol y el estado son de la parte 2 (S15) y la contraseña de otro
	// usuario no se muestra ni se escribe (S11).
	let { data }: PageProps = $props();

	let enviando = $state(false);
	let mensajeError = $state('');
	let errores = $state<Record<string, string>>({});

	async function guardar(id: number, datos: { nombre: string; email: string }) {
		enviando = true;
		mensajeError = '';
		errores = {};

		try {
			await editar(id, datos);

			// Vuelve al listado reemplazando la entrada del historial: el formulario ya no tiene nada
			// que hacer detrás del "atrás" de una cuenta que se guardó. `reset: false` deja el scroll
			// y el foco como estaban.
			await goto(resolve('/(app)/admin/usuarios'), { replace: true, reset: false });
		} catch (error) {
			// `email_repetido` cae en el campo del email: es un error del dato, no del formulario.
			const mapeado = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_USUARIO);

			errores = mapeado.campos;
			mensajeError = mapeado.mensaje;
		} finally {
			enviando = false;
		}
	}
</script>

<svelte:head>
	<title>Javito · Editar usuario</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-2xl flex-col gap-6 px-5 py-10">
	{#if data.cuenta === null}
		<Empty class="rounded-xl border border-border bg-card">
			<EmptyHeader>
				<EmptyTitle>Esta cuenta no existe</EmptyTitle>
				<EmptyDescription>
					Puede que la hayan borrado o que el enlace esté mal. Volvé al listado y elegí la cuenta de
					nuevo.
				</EmptyDescription>
			</EmptyHeader>
		</Empty>

		<Button class="self-start" variant="ghost" href={resolve('/(app)/admin/usuarios')}>
			Volver al listado
		</Button>
	{:else}
		{@const cuenta = data.cuenta}

		<header class="flex flex-col gap-1">
			<h1 class="text-3xl font-semibold tracking-tight">Editar usuario</h1>
			<p class="text-sm text-muted-foreground">
				Corregí el nombre y el email de la cuenta. El rol, el estado y la contraseña no se editan
				acá.
			</p>
		</header>

		{#if mensajeError}
			<Alert variant="destructive" role="alert">
				<AlertDescription>{mensajeError}</AlertDescription>
			</Alert>
		{/if}

		<UsuarioForm
			esAlta={false}
			inicial={cuenta}
			{enviando}
			{errores}
			textoDelBoton="Guardar cambios"
			alEnviar={(datos: AltaDeUsuario) =>
				void guardar(cuenta.id, { nombre: datos.nombre, email: datos.email })}
		/>

		<Button class="self-start" variant="ghost" href={resolve('/(app)/admin/usuarios')}>
			Volver al listado
		</Button>
	{/if}
</main>
