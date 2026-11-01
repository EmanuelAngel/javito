import type { PageLoad } from './$types';
import { listar as listarCategorias } from '$lib/api/categorias';
import { listar as listarHabitos } from '$lib/api/habitos';

export const ssr = false;

export const load: PageLoad = async () => {
	// Un solo pedido sin filtro: la separación entre activos y archivados es una lectura del
	// mismo listado (S9) y van juntos en la pantalla, en bloques distintos.
	const [habitos, categorias] = await Promise.all([listarHabitos(), listarCategorias()]);

	return {
		activos: habitos.filter((habito) => habito.activo),
		archivados: habitos.filter((habito) => !habito.activo),
		categorias
	};
};
