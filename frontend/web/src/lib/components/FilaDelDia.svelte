<script lang="ts">
	import type { HabitoDelDia } from '$lib/api/dias';

	let { habito }: { habito: HabitoDelDia } = $props();

	const progreso = $derived(
		habito.frecuencia.tipo === 'semanal'
			? `${habito.semana.cumplidos} de ${habito.semana.objetivo} esta semana`
			: 'Todos los días'
	);

	// En sí/no y abstinencia la meta no se guarda: tildar vale 1.
	const metaDelDia = $derived(habito.meta ?? 1);

	const detalle = $derived(
		habito.tipo === 'contador'
			? `${habito.registro?.valor ?? 0} de ${metaDelDia} ${habito.unidad ?? ''}`.trim()
			: habito.registro
				? 'Hecho'
				: 'Sin hacer'
	);

	const cumplido = $derived(habito.registro?.completado ?? false);
</script>

<li
	class="flex items-center justify-between gap-4 rounded-xl border border-border bg-card px-5 py-4"
>
	<div class="flex min-w-0 flex-col gap-1">
		<span class="truncate font-medium">{habito.titulo}</span>
		<span class="text-sm text-muted-foreground">
			{habito.categoria ?? 'Sin categoría'} · {progreso}
		</span>
	</div>

	<span
		class="shrink-0 rounded-full border border-border px-3 py-1 text-xs font-medium {cumplido
			? 'bg-primary/10 text-primary'
			: 'bg-muted text-muted-foreground'}"
	>
		{detalle}
	</span>
</li>
