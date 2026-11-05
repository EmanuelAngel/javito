import type { PageLoad } from './$types';
import { detalle as detalleDeRutina, listar as listarRutinas } from '$lib/api/rutinas';

export const ssr = false;

export const load: PageLoad = async () => {
	const rutinas = await listarRutinas();

	// Se abre la primera con sus hábitos: refrescar la pantalla no tiene que dejar el panel vacío.
	const detalle = rutinas.length > 0 ? await detalleDeRutina(rutinas[0].id) : null;

	return { rutinas, detalle };
};
