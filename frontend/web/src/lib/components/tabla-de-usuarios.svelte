<script lang="ts">
	import type { Pagina, UsuarioAdministrado } from '$lib/api/usuarios';
	import Paginador from '$lib/components/paginador.svelte';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import * as Table from '$lib/components/ui/table/index.js';

	// El listado de cuentas del administrador: nombre, email, rol y estado en cada fila, y el estado
	// como badge para distinguir activos de desactivados de un vistazo. No sabe de pedidos: avisa el
	// cambio de página y la pantalla decide.
	type Props = {
		actual: Pagina<UsuarioAdministrado>;
		alCambiarDePagina: (pagina: number) => void;
	};

	let { actual, alCambiarDePagina }: Props = $props();
</script>

<div class="overflow-x-auto rounded-xl border border-border bg-card">
	<Table.Root>
		<Table.Header>
			<Table.Row>
				<Table.Head>Nombre</Table.Head>
				<Table.Head>Email</Table.Head>
				<Table.Head>Rol</Table.Head>
				<Table.Head>Estado</Table.Head>
			</Table.Row>
		</Table.Header>
		<Table.Body>
			{#each actual.elementos as cuenta (cuenta.id)}
				<Table.Row>
					<Table.Cell class="font-medium">{cuenta.nombre}</Table.Cell>
					<Table.Cell class="text-muted-foreground">{cuenta.email}</Table.Cell>
					<Table.Cell>
						<Badge variant="secondary">{cuenta.rol}</Badge>
					</Table.Cell>
					<Table.Cell>
						<Badge variant={cuenta.activo ? 'secondary' : 'outline'}>
							{cuenta.activo ? 'Activo' : 'Inactivo'}
						</Badge>
					</Table.Cell>
				</Table.Row>
			{/each}
		</Table.Body>
	</Table.Root>
</div>

{#if actual.totalDePaginas > 1}
	<Paginador
		pagina={actual.pagina}
		totalDePaginas={actual.totalDePaginas}
		alCambiar={alCambiarDePagina}
		etiqueta="Paginación de usuarios"
	/>
{/if}
