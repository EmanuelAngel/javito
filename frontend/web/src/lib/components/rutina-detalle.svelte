<script lang="ts">
	import TrashIcon from '@lucide/svelte/icons/trash-2';
	import type { RutinaDetalle } from '$lib/api/rutinas';

	type Props = {
		detalle: RutinaDetalle | null;
		alQuitar: (habitoId: number) => void;
	};

	let { detalle, alQuitar }: Props = $props();
</script>

{#if detalle === null}
	<p class="text-sm text-muted-foreground">Elegí una rutina para ver sus hábitos.</p>
{:else}
	<div class="flex flex-col gap-3">
		<div>
			<h2 class="text-xl font-semibold tracking-tight">{detalle.nombre}</h2>
			{#if detalle.descripcion}
				<p class="text-sm text-muted-foreground">{detalle.descripcion}</p>
			{/if}
		</div>

		{#if detalle.habitos.length === 0}
			<p class="text-sm text-muted-foreground">Todavía no tiene hábitos: buscalos abajo.</p>
		{:else}
			<ol class="flex flex-col gap-2">
				{#each detalle.habitos as habito (habito.id)}
					<li
						class="flex items-center justify-between gap-3 rounded-xl border border-border bg-card px-4 py-2"
					>
						<span class="flex items-center gap-3 text-sm">
							<span class="text-xs text-muted-foreground tabular-nums">{habito.orden}</span>
							{habito.titulo}
						</span>
						<button
							type="button"
							class="flex items-center gap-1.5 rounded-lg border border-border bg-background px-2 py-1 text-xs text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
							onclick={() => alQuitar(habito.id)}
						>
							<TrashIcon class="size-3.5" />
							Quitar
						</button>
					</li>
				{/each}
			</ol>
		{/if}
	</div>
{/if}
