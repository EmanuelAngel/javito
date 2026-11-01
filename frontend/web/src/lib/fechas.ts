/**
 * Fechas del SPA. La API pide un aaaa-mm-dd y no sabe en qué huso horario vive quien mira la
 * pantalla, así que el día lo resuelve el navegador.
 */

/**
 * El día de hoy en el huso del navegador, en el formato que espera la API (aaaa-mm-dd).
 * No se usa `toISOString()`: devuelve UTC, y a las 22 de Argentina ya sería el día siguiente.
 */
export function fechaDeHoy(ahora: Date = new Date()): string {
	const mes = String(ahora.getMonth() + 1).padStart(2, '0');
	const dia = String(ahora.getDate()).padStart(2, '0');

	return `${ahora.getFullYear()}-${mes}-${dia}`;
}

/**
 * Interpreta un aaaa-mm-dd como fecha local. `new Date('2026-11-01')` lo lee como UTC y en Argentina
 * cae el 31 de octubre: el corrimiento clásico al mostrar fechas que sólo son días.
 */
export function fechaLocalDesdeIso(iso: string): Date {
	const [anio, mes, dia] = iso.split('-').map(Number);

	return new Date(anio, mes - 1, dia);
}

/** El día escrito para leer: «domingo, 1 de noviembre». */
export function diaLargo(iso: string): string {
	return new Intl.DateTimeFormat('es-AR', {
		weekday: 'long',
		day: 'numeric',
		month: 'long'
	}).format(fechaLocalDesdeIso(iso));
}
