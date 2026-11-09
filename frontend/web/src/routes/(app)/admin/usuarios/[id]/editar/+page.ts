import { redirect } from '@sveltejs/kit';
import { resolve } from '$app/paths';
import { ErrorDeApi } from '$lib/api/pedir';
import { obtener } from '$lib/api/usuarios';
import { exigirAdministrador } from '$lib/roles';
import type { PageLoad } from './$types';

// SPA sin SSR: sólo Administración, y la cuenta se pide desde el navegador con el token de la sesión.
export const ssr = false;

export const load: PageLoad = async ({ params, parent }) => {
	await exigirAdministrador(parent);

	const id = Number(params.id);

	// Un id que no es un entero positivo no es una cuenta que pueda existir: se vuelve al listado en
	// vez de pedir `/api/usuarios/abc` y mostrar el error de una URL mal armada.
	if (!Number.isInteger(id) || id <= 0) {
		redirect(302, resolve('/(app)/admin/usuarios'));
	}

	try {
		// El detalle es la única fuente honesta de los datos actuales: el listado devuelve páginas y
		// habría que buscar por texto la fila que ya se sabe cuál es.
		return { cuenta: await obtener(id) };
	} catch (error) {
		// Una cuenta que ya no existe se dice en la pantalla, en vez de confundirse con un fallo de red.
		if (error instanceof ErrorDeApi && error.estado === 404) return { cuenta: null };

		throw error;
	}
};
