// Módulo del historial: sólo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni
// validación de runtime: el contrato lo define la WebAPI, que es nuestra.

import type { FrecuenciaDeHabito, TipoDeHabito } from './habitos';
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

/**
 * El estado de un día del detalle (HU-HIS-02, L6): cumplido si su registro se completó, vacío si es
 * un día evaluable de un hábito diario sin marca, y neutro si el día queda fuera del período
 * evaluable —anterior al primer registro— o si es un semanal, cuya cuota no es de todos los días.
 */
export type EstadoDelDiaDelHistorial = 'cumplido' | 'vacio' | 'neutro';

/** Un día de la cuadrícula: la fecha y su estado, que es lo único que pinta el cliente. */
export type DiaDelHistorial = {
	/** aaaa-mm-dd. */
	fecha: string;
	estado: EstadoDelDiaDelHistorial;
};

/**
 * El detalle histórico de un hábito (HU-HIS-02, L8): la ventana elegida, sus totales y el día por
 * día. `cumplidos` y `evaluables` siguen la unidad del hábito —días en un diario, semanas en un
 * semanal—, que es la misma del porcentaje (L4).
 */
export type DetalleDelHistorial = {
	habitoId: number;
	habito: string;
	tipo: TipoDeHabito;
	frecuencia: FrecuenciaDeHabito;
	/** El largo de la ventana: 30, 60 o 365 días. */
	dias: number;
	/** Primer día de la ventana, aaaa-mm-dd. */
	desde: string;
	/** Último día de la ventana, aaaa-mm-dd. */
	hasta: string;
	cumplidos: number;
	evaluables: number;
	/** Con un decimal, sobre `evaluables`. Cero cuando no hay nada evaluable (L5). */
	porcentaje: number;
	cuadricula: DiaDelHistorial[];
};

/**
 * El detalle de un hábito propio en el período elegido. `dias` es 30, 60 o 365 —el servidor rechaza
 * cualquier otro— y `hasta` es el último día incluido, que manda el navegador porque el servidor
 * corre en UTC y el día lo sabe quien mira la pantalla.
 *
 * Un hábito ajeno o inexistente llega como el mismo 404 `no_encontrado`: la pantalla muestra su
 * mensaje y no inventa otro.
 */
export function detalle(id: number, dias: number, hasta: string): Promise<DetalleDelHistorial> {
	const parametros = new URLSearchParams({ dias: String(dias), hasta });

	return pedir<DetalleDelHistorial>(`/api/habitos/${id}/historial?${parametros}`);
}
