<script lang="ts">
	import PlusIcon from '@lucide/svelte/icons/plus';
	import type { Rutina } from '$lib/api/rutinas';

	type Props = {
		rutinas: Rutina[];
		seleccionada: number | null;
		alElegir: (rutinaId: number) => void;
	};

	let { rutinas, seleccionada, alElegir }: Props = $props();

	// Las inactivas van aparte (HU-RUT-05): no se borran, y desde acá se eligen para reactivarlas.
	const activas = $derived(rutinas.filter((rutina) => rutina.activo));
	const inactivas = $derived(rutinas.filter((rutina) => !rutina.activo));
</script>

{#snippet fila(rutina: Rutina)}
	<li>
		<button
			type="button"
			class={[
				'flex w-full flex-col rounded-xl border px-4 py-3 text-left transition-colors',
				rutina.id === seleccionada
					? 'border-primary bg-accent'
					: 'border-border bg-card hover:bg-accent',
				!rutina.activo && 'opacity-70'
			]}
			aria-current={rutina.id === seleccionada ? 'true' : undefined}
			onclick={() => alElegir(rutina.id)}
		>
			<span class="flex items-center gap-2 font-medium">
				{#if rutina.id === seleccionada}
					<PlusIcon class="size-4 rotate-45 text-muted-foreground" />
				{/if}
				{rutina.nombre}
			</span>
			{#if rutina.descripcion}
				<span class="text-sm text-muted-foreground">{rutina.descripcion}</span>
			{/if}
		</button>
	</li>
{/snippet}

<ul class="flex flex-col gap-2">
	{#each activas as rutina (rutina.id)}
		{@render fila(rutina)}
	{/each}
</ul>

{#if inactivas.length > 0}
	<section data-test="rutinas-inactivas" class="flex flex-col gap-2">
		<h2 class="text-sm font-medium text-muted-foreground">Inactivas</h2>
		<ul class="flex flex-col gap-2">
			{#each inactivas as rutina (rutina.id)}
				{@render fila(rutina)}
			{/each}
		</ul>
	</section>
{/if}
