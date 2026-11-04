<script lang="ts">
	import { FORMATOS_DEL_AVATAR, TAMANO_MAXIMO_DEL_AVATAR, subirAvatar } from '$lib/api/perfil';
	import { ErrorDeApi } from '$lib/api/pedir';
	import Avatar from '$lib/components/avatar.svelte';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { sesion } from '$lib/sesion.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// La sesión es la única fuente del usuario: el header y esta pantalla leen el mismo estado, así
	// que refrescarlo acá repinta los dos sin recargar (HU-PER-02, S5).
	let usuario = $derived(sesion.actual?.usuario ?? data.usuario);

	let enviando = $state(false);
	let mensajeError = $state('');
	let mensajeOk = $state('');

	async function elegir(evento: Event) {
		const entrada = evento.currentTarget as HTMLInputElement;
		const archivo = entrada.files?.[0];

		// El input se limpia antes de nada: elegir otra vez el mismo archivo tiene que volver a
		// disparar el change, aunque el intento anterior haya fallado.
		entrada.value = '';

		if (!archivo) return;

		mensajeError = '';
		mensajeOk = '';

		// Validación del navegador (la historia pide que el servidor valide **además**): esto sólo
		// evita mandar 20 MB por la red para que vuelvan rechazados.
		if (!FORMATOS_DEL_AVATAR.split(',').includes(archivo.type)) {
			mensajeError = 'Solo se aceptan imágenes JPG, PNG o WebP.';
			return;
		}

		if (archivo.size > TAMANO_MAXIMO_DEL_AVATAR) {
			mensajeError = 'La imagen no puede superar los 5 MB.';
			return;
		}

		enviando = true;

		try {
			sesion.refrescarUsuario(await subirAvatar(archivo));
			mensajeOk = 'Tu avatar ya está actualizado.';
		} catch (error) {
			mensajeError =
				error instanceof ErrorDeApi
					? error.message
					: 'No se pudo contactar a la WebAPI. ¿Está levantada?';
		} finally {
			enviando = false;
		}
	}
</script>

<svelte:head>
	<title>Javito · Perfil</title>
</svelte:head>

<main class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-5 py-10">
	<h1 class="text-3xl font-semibold tracking-tight">Perfil</h1>

	<section
		class="flex flex-col gap-6 rounded-xl border border-border bg-card p-6 sm:flex-row sm:items-center"
	>
		<Avatar nombre={usuario.nombre} avatarUrl={usuario.avatarUrl} tamano="grande" />

		<div class="flex min-w-0 flex-1 flex-col gap-4">
			<div class="flex flex-col gap-1">
				<h2 class="text-lg font-medium">Mi avatar</h2>
				<p class="text-sm text-muted-foreground">
					Es la imagen que te identifica en la barra superior. JPG, PNG o WebP, hasta 5 MB.
				</p>
			</div>

			<Field.Field>
				<Field.Label for="avatar">Cambiar la foto</Field.Label>
				<!--
					<input> nativo y no el Input vendorizado: ese componente enlaza `value` además de `files`,
					y Svelte intenta reescribir el value de un input de archivo, que sólo acepta "". El
					navegador tira InvalidStateError y la pantalla se cae al 500.
				-->
				<input
					id="avatar"
					type="file"
					accept={FORMATOS_DEL_AVATAR}
					disabled={enviando}
					onchange={elegir}
					class="w-full text-sm text-muted-foreground file:mr-3 file:h-8 file:cursor-pointer file:rounded-lg file:border file:border-border file:bg-background file:px-3 file:text-sm file:font-medium file:text-foreground hover:file:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
				/>
				<Field.Description>
					Al subir una nueva, la anterior se borra: no se acumulan.
				</Field.Description>
			</Field.Field>

			{#if enviando}
				<p class="text-sm text-muted-foreground" role="status">Subiendo la imagen…</p>
			{:else if mensajeError}
				<Alert variant="destructive" role="alert">
					<AlertDescription>{mensajeError}</AlertDescription>
				</Alert>
			{:else if mensajeOk}
				<p class="text-sm text-muted-foreground" role="status">{mensajeOk}</p>
			{/if}
		</div>
	</section>
</main>
