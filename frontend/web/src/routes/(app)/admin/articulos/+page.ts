import type { PageLoad } from './$types';
import { exigirAdministrador } from '$lib/roles';

// SPA sin SSR. Sólo el guard: el listado se pide desde la pantalla, donde se pueden mostrar el
// `Spinner` y el `Alert` mientras viaja (mismo criterio que el catálogo).
export const ssr = false;

export const load: PageLoad = async ({ parent }) => {
	await exigirAdministrador(parent);
};
