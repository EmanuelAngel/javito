// Lo que el historial muestra de cada registro (#82, S4). Es una función pura a propósito: la fila
// que se pinta se prueba sin montar la pantalla, y la tabla no repite formatos.

import type { FilaDelHistorial } from './api/historial';
import { fechaLocalDesdeIso } from './fechas';

/** Una fila lista para pintar: las cinco cosas que pide la historia, ya escritas para leer. */
export type FilaMostrada = {
	/** La identidad de la marca: un hábito se registra una sola vez por día. */
	clave: string;
	fecha: string;
	habito: string;
	valor: string;
	meta: string;
	/** «Cumplido» o «Sin cumplir»: el estado se lee sin depender del color (como en la semana). */
	estado: string;
	cumplido: boolean;
};

const NUMERO = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 2 });

const FECHA_LARGA = new Intl.DateTimeFormat('es-AR', {
	weekday: 'long',
	day: 'numeric',
	month: 'long',
	year: 'numeric'
});

/** «lunes, 4 de junio de 2001». El año va siempre: un historial cruza años. */
export function fechaDelHistorial(iso: string): string {
	return FECHA_LARGA.format(fechaLocalDesdeIso(iso));
}

export function filaDelHistorial(fila: FilaDelHistorial): FilaMostrada {
	return {
		clave: `${fila.fecha}/${fila.habitoId}`,
		fecha: fechaDelHistorial(fila.fecha),
		habito: fila.habito,
		valor: NUMERO.format(fila.valor),
		meta: NUMERO.format(fila.metaObjetivo),
		estado: fila.completado ? 'Cumplido' : 'Sin cumplir',
		cumplido: fila.completado
	};
}

/** Las filas en el orden en que llegaron: el servidor ya las ordenó por fecha descendente. */
export function filasDelHistorial(filas: FilaDelHistorial[]): FilaMostrada[] {
	return filas.map(filaDelHistorial);
}
