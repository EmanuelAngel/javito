import { describe, expect, it } from 'vitest';
import { diaLargo, fechaDeHoy, fechaLocalDesdeIso } from './fechas';

describe('fechas', () => {
	it('la fecha de hoy sale del huso del navegador y no de UTC', () => {
		// 1/11 a las 22:30 en Argentina ya es 2/11 en UTC: toISOString() devolvería el día siguiente
		// y la pantalla pediría un día que no es el del usuario.
		const ahora = new Date(2026, 10, 1, 22, 30);

		expect(fechaDeHoy(ahora)).toBe('2026-11-01');
	});

	it('la fecha de hoy rellena con ceros el día y el mes', () => {
		expect(fechaDeHoy(new Date(2026, 0, 5, 9, 0))).toBe('2026-01-05');
	});

	it('un aaaa-mm-dd se interpreta como fecha local, sin corrimiento', () => {
		const fecha = fechaLocalDesdeIso('2026-11-01');

		expect(fecha.getFullYear()).toBe(2026);
		expect(fecha.getMonth()).toBe(10);
		expect(fecha.getDate()).toBe(1);
	});

	it('el día se escribe en castellano y completo', () => {
		const largo = diaLargo('2026-11-01');

		expect(largo).toContain('domingo');
		expect(largo).toContain('noviembre');
	});
});
