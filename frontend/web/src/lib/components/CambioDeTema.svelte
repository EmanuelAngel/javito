<script lang="ts">
	import MoonIcon from '@lucide/svelte/icons/moon';
	import SunIcon from '@lucide/svelte/icons/sun';
	import { toggleMode } from 'mode-watcher';
</script>

<!-- Un solo control de tema para todo el shell: vive en el header de (app) y por eso aparece
	en todas las pantallas. ModeWatcher (raíz) es quien aplica la clase .dark. -->
<button
	type="button"
	onclick={toggleMode}
	aria-label="Alternar entre tema claro y oscuro"
	class="relative grid size-9 shrink-0 place-items-center rounded-lg border border-border bg-background text-foreground transition-colors hover:bg-accent"
>
	<SunIcon
		class="col-start-1 row-start-1 size-4 scale-100 rotate-0 transition-transform dark:scale-0 dark:-rotate-90"
	/>
	<MoonIcon
		class="col-start-1 row-start-1 size-4 scale-0 rotate-90 transition-transform dark:scale-100 dark:rotate-0"
	/>
</button>
