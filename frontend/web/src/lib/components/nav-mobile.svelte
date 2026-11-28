<script lang="ts">
	import type { Component } from 'svelte';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import ClockIcon from '@lucide/svelte/icons/clock';
	import FileTextIcon from '@lucide/svelte/icons/file-text';
	import LayersIcon from '@lucide/svelte/icons/layers';
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import MenuIcon from '@lucide/svelte/icons/menu';
	import NewspaperIcon from '@lucide/svelte/icons/newspaper';
	import PenLineIcon from '@lucide/svelte/icons/pen-line';
	import RepeatIcon from '@lucide/svelte/icons/repeat';
	import SunIcon from '@lucide/svelte/icons/sun';
	import TagIcon from '@lucide/svelte/icons/tag';
	import UsersIcon from '@lucide/svelte/icons/users';
	import UserIcon from '@lucide/svelte/icons/user';
	import CambioDeTema from '$lib/components/cambio-de-tema.svelte';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Separator } from '$lib/components/ui/separator/index.js';
	import * as Sheet from '$lib/components/ui/sheet/index.js';
	import { cn } from '$lib/utils.js';

	let {
		rol,
		alSalir,
		class: className
	}: { rol: string; alSalir: () => void | Promise<void>; class?: string } = $props();

	type Enlace = { titulo: string; href: string; icono: Component };

	// Las rutas van con `resolve` y no como texto suelto: el route id dentro de (app) es el
	// que conoce SvelteKit, y así el href sigue a la ruta si algún día se mueve.
	const enlacesDelUsuario: Enlace[] = [
		// «Hoy» va primero: es la pantalla a la que se vuelve todo el tiempo y en el ancho
		// angosto sólo se llegaba tocando el logo, que no se lee como un enlace.
		{ titulo: 'Hoy', href: resolve('/(app)/hoy'), icono: SunIcon },
		{ titulo: 'Hábitos', href: resolve('/(app)/habitos'), icono: RepeatIcon },
		{ titulo: 'Rutinas', href: resolve('/(app)/rutinas'), icono: LayersIcon },
		{ titulo: 'Historial', href: resolve('/(app)/historial'), icono: ClockIcon },
		// El catálogo lo lee cualquier usuario con sesión: no hay catálogo público, pero
		// tampoco hace falta un rol.
		{ titulo: 'Artículos', href: resolve('/(app)/articulos'), icono: NewspaperIcon },
		{ titulo: 'Perfil', href: resolve('/(app)/perfil'), icono: UserIcon }
	];

	// Lo de Administración va sólo para quien lo es: el servidor rechaza igual con 403,
	// esconder el enlace evita ofrecer un camino que termina en un error.
	const enlacesDeAdministracion: Enlace[] = [
		{ titulo: 'Publicar', href: resolve('/(app)/articulos/nuevo'), icono: PenLineIcon },
		{ titulo: 'Categorías', href: resolve('/(app)/admin/categorias'), icono: TagIcon },
		// «Artículos» a secas es el catálogo: el rótulo largo evita dos enlaces iguales.
		{
			titulo: 'Administrar artículos',
			href: resolve('/(app)/admin/articulos'),
			icono: FileTextIcon
		},
		{ titulo: 'Usuarios', href: resolve('/(app)/admin/usuarios'), icono: UsersIcon }
	];

	const enlaces = $derived(
		rol === 'Administrador' ? [...enlacesDelUsuario, ...enlacesDeAdministracion] : enlacesDelUsuario
	);

	// El enlace activo es el que coincide con el prefijo más largo: en /articulos/nuevo se
	// marca «Publicar» y no además el catálogo, que también lo prefija.
	const hrefActivo = $derived(
		enlaces
			.filter(({ href }) => page.url.pathname === href || page.url.pathname.startsWith(`${href}/`))
			.reduce(
				(masLargo, enlace) => (enlace.href.length > masLargo.length ? enlace.href : masLargo),
				''
			)
	);

	let abierto = $state(false);

	// El drawer se cierra al navegar mirándole la URL, no interceptando cada enlace: así
	// también se cierra si la navegación la dispara otro (un redirect del guard, el logout).
	$effect(() => {
		if (page.url.href) {
			abierto = false;
		}
	});
</script>

<Sheet.Root bind:open={abierto}>
	<Sheet.Trigger>
		{#snippet child({ props })}
			<Button {...props} variant="ghost" size="icon-sm" aria-label="Abrir menú" class={className}>
				<MenuIcon data-icon="inline-start" />
			</Button>
		{/snippet}
	</Sheet.Trigger>

	<Sheet.Content side="right">
		<Sheet.Header>
			<Sheet.Title>Menú</Sheet.Title>
		</Sheet.Header>

		<nav aria-label="Navegación principal" class="flex flex-1 flex-col gap-1 px-2">
			{#each enlaces as { titulo, href, icono: Icono } (href)}
				{@const activo = href === hrefActivo}
				<Button
					{href}
					variant={activo ? 'secondary' : 'ghost'}
					size="lg"
					aria-current={activo ? 'page' : undefined}
					class={cn(
						'h-10 w-full justify-start',
						activo ? 'text-foreground' : 'text-muted-foreground'
					)}
				>
					<Icono data-icon="inline-start" />
					{titulo}
				</Button>
			{/each}
		</nav>

		<Separator />

		<Sheet.Footer class="flex-row items-center">
			<CambioDeTema />
			<Button variant="outline" size="lg" class="h-10 flex-1 justify-start" onclick={alSalir}>
				<LogOutIcon data-icon="inline-start" />
				Salir
			</Button>
		</Sheet.Footer>
	</Sheet.Content>
</Sheet.Root>
