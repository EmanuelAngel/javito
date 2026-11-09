<script lang="ts">
	import { ROLES, type AltaDeUsuario, type RolDeUsuario } from '$lib/api/usuarios';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Field from '$lib/components/ui/field/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';

	// Formulario de una cuenta (HU-USU-02 y HU-USU-03). El alta lo usa completo; la edición (#86) lo
	// monta con `esAlta` en falso y esconde contraseña y rol: esos datos no se editan en la parte 1
	// (S15) y la contraseña de otro usuario no se muestra ni se puede leer (S11). Los pedidos y el
	// mapeo de errores quedan en la pantalla, así el formulario no sabe de HTTP.
	type Props = {
		/** El alta pide contraseña y rol; la edición sólo nombre y email. */
		esAlta: boolean;
		enviando: boolean;
		/** Mensaje por campo que devolvió el servidor, con la clave del campo del formulario. */
		errores: Record<string, string>;
		textoDelBoton: string;
		/**
		 * Valores con los que arranca el formulario. La edición los precarga con la cuenta que acaba de
		 * leer; el alta arranca vacía y no manda nada.
		 */
		inicial?: { nombre: string; email: string };
		alEnviar: (datos: AltaDeUsuario) => void;
	};

	let { esAlta, enviando, errores, textoDelBoton, inicial, alEnviar }: Props = $props();

	// Los valores son del formulario desde que se monta: no siguen al padre mientras se escribe. Por
	// eso `inicial` se lee una sola vez, como instantánea, y no se vuelve a mirar en cada render: si
	// el padre recargara la cuenta a mitad de la edición, pisaría lo que la persona escribió.
	// svelte-ignore state_referenced_locally
	const valores = $state({
		nombre: inicial?.nombre ?? '',
		email: inicial?.email ?? '',
		password: '',
		rol: 'Usuario' as RolDeUsuario
	});

	function mensajes(campo: string) {
		const mensaje = errores[campo];

		return mensaje ? [{ message: mensaje }] : [];
	}

	function enviar(evento: SubmitEvent) {
		evento.preventDefault();

		// Sale siempre el objeto completo: con `esAlta` en falso, la pantalla de edición manda sólo
		// nombre y email y descarta los otros dos, que no viajan en su contrato.
		alEnviar({
			nombre: valores.nombre,
			email: valores.email,
			password: valores.password,
			rol: valores.rol
		});
	}
</script>

<form class="flex flex-col gap-4 rounded-xl border border-border bg-card p-6" onsubmit={enviar}>
	<Field.Group>
		<Field.Field data-invalid={errores.nombre !== undefined}>
			<Field.Label for="usuario-nombre">Nombre</Field.Label>
			<Input
				id="usuario-nombre"
				bind:value={valores.nombre}
				required
				maxlength={100}
				autocomplete="name"
				aria-invalid={errores.nombre !== undefined}
			/>
			<Field.Error errors={mensajes('nombre')} />
		</Field.Field>

		<Field.Field data-invalid={errores.email !== undefined}>
			<Field.Label for="usuario-email">Email</Field.Label>
			<Input
				id="usuario-email"
				type="email"
				bind:value={valores.email}
				required
				maxlength={150}
				autocomplete="email"
				aria-invalid={errores.email !== undefined}
			/>
			<Field.Error errors={mensajes('email')} />
		</Field.Field>

		{#if esAlta}
			<Field.Field data-invalid={errores.password !== undefined}>
				<Field.Label for="usuario-password">Contraseña</Field.Label>
				<Input
					id="usuario-password"
					type="password"
					bind:value={valores.password}
					required
					minlength={8}
					maxlength={72}
					autocomplete="new-password"
					aria-invalid={errores.password !== undefined}
				/>
				<Field.Description>
					Entre 8 y 72 caracteres. Se la comunicás vos: el sistema no manda emails.
				</Field.Description>
				<Field.Error errors={mensajes('password')} />
			</Field.Field>

			<Field.Field data-invalid={errores.rol !== undefined}>
				<Field.Label for="usuario-rol">Rol</Field.Label>
				<NativeSelect
					id="usuario-rol"
					class="w-full"
					bind:value={valores.rol}
					required
					aria-invalid={errores.rol !== undefined}
				>
					{#each ROLES as rol (rol)}
						<NativeSelectOption value={rol}>{rol}</NativeSelectOption>
					{/each}
				</NativeSelect>
				<Field.Description>
					El Administrador ve y escribe en toda la administración.
				</Field.Description>
				<Field.Error errors={mensajes('rol')} />
			</Field.Field>
		{/if}
	</Field.Group>

	<Button type="submit" disabled={enviando}>{textoDelBoton}</Button>
</form>
