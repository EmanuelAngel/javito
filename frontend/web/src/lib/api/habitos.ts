// Módulo de hábitos: sólo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni
// validación de runtime: el contrato lo define la WebAPI, que es nuestra.

import { pedir } from './pedir';

export type TipoDeHabito = 'si_no' | 'contador' | 'abstinencia';

export type FrecuenciaDeHabito = {
	tipo: 'diaria' | 'semanal';
	/** Cuando el tipo es `diaria`, `veces` va null. En `semanal`, es de 1 a 6. */
	veces: number | null;
};

export type Habito = {
	id: number;
	titulo: string;
	descripcion: string | null;
	/** Puede apuntar a una categoría inactiva: sigue cargándose con el nombre completo. */
	categoriaId: number | null;
	tipo: TipoDeHabito;
	meta: number | null;
	unidad: string | null;
	frecuencia: FrecuenciaDeHabito;
	activo: boolean;
};

/** El alta de un hábito. El tipo decide: un `contador` lleva meta y unidad, el resto, null. */
export type DatosDeHabito = {
	titulo: string;
	descripcion: string | null;
	/** `null` sólo al editar un hábito que ya venía sin categoría: el alta exige una. */
	categoriaId: number | null;
	tipo: TipoDeHabito;
	meta: number | null;
	unidad: string | null;
	frecuencia: FrecuenciaDeHabito;
};

export function listar(activo?: boolean): Promise<Habito[]> {
	const query = activo === undefined ? '' : `?activo=${activo}`;
	return pedir<Habito[]>(`/api/habitos${query}`);
}

export function crear(datos: DatosDeHabito): Promise<Habito> {
	return pedir<Habito>('/api/habitos', {
		method: 'POST',
		body: JSON.stringify(datos)
	});
}

/**
 * Corrige un hábito. Los registros no se tocan: la meta nueva rige desde la próxima marca y los días
 * ya cerrados conservan la suya, así que la pantalla no tiene que reenviarlos.
 */
export function editar(id: number, datos: DatosDeHabito): Promise<Habito> {
	return pedir<Habito>(`/api/habitos/${id}`, {
		method: 'PUT',
		body: JSON.stringify(datos)
	});
}

/**
 * Archiva (`false`) o reactiva (`true`) un hábito (HU-HAB-04). Es baja lógica: sus registros
 * quedan, y repetir el estado que ya tiene no es un error.
 */
export function cambiarActivo(id: number, activo: boolean): Promise<Habito> {
	return pedir<Habito>(`/api/habitos/${id}/activo`, {
		method: 'PUT',
		body: JSON.stringify({ activo })
	});
}

/**
 * Busca entre los hábitos activos de quien pregunta, para agregarlos a una rutina (HU-RUT-02).
 * El servidor impone las dos reglas —sólo míos, sólo activos— así que la pantalla no las repite; el
 * `signal` lo usa el buscador para abortar el pedido anterior cuando se sigue escribiendo.
 */
export function buscar(texto: string, signal: AbortSignal): Promise<Habito[]> {
	return pedir<Habito[]>(`/api/habitos?activo=true&q=${encodeURIComponent(texto)}`, { signal });
}
