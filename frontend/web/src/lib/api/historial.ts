// Módulo del historial: sólo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni
// validación de runtime: el contrato lo define la WebAPI, que es nuestra.

import type { Pagina } from './pagina';
import { pedir } from './pedir';

export type { Pagina } from './pagina';

/**
 * Una fila del historial (HU-HIS-01): la fecha, el hábito, el valor alcanzado, la meta con la que se
 * cerró ese día y si se completó. Las notas todavía no viajan: las escribe #74.
 */
export type FilaDelHistorial = {
	/** aaaa-mm-dd: el servidor no manda hora porque ninguna pantalla la muestra. */
	fecha: string;
	habitoId: number;
	habito: string;
	valor: number;
	/** La meta de ese día, no la vigente: editar el hábito no reescribe el pasado. */
	metaObjetivo: number;
	completado: boolean;
};

/**
 * Lo que se le puede pedir al historial. Todo opcional: sin nada, el servidor decide (página 1, seis
 * por página, todos los hábitos y todas las fechas). Un filtro ausente no viaja: el default vive en
 * la WebAPI, no duplicado acá.
 */
export type FiltrosDelHistorial = {
	habitoId?: number | null;
	/** Primer día incluido, aaaa-mm-dd. */
	desde?: string | null;
	/** Último día incluido, aaaa-mm-dd. */
	hasta?: string | null;
	tamanioDePagina?: number;
};

/**
 * El historial paginado en el servidor, del día más nuevo al más viejo (HU-HIS-01). Una página
 * pasada del final vuelve vacía con la metadata tal como se pidió: corregirla en silencio escondería
 * que quien la pidió se equivocó.
 */
export function listar(
	pagina: number,
	filtros: FiltrosDelHistorial = {}
): Promise<Pagina<FilaDelHistorial>> {
	const parametros = new URLSearchParams();

	parametros.set('pagina', String(pagina));
	if (filtros.tamanioDePagina !== undefined) {
		parametros.set('tamanioDePagina', String(filtros.tamanioDePagina));
	}
	if (filtros.habitoId !== undefined && filtros.habitoId !== null) {
		parametros.set('habitoId', String(filtros.habitoId));
	}
	if (filtros.desde) parametros.set('desde', filtros.desde);
	if (filtros.hasta) parametros.set('hasta', filtros.hasta);

	return pedir<Pagina<FilaDelHistorial>>(`/api/historial?${parametros}`);
}
