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

/** El alto de un hábito. El tipo decide: un `contador` lleva meta y unidad, el resto, null. */
export type DatosDeHabito = {
	titulo: string;
	descripcion: string | null;
	categoriaId: number;
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
