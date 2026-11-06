import type { PageLoad } from './$types';
import { listar } from '$lib/api/categorias';
import { exigirAdministrador } from '$lib/roles';

export const ssr = false;

export const load: PageLoad = async ({ parent }) => {
	await exigirAdministrador(parent);
	return { categorias: await listar() };
};
