import type { PageLoad } from './$types';
import { obtener } from '$lib/api/dias';
import { detalle, listar } from '$lib/api/rutinas';
import { fechaDeHoy } from '$lib/fechas';
import { rutinaDelFiltro, rutinasDelFiltro } from '$lib/filtro-por-rutina';
import { leerEnteroPositivo } from '$lib/paginado.svelte';

export const ssr = false;

// El filtro por rutina (HU-RUT-04) vive en `?rutinaId=` y es sólo una vista: el día llega completo,
// como siempre, y la rutina elegida aporta qué hábitos mostrar y en qué orden. Un id que no es de
// una rutina activa propia se ignora y la pantalla muestra todos.
export const load: PageLoad = async ({ url }) => {
	const [dia, rutinas] = await Promise.all([obtener(fechaDeHoy()), listar()]);
	const activas = rutinasDelFiltro(rutinas);
	const rutinaId = rutinaDelFiltro(leerEnteroPositivo(url.searchParams.get('rutinaId')), activas);

	return {
		dia,
		rutinas: activas,
		rutina: rutinaId === null ? null : await detalle(rutinaId)
	};
};
