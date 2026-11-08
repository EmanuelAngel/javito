<script lang="ts">
	import PowerOffIcon from '@lucide/svelte/icons/power-off';
	import { cambiarActivo } from '$lib/api/articulos';
	import { ErrorDeApi } from '$lib/api/pedir';
	import * as AlertDialog from '$lib/components/ui/alert-dialog/index.js';
	import { Alert, AlertDescription } from '$lib/components/ui/alert/index.js';
	import { Button } from '$lib/components/ui/button/index.js';

	// «Desactivar» del detalle (S24): pide confirmación porque el artículo desaparece del catálogo
	// para todos. Reactivarlo es barato (Administración), pero retirarlo por un clic de más no.
	let {
		id,
		alDesactivar
	}: {
		id: number;
		/** Ya está retirado: la pantalla decide a dónde ir (el catálogo, con su query). */
		alDesactivar: () => void;
	} = $props();

	let abierto = $state(false);
	let enviando = $state(false);
	let mensajeError = $state('');

	async function confirmar() {
		enviando = true;
		mensajeError = '';

		try {
			await cambiarActivo(id, false);
			abierto = false;
			alDesactivar();
		} catch (error) {
			// El diálogo se queda abierto: el error se lee en contexto y se puede reintentar.
			mensajeError =
				error instanceof ErrorDeApi ? error.message : 'No se pudo desactivar el artículo.';
		} finally {
			enviando = false;
		}
	}
</script>

<!-- Cerrar y volver a abrir no arrastra el error del intento anterior. -->
<AlertDialog.Root bind:open={abierto} onOpenChange={() => (mensajeError = '')}>
	<AlertDialog.Trigger>
		{#snippet child({ props })}
			<Button {...props} variant="outline" size="sm">
				<PowerOffIcon class="size-4" />
				Desactivar
			</Button>
		{/snippet}
	</AlertDialog.Trigger>
	<AlertDialog.Content>
		<AlertDialog.Header>
			<AlertDialog.Title>¿Desactivar este artículo?</AlertDialog.Title>
			<AlertDialog.Description>
				Deja de aparecer en el catálogo para todos y no se puede abrir por su enlace. No se borra:
				se puede reactivar desde Administración.
			</AlertDialog.Description>
		</AlertDialog.Header>

		{#if mensajeError}
			<Alert variant="destructive" role="alert">
				<AlertDescription>{mensajeError}</AlertDescription>
			</Alert>
		{/if}

		<AlertDialog.Footer>
			<AlertDialog.Cancel disabled={enviando}>Cancelar</AlertDialog.Cancel>
			<!-- Un Button y no `AlertDialog.Action`: la acción cierra el diálogo al hacer clic y, si el
				 pedido falla, tiene que quedar abierto con el error a la vista. -->
			<Button type="button" variant="destructive" disabled={enviando} onclick={confirmar}>
				Desactivar
			</Button>
		</AlertDialog.Footer>
	</AlertDialog.Content>
</AlertDialog.Root>
