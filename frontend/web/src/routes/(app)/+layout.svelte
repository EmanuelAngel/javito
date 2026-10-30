<script lang="ts">
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import UserIcon from '@lucide/svelte/icons/user';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { sesion } from '$lib/sesion.svelte';
	import type { LayoutProps } from './$types';

	let { data, children }: LayoutProps = $props();

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
				<span class="hidden text-sm text-muted-foreground sm:inline">{data.usuario.nombre}</span>
				<!-- Avatar placeholder: el perfil real entra con el módulo de usuario -->
				<span
					class="grid size-9 place-items-center rounded-full border border-border bg-background text-muted-foreground"
					aria-hidden="true"
				>
					<UserIcon class="size-4" />
				</span>
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
