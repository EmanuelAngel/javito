/**
 * La semana de la tarjeta del día. La API manda los siete días con su marca; acá se les da lo que
 * necesita la pantalla: la inicial, el estado y la etiqueta que lee el lector de pantalla.
 *
 * El estado no se apoya sólo en el color: un día cumplido también lleva la tilde y lo dice su
 * etiqueta, y un día sin marca nunca se pinta en rojo (HU-REG-04): el día por sí solo no sabe si
 * todavía no llegó o si falló.
 */

export type DiaDeLaSemanaApi = {
	fecha: string;
	completado: boolean;
};

export type EstadoDelDia = {
	fecha: string;
	inicial: string;
	cumplido: boolean;
	esHoy: boolean;
	etiqueta: string;
};

/** Las mismas letras que el calendario de pared: la semana empieza el lunes, como la de la API. */
const INICIALES = ['L', 'M', 'X', 'J', 'V', 'S', 'D'] as const;

const NOMBRES = ['lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado', 'domingo'] as const;

export function estadosDeLaSemana(dias: DiaDeLaSemanaApi[], hoy: string): EstadoDelDia[] {
	return dias.map((dia, indice) => {
		const esHoy = dia.fecha === hoy;
		const numero = Number(dia.fecha.slice(8, 10));
		const marca = dia.completado ? 'cumplido' : 'sin marcar';

		return {
			fecha: dia.fecha,
			inicial: INICIALES[indice] ?? '·',
			cumplido: dia.completado,
			esHoy,
			etiqueta: `${NOMBRES[indice] ?? dia.fecha} ${numero}: ${marca}${esHoy ? ', hoy' : ''}`
		};
	});
}
