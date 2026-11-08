import { describe, expect, it } from 'vitest';
import type { HabitoDelDia } from './api/dias';
import type { Rutina, RutinaDetalle } from './api/rutinas';
import { habitosDeLaRutina, rutinaDelFiltro, rutinasDelFiltro } from './filtro-por-rutina';

function rutina(id: number, activo: boolean): Rutina {
	return { id, nombre: `Rutina ${id}`, descripcion: null, activo };
}

// Sólo importa el id: el filtro no mira nada más del hábito.
function habito(id: number): HabitoDelDia {
	return { id } as HabitoDelDia;
}

describe('filtrar el día por rutina (HU-RUT-04)', () => {
	it('ofrece sólo las rutinas activas, en el orden en que llegan', () => {
		const rutinas = [rutina(1, true), rutina(4, false), rutina(2, true)];

		expect(rutinasDelFiltro(rutinas).map((r) => r.id)).toEqual([1, 2]);
	});

	it('elige la rutina sólo si es una de las activas; si no, muestra todos', () => {
		const activas = [rutina(1, true), rutina(2, true)];

		expect(rutinaDelFiltro(2, activas)).toBe(2);
		expect(rutinaDelFiltro(null, activas)).toBeNull();
		// Inactiva, ajena o inexistente: ninguna está entre las activas propias.
		expect(rutinaDelFiltro(4, activas)).toBeNull();
		expect(rutinaDelFiltro(99, activas)).toBeNull();
	});

	it('muestra sólo los hábitos de la rutina, en el orden de la rutina y no en el del día', () => {
		const dia = [habito(1), habito(2), habito(5), habito(9), habito(11)];
		const manana: RutinaDetalle = {
			...rutina(1, true),
			habitos: [
				{ id: 9, titulo: 'Hidratación', orden: 1 },
				{ id: 1, titulo: 'Leer', orden: 2 },
				{ id: 5, titulo: 'Meditar', orden: 4 },
				{ id: 11, titulo: 'Estirar', orden: 3 }
			]
		};

		expect(habitosDeLaRutina(dia, manana).map((h) => h.id)).toEqual([9, 1, 11, 5]);
	});

	it('devuelve los mismos objetos del día: marcar en el filtro marca el hábito real', () => {
		const dia = [habito(1), habito(9)];
		const rutinaDe: RutinaDetalle = {
			...rutina(1, true),
			habitos: [{ id: 9, titulo: 'Hidratación', orden: 1 }]
		};

		expect(habitosDeLaRutina(dia, rutinaDe)[0]).toBe(dia[1]);
	});

	it('descarta los hábitos de la rutina que no están en el día', () => {
		const rutinaDe: RutinaDetalle = {
			...rutina(1, true),
			habitos: [{ id: 17, titulo: 'Correr', orden: 1 }]
		};

		expect(habitosDeLaRutina([habito(1)], rutinaDe)).toEqual([]);
	});
});
