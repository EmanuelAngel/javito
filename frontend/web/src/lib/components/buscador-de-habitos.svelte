<script lang="ts">
	import SearchIcon from '@lucide/svelte/icons/search';
	import type { Habito } from '$lib/api/habitos';

	type Props = {
		texto: string;
		resultados: Habito[];
		buscando: boolean;
		mensaje: string;
		alEscribir: (texto: string) => void;
		alAgregar: (habitoId: number) => void;
	};

	let { texto, resultados, buscando, mensaje, alEscribir, alAgregar }: Props = $props();
</script>

<!-- Cada tecla consulta al servidor (HU-RUT-02): el estado «Buscando…» es la prueba de que el pedido
     está en vuelo y no de que se filtró una lista ya bajada. -->
<div class="flex flex-col gap-3 rounded-xl border border-border bg-card p-4">
	<label class="text-sm font-medium" for="buscador-de-habitos">Agregar un hábito</label>

	<div class="relative">
		<SearchIcon
			class="pointer-events-none absolute top-2.5 left-3 size-4 text-muted-foreground"
			aria-hidden="true"
		/>
		<input
			id="buscador-de-habitos"
			type="search"
			autocomplete="off"
			class="w-full rounded-lg border border-border bg-background py-2 pr-3 pl-9 text-sm text-foreground placeholder:text-muted-foreground"
			placeholder="Buscá entre tus hábitos activos"
			value={texto}
			oninput={(evento) => alEscribir(evento.currentTarget.value)}
		/>
	</div>

	{#if buscando}
		<p class="text-xs text-muted-foreground">Buscando…</p>
	{:else if mensaje !== ''}
		<p class="text-xs text-destructive">{mensaje}</p>
	{:else if texto.trim() !== '' && resultados.length === 0}
		<p class="text-xs text-muted-foreground">Ningún hábito activo coincide con «{texto.trim()}».</p>
	{:else if resultados.length > 0}
		<ul class="flex max-h-64 flex-col gap-1 overflow-y-auto">
			{#each resultados as habito (habito.id)}
				<li>
					<button
						type="button"
						class="w-full rounded-lg border border-border bg-background px-3 py-1.5 text-left text-sm transition-colors hover:bg-accent"
						onclick={() => alAgregar(habito.id)}
					>
						{habito.titulo}
					</button>
				</li>
			{/each}
		</ul>
	{/if}
</div>
