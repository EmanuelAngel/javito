import type { PageLoad } from './$types';
import { listar } from '$lib/api/categorias';

export const ssr = false;

export const load: PageLoad = async () => {
	return { categorias: await listar() };
};
