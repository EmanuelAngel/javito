import { listar } from '$lib/api/categorias';
import { exigirAdministrador } from '$lib/roles';
import type { PageLoad } from './$types';

export const ssr = false;

export const load: PageLoad = async ({ parent }) => {
	await exigirAdministrador(parent);
	// El desplegable ofrece sólo las activas: una categoría retirada no clasifica nada nuevo
	// (HU-CAT-04), y el servidor rechaza el intento aunque el cliente la mande.
	return { categorias: await listar(true) };
};
