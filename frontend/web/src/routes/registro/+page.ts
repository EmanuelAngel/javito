import { redirect } from '@sveltejs/kit';
import { sesion } from '$lib/sesion.svelte';
import { destinoSeguro } from '$lib/navegacion';
import type { PageLoad } from './$types';

export const ssr = false;

// Con sesión activa el registro no tiene nada que hacer; el destino se valida igual que en el login.
export const load: PageLoad = ({ url }) => {
	const destino = destinoSeguro(url.searchParams.get('destino'));

	if (sesion.token) {
		redirect(302, destino);
	}

	return { destino };
};
