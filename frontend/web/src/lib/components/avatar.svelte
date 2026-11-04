<script lang="ts">
	import { urlDeArchivo } from '$lib/config';

	// Un solo componente para las dos apariciones del avatar (la barra y la pantalla de perfil), con
	// el mismo reemplazo cuando la cuenta todavía no subió nada: la inicial del nombre, nunca un
	// ícono genérico ni una imagen rota.
	let {
		nombre,
		avatarUrl,
		tamano = 'chico'
	}: { nombre: string; avatarUrl: string | null; tamano?: 'chico' | 'grande' } = $props();

	const tamanos = {
		chico: 'size-9 text-sm',
		grande: 'size-24 text-3xl'
	} as const;

	// `[...texto]` y no `texto[0]`: con un nombre que empieza en emoji, `[0]` parte el par sustituto.
	const inicial = $derived(([...nombre.trim()][0] ?? '?').toLocaleUpperCase('es'));
	const etiqueta = $derived(`Avatar de ${nombre}`);
</script>

{#if avatarUrl}
	<img
		src={urlDeArchivo(avatarUrl)}
		alt={etiqueta}
		class="shrink-0 rounded-full border border-border object-cover {tamanos[tamano]}"
	/>
{:else}
	<span
		role="img"
		aria-label={etiqueta}
		class="grid shrink-0 place-items-center rounded-full border border-border bg-muted font-semibold text-muted-foreground select-none {tamanos[
			tamano
		]}"
	>
		{inicial}
	</span>
{/if}
