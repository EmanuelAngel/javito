import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Dia } from './api/dias';
import { estadoDelDia } from './hoy.svelte';

const AVISO = 'No se pudo guardar. Probá de nuevo.';

/** Dos hábitos: uno sí/no sin marcar y un contador en 9 de 10. */
function unDia(): Dia {
	return {
		fecha: '2001-01-01',
		habitos: [
			{
				id: 1,
				titulo: 'Liberación de cuello',
				categoria: 'Cuerpo',
				tipo: 'si_no',
				meta: null,
				unidad: null,
				frecuencia: { tipo: 'diaria', veces: null },
				semana: { cumplidos: 2, objetivo: 7 },
				registro: null
			},
			{
				id: 15,
				titulo: 'Leer',
				categoria: 'Mente',
				tipo: 'contador',
				meta: 10,
				unidad: 'páginas',
				frecuencia: { tipo: 'diaria', veces: null },
				semana: { cumplidos: 0, objetivo: 7 },
				registro: { valor: 9, completado: false }
			}
		]
	};
}

/**
 * Servidor falso: entrega las respuestas de la cola en orden y anota qué se pidió. Si la cola se
 * vacía, falla como fallaría la red.
 */
function servidor(...respuestas: Response[]) {
	const llamadas: string[] = [];
	const falso = vi.fn(async (url: string, opciones?: RequestInit) => {
		llamadas.push(`${opciones?.method ?? 'GET'} ${url}`);
		const siguiente = respuestas.shift();

		if (!siguiente) {
			throw new TypeError('fetch failed');
		}

		return siguiente;
	});

	vi.stubGlobal('fetch', falso);

	return llamadas;
}

function ok(cuerpo: unknown): Response {
	return new Response(JSON.stringify(cuerpo), {
		status: 200,
		headers: { 'Content-Type': 'application/json' }
	});
}

describe('estado del día', () => {
	afterEach(() => vi.unstubAllGlobals());

	it('marcar pinta la fila antes de que responda la red', async () => {
		const llamadas = servidor(ok({ valor: 1, metaObjetivo: 1, completado: true }));
		const estado = estadoDelDia(unDia());
		const habito = estado.dia.habitos[0];

		const pendiente = estado.alternar(habito);

		// Sin esperar: el toque ya se ve, que es lo que pide REG-01.
		expect(estado.dia.habitos[0].registro).toEqual({ valor: 1, completado: true });
		expect(estado.dia.habitos[0].semana.cumplidos).toBe(3);

		await pendiente;

		expect(llamadas).toEqual(['PUT http://localhost:5256/api/habitos/1/registros/2001-01-01']);
		expect(estado.aviso).toBe('');
	});

	it('si el pedido falla, vuelve a lo que dice el servidor y avisa', async () => {
		servidor(new Response('', { status: 500 }), ok(unDia()));
		const estado = estadoDelDia(unDia());

		await estado.alternar(estado.dia.habitos[0]);

		expect(estado.dia.habitos[0].registro).toBeNull();
		expect(estado.dia.habitos[0].semana.cumplidos).toBe(2);
		expect(estado.aviso).toBe(AVISO);
	});

	it('si tampoco se puede releer el día, vuelve a la foto anterior al toque', async () => {
		servidor(new Response('', { status: 500 }));
		const estado = estadoDelDia(unDia());

		await estado.alternar(estado.dia.habitos[0]);

		expect(estado.dia.habitos[0].registro).toBeNull();
		expect(estado.aviso).toBe(AVISO);
	});

	it('subir el contador hasta la meta lo completa y suma la semana', async () => {
		servidor(ok({ valor: 10, metaObjetivo: 10, completado: true }));
		const estado = estadoDelDia(unDia());

		await estado.subir(estado.dia.habitos[1]);

		expect(estado.dia.habitos[1].registro).toEqual({ valor: 10, completado: true });
		expect(estado.dia.habitos[1].semana.cumplidos).toBe(1);
	});

	it('bajar en cero no gasta un pedido ni escribe una marca', async () => {
		const llamadas = servidor();
		const estado = estadoDelDia({
			...unDia(),
			habitos: [{ ...unDia().habitos[1], registro: { valor: 0, completado: false } }]
		});

		await estado.bajar(estado.dia.habitos[0]);

		expect(llamadas).toEqual([]);
		expect(estado.dia.habitos[0].registro).toEqual({ valor: 0, completado: false });
	});
});
