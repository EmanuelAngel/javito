import { redirect } from '@sveltejs/kit';
import { sesion } from '$lib/sesion.svelte';
import type { PageLoad } from './$types';

export const ssr = false;

// Con sesión activa el login no tiene nada que hacer: manda al destino original o a /hoy.
export const load: PageLoad = ({ url }) => {
	if (sesion.token) {
		redirect(302, url.searchParams.get('destino') ?? '/hoy');
	}

	// El formulario lee el destino acá, así la URL es la única fuente de la verdad.
	return { destino: url.searchParams.get('destino') };
};
