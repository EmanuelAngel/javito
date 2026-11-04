import { describe, expect, it } from 'vitest';
import { estadosDeLaSemana } from './semana';

/** Los siete días tal como los manda la API: de lunes a domingo, sin huecos. */
const dias = (cumplidos: number[]) =>
	[
		'2026-11-02',
		'2026-11-03',
		'2026-11-04',
		'2026-11-05',
		'2026-11-06',
		'2026-11-07',
		'2026-11-08'
	].map((fecha, indice) => ({ fecha, completado: cumplidos.includes(indice) }));

describe('estadosDeLaSemana', () => {
	it('devuelve los siete días en el orden de la API, con la inicial de cada uno', () => {
		const estados = estadosDeLaSemana(dias([0, 1]), '2026-11-03');

		expect(estados.map((estado) => estado.fecha)).toEqual([
			'2026-11-02',
			'2026-11-03',
			'2026-11-04',
			'2026-11-05',
			'2026-11-06',
			'2026-11-07',
			'2026-11-08'
		]);
		expect(estados.map((estado) => estado.inicial)).toEqual(['L', 'M', 'X', 'J', 'V', 'S', 'D']);
	});

	it('marca los días cumplidos y sólo esos', () => {
		const estados = estadosDeLaSemana(dias([0, 2]), '2026-11-06');

		expect(estados.map((estado) => estado.cumplido)).toEqual([
			true,
			false,
			true,
			false,
			false,
			false,
			false
		]);
	});

	it('marca el día pedido como hoy, aunque no tenga marca', () => {
		const estados = estadosDeLaSemana(dias([0]), '2026-11-04');

		expect(estados.filter((estado) => estado.esHoy).map((estado) => estado.fecha)).toEqual([
			'2026-11-04'
		]);
	});

	it('escribe la etiqueta que lee el lector de pantalla, sin depender del color', () => {
		const estados = estadosDeLaSemana(dias([0]), '2026-11-02');

		expect(estados[0].etiqueta).toBe('lunes 2: cumplido, hoy');
		expect(estados[1].etiqueta).toBe('martes 3: sin marcar');
	});
});
