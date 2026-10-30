import { redirect } from '@sveltejs/kit';
import type { Usuario } from '$lib/sesion.svelte';
import { pedir } from '$lib/api/pedir';
import { sesion } from '$lib/sesion.svelte';
import type { LayoutLoad } from './$types';

// Guard de todo el grupo (app): sin token, manda a /login recordando el destino original
// (?destino=, S10); con token, lo valida contra la fila actual (/yo) y expone el usuario
// fresco a las páginas del grupo (S2).
export const load: LayoutLoad = async ({ url }) => {
	if (!sesion.token) {
		redirect(302, `/login?destino=${encodeURIComponent(url.pathname + url.search)}`);
	}

	try {
		const usuario = await pedir<Usuario>('/api/sesion/yo');
		sesion.refrescarUsuario(usuario);
		return { usuario };
	} catch {
		// Token inválido o WebAPI caída: por S14, el 401 desloguea (pedir ya lo hizo) y
		// acá limpiamos por si el error fue otro, para no dejar sesión a medias.
		sesion.cerrar();
		redirect(302, `/login?destino=${encodeURIComponent(url.pathname + url.search)}`);
	}
};
