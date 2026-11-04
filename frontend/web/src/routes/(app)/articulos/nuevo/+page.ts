import { listar } from '$lib/api/categorias';
import type { PageLoad } from './$types';

export const ssr = false;

export const load: PageLoad = async () => {
	// El desplegable ofrece sólo las activas: una categoría retirada no clasifica nada nuevo
	// (HU-CAT-04), y el servidor rechaza el intento aunque el cliente la mande.
	return { categorias: await listar(true) };
};
