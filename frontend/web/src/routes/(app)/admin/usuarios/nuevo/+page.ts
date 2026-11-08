import type { PageLoad } from './$types';
import { exigirAdministrador } from '$lib/roles';

// SPA sin SSR y sólo el guard, igual que el listado: la pantalla no necesita datos del servidor
// porque el formulario del alta arranca vacío.
export const ssr = false;

export const load: PageLoad = async ({ parent }) => {
	await exigirAdministrador(parent);
};
