import { describe, expect, it } from 'vitest';
import type { FilaDelHistorial } from './api/historial';
import { fechaDelHistorial, filaDelHistorial, filasDelHistorial } from './historial';

// #82, S4: cada fila muestra fecha, hábito, valor alcanzado, meta de ese día y si se completó. La
// proyección se prueba sin montar la pantalla; la tabla sólo pinta lo que sale de acá.

function unaFila(parcial: Partial<FilaDelHistorial> = {}): FilaDelHistorial {
	return {
		fecha: '2001-06-04',
		habitoId: 15,
		habito: 'Leer',
		valor: 4,
		metaObjetivo: 10,
		completado: false,
		...parcial
	};
}

describe('la fila del historial', () => {
	it('lleva las cinco cosas que pide la historia', () => {
		expect(filaDelHistorial(unaFila())).toEqual({
			clave: '2001-06-04/15',
			fecha: 'lunes, 4 de junio de 2001',
			habito: 'Leer',
			valor: '4',
			meta: '10',
			estado: 'Sin cumplir',
			cumplido: false
		});
	});

	it('distingue el cumplido del que no llegó, sin depender del color', () => {
		expect(filaDelHistorial(unaFila({ valor: 10, completado: true }))).toMatchObject({
			estado: 'Cumplido',
			cumplido: true
		});
		expect(filaDelHistorial(unaFila({ valor: 9, completado: false }))).toMatchObject({
			estado: 'Sin cumplir',
			cumplido: false
		});
	});

	it('escribe el valor y la meta como números de acá, y conserva el orden del servidor', () => {
		const filas = filasDelHistorial([
			unaFila({ fecha: '2001-06-06', valor: 2.5, metaObjetivo: 10 }),
			unaFila({ fecha: '2001-06-05', habitoId: 1, habito: 'Caminata', valor: 1, metaObjetivo: 1 })
		]);

		expect(filas.map((fila) => fila.valor)).toEqual(['2,5', '1']);
		expect(filas.map((fila) => fila.meta)).toEqual(['10', '1']);
		expect(filas.map((fila) => fila.fecha)).toEqual([
			fechaDelHistorial('2001-06-06'),
			fechaDelHistorial('2001-06-05')
		]);
		expect(filas.map((fila) => fila.clave)).toEqual(['2001-06-06/15', '2001-06-05/1']);
	});
});
