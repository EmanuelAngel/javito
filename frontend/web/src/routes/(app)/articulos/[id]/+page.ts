import { obtener } from '$lib/api/articulos';
import { listar } from '$lib/api/categorias';
import { ErrorDeApi } from '$lib/api/pedir';
import type { PageLoad } from './$types';

// SPA sin SSR: el artículo se pide desde el navegador, con el token de la sesión.
export const ssr = false;

export const load: PageLoad = async ({ params }) => {
	const id = Number(params.id);

	// Todas las categorías, no sólo las activas: un artículo publicado conserva la suya aunque
	// después se desactive, y el detalle tiene que poder nombrarla.
	const categorias = await listar();

	if (!Number.isInteger(id) || id <= 0) return { articulo: null, categorias };

	try {
		return { articulo: await obtener(id), categorias };
	} catch (error) {
		// Un artículo inexistente o dado de baja es un 404: la pantalla lo dice en vez de romperse.
		if (error instanceof ErrorDeApi && error.estado === 404) return { articulo: null, categorias };

		throw error;
	}
};
