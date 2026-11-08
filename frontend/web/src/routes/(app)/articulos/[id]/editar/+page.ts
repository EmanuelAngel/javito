import { obtener } from '$lib/api/articulos';
import { listar } from '$lib/api/categorias';
import { ErrorDeApi } from '$lib/api/pedir';
import { exigirAdministrador } from '$lib/roles';
import type { PageLoad } from './$types';

export const ssr = false;

export const load: PageLoad = async ({ params, parent }) => {
	await exigirAdministrador(parent);

	const id = Number(params.id);

	if (!Number.isInteger(id) || id <= 0) return { articulo: null, categorias: [] };

	try {
		const [articulo, todas] = await Promise.all([obtener(id), listar()]);

		// El desplegable ofrece las activas y, si la del artículo se desactivó después de publicarlo,
		// también esa: conservarla vale, cambiar a otra exige una activa (S22, como hábitos L4).
		const categorias = todas.filter(
			(categoria) => categoria.activo || categoria.id === articulo.categoriaId
		);

		return { articulo, categorias };
	} catch (error) {
		// Inexistente o dado de baja: un inactivo no se corrige, primero se reactiva (HU-ART-03).
		if (error instanceof ErrorDeApi && error.estado === 404)
			return { articulo: null, categorias: [] };

		throw error;
	}
};
