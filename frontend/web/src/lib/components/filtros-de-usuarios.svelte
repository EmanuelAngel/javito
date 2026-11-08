<script lang="ts">
	import type { Estado } from '$lib/administracion-de-usuarios.svelte';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { Label } from '$lib/components/ui/label/index.js';

	// Los dos filtros del listado: un texto que busca en nombre o email (S3) y el estado (S4). No
	// guardan estado propio: avisan y la pantalla navega, así la URL sigue siendo la única verdad.
	type Props = {
		q: string;
		estado: Estado;
		alBuscar: (q: string) => void;
		alFiltrar: (activo: 'true' | 'false' | null) => void;
	};

	let { q, estado, alBuscar, alFiltrar }: Props = $props();

	const opciones = [
		{ activo: null, filtro: 'todos', rotulo: 'Todos' },
		{ activo: 'true', filtro: 'activos', rotulo: 'Activos' },
		{ activo: 'false', filtro: 'desactivados', rotulo: 'Desactivados' }
	] as const;

	function enviar(evento: SubmitEvent) {
		evento.preventDefault();
		const datos = new FormData(evento.currentTarget as HTMLFormElement);

		alBuscar(String(datos.get('q') ?? '').trim());
	}
</script>

<form class="flex flex-wrap items-end gap-3" onsubmit={enviar}>
	<div class="flex min-w-56 flex-1 flex-col gap-1.5">
		<Label for="buscar-usuarios">Buscar</Label>
		<Input id="buscar-usuarios" name="q" type="search" value={q} placeholder="Nombre o email" />
	</div>
	<Button type="submit" variant="outline">Buscar</Button>
</form>

<div class="flex flex-wrap gap-2" role="group" aria-label="Filtrar por estado">
	{#each opciones as opcion (opcion.rotulo)}
		{@const elegida = estado === opcion.filtro}
		<Button
			type="button"
			variant={elegida ? 'default' : 'outline'}
			size="sm"
			aria-pressed={elegida}
			onclick={() => alFiltrar(opcion.activo)}
		>
			{opcion.rotulo}
		</Button>
	{/each}
</div>
