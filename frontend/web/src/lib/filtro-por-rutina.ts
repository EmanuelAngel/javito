// Filtrar el día por rutina (HU-RUT-04). Es sólo una vista sobre el día ya cargado: no pide nada ni
// copia hábitos, así que marcar dentro del filtro marca el mismo hábito que se ve sin filtro.

import type { HabitoDelDia } from './api/dias';
import type { Rutina, RutinaDetalle } from './api/rutinas';

/** El selector ofrece sólo las activas: desactivar una rutina la saca de los filtros (HU-RUT-05). */
export function rutinasDelFiltro(rutinas: Rutina[]): Rutina[] {
	return rutinas.filter((rutina) => rutina.activo);
}

/** El id pedido sólo vale si es de una rutina activa propia; si no, la pantalla muestra todos. */
export function rutinaDelFiltro(id: number | null, activas: Rutina[]): number | null {
	return activas.some((rutina) => rutina.id === id) ? id : null;
}

/**
 * Los hábitos del día que están en la rutina, en el orden de la rutina. Un hábito de la rutina que
 * no está en el día (archivado) se descarta.
 */
export function habitosDeLaRutina(habitos: HabitoDelDia[], rutina: RutinaDetalle): HabitoDelDia[] {
	const porId = new Map(habitos.map((habito) => [habito.id, habito]));

	return [...rutina.habitos]
		.sort((a, b) => a.orden - b.orden)
		.flatMap((enRutina) => porId.get(enRutina.id) ?? []);
}
