import { redirect } from '@sveltejs/kit';
import { sesion } from '$lib/sesion.svelte';
import { destinoSeguro } from '$lib/navegacion';
import type { PageLoad } from './$types';

export const ssr = false;

// Con sesión activa el login no tiene nada que hacer: manda al destino original si es una ruta
// interna, o a /hoy si el ?destino= es una URL ajena o algoIrreconocible.
export const load: PageLoad = ({ url }) => {
	if (sesion.token) {
		redirect(302, destinoSeguro(url.searchParams.get('destino')));
	}

	// El formulario lee el destino acá, así la URL es la única fuente de la verdad. Siempre sale
	// validado: el ?destino= crudo puede ser un open redirect (//otro.host).
	return { destino: destinoSeguro(url.searchParams.get('destino')) };
};
