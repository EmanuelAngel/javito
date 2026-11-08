<script lang="ts">
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import Avatar from '$lib/components/avatar.svelte';
	import CambioDeTema from '$lib/components/cambio-de-tema.svelte';
	import { sesion } from '$lib/sesion.svelte';
	import type { LayoutProps } from './$types';

	let { data, children }: LayoutProps = $props();

	// El header lee la sesión y no el `data` del load: subir el avatar en /perfil refresca el estado
	// y la barra se repinta sin recargar ni volver a navegar (HU-PER-02, S5).
	const usuario = $derived(sesion.usuario ?? data.usuario);

	// Redirect centralizado del 401 (S5): si pedir cierra la sesión mientras estás dentro
	// de (app), acá se manda a /login una sola vez, recordando a dónde ibas.
	let volviendoAlLogin = false;
	$effect(() => {
		if (!sesion.actual && !volviendoAlLogin) {
			volviendoAlLogin = true;
			// El destino es la ruta en curso con su query. `resolve()` no aplica: el route id de
			// /login no tiene params dinamicos y la URL viene de la navegacion actual.
			// eslint-disable-next-line svelte/no-navigation-without-resolve
			goto(`/login?destino=${encodeURIComponent(page.url.pathname + page.url.search)}`);
		}
	});

	// Logout local: el token vence solo (8 hs, sin refresh), pero el usuario puede
	// adelantarlo desde acá.
	async function salir() {
		// Se marca antes de limpiar el estado: si no, el effect de arriba ve la sesión vacía y compite
		// con este goto, y el usuario termina en /login?destino=<la pantalla que acaba de dejar>.
		// Salir a propósito tiene que dejar un login limpio (HU-ACC-03: computadora compartida).
		volviendoAlLogin = true;
		sesion.cerrar();
		await goto(resolve('/login'));
	}
</script>

<svelte:head>
	<title>Javito</title>
</svelte:head>

<div class="flex min-h-dvh flex-col">
	<header class="border-b border-border bg-card">
		<div class="mx-auto flex max-w-5xl items-center justify-between gap-4 px-5 py-3">
			<!-- El route id dentro de (app) es /(app)/hoy: como no hay base path, el href va directo
					;y la regla no-navigation-without-resolve queda desactivada -->
			<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
			<a href="/hoy" class="text-lg font-semibold tracking-tight">Javito</a>

			<div class="flex items-center gap-3">
				<a
					href={resolve('/(app)/habitos')}
					class="text-sm text-muted-foreground transition-colors hover:text-foreground"
				>
					Hábitos
				</a>
				<a
					href={resolve('/(app)/rutinas')}
					class="text-sm text-muted-foreground transition-colors hover:text-foreground"
				>
					Rutinas
				</a>
				<a
					href={resolve('/(app)/historial')}
					class="text-sm text-muted-foreground transition-colors hover:text-foreground"
				>
					Historial
				</a>
				<!-- El catálogo lo lee cualquier usuario con sesión: no hay catálogo público, pero
					 tampoco hace falta un rol. -->
				<a
					href={resolve('/(app)/articulos')}
					class="text-sm text-muted-foreground transition-colors hover:text-foreground"
				>
					Artículos
				</a>
				{#if data.usuario.rol === 'Administrador'}
					<!-- Publicar es sólo de Administración. El servidor rechaza igual con 403; esconder el
						 enlace sólo evita ofrecer un camino que termina en un error. -->
					<a
						href={resolve('/(app)/articulos/nuevo')}
						class="text-sm text-muted-foreground transition-colors hover:text-foreground"
					>
						Publicar
					</a>
					<a
						href={resolve('/(app)/admin/categorias')}
						class="text-sm text-muted-foreground transition-colors hover:text-foreground"
					>
						Categorías
					</a>
					<!-- «Artículos» a secas es el catálogo: el rótulo largo evita dos enlaces iguales. -->
					<a
						href={resolve('/(app)/admin/articulos')}
						class="text-sm text-muted-foreground transition-colors hover:text-foreground"
					>
						Administrar artículos
					</a>
				{/if}
				<span class="hidden text-sm text-muted-foreground sm:inline">{usuario.nombre}</span>
				<CambioDeTema />
				<!-- El avatar es el acceso al perfil: es el gesto que la historia promete. -->
				<a
					href={resolve('/(app)/perfil')}
					aria-label="Mi perfil"
					class="rounded-full transition-opacity hover:opacity-80"
				>
					<Avatar nombre={usuario.nombre} avatarUrl={usuario.avatarUrl} />
				</a>
				<button
					type="button"
					onclick={salir}
					class="flex items-center gap-1.5 rounded-lg border border-border bg-background px-3 py-1.5 text-sm text-foreground transition-colors hover:bg-accent"
				>
					<LogOutIcon class="size-4" />
					Salir
				</button>
			</div>
		</div>
	</header>

	{@render children()}
</div>
