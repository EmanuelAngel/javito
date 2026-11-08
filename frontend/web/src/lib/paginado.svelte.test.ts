import { describe, expect, it } from 'vitest';
import type { Pagina } from './api/pagina';
import { armarConsulta, crearPaginado, leerEnteroPositivo, leerPagina } from './paginado.svelte';

// #63: el estado de paginado sale de la pantalla del catálogo y no sabe de artículos. Se prueba sin
// montar nada: leer la URL, armarla de vuelta y la guardia que descarta respuestas viejas.

function una(
	pagina: number,
	elementos: string[],
	totalDeRegistros = elementos.length
): Pagina<string> {
	return {
		elementos,
		pagina,
		tamanioDePagina: 6,
		totalDeRegistros,
		totalDePaginas: Math.max(1, Math.ceil(totalDeRegistros / 6))
	};
}

/** Un pedido que se resuelve cuando la prueba decide: así se ordenan las llegadas a mano. */
function diferido<T>() {
	let resolver!: (valor: T) => void;
	let rechazar!: (error: unknown) => void;
	const promesa = new Promise<T>((ok, mal) => {
		resolver = ok;
		rechazar = mal;
	});

	return { promesa, resolver, rechazar };
}

describe('leer la URL', () => {
	it('lee enteros positivos y descarta lo demás', () => {
		expect(leerEnteroPositivo('3')).toBe(3);
		expect(leerEnteroPositivo(null)).toBeNull();
		expect(leerEnteroPositivo('')).toBeNull();
		expect(leerEnteroPositivo('0')).toBeNull();
		expect(leerEnteroPositivo('-2')).toBeNull();
		expect(leerEnteroPositivo('1.5')).toBeNull();
		expect(leerEnteroPositivo('abc')).toBeNull();
	});

	it('sin una página válida lee la primera', () => {
		expect(leerPagina('4')).toBe(4);
		expect(leerPagina(null)).toBe(1);
		expect(leerPagina('0')).toBe(1);
	});
});

describe('armar la URL', () => {
	it('omite la primera página y los filtros vacíos, y deja la página al final', () => {
		expect(armarConsulta({ categoriaId: null }, 1)).toBe('');
		expect(armarConsulta({ categoriaId: 3 }, 1)).toBe('?categoriaId=3');
		expect(armarConsulta({ categoriaId: null }, 2)).toBe('?pagina=2');
		expect(armarConsulta({ categoriaId: 3 }, 2)).toBe('?categoriaId=3&pagina=2');
		expect(armarConsulta({ q: 'ana gómez', rol: '' }, 1)).toBe('?q=ana%20g%C3%B3mez');
	});
});

describe('crearPaginado', () => {
	it('guarda la página que llegó y deja de cargar', async () => {
		const paginado = crearPaginado<string, null>({
			pedir: async (pagina) => una(pagina, ['a', 'b']),
			mensajeDeError: () => 'falló'
		});

		await paginado.cargar(1, null);

		expect(paginado.estado.pagina?.elementos).toEqual(['a', 'b']);
		expect(paginado.estado.cargando).toBe(false);
		expect(paginado.hayElementos).toBe(true);
		expect(paginado.paginaInexistente).toBe(false);
	});

	it('descarta la respuesta que llega después de un pedido más nuevo', async () => {
		const pedidos = [diferido<Pagina<string>>(), diferido<Pagina<string>>()];
		let llamada = 0;
		const paginado = crearPaginado<string, null>({
			pedir: () => pedidos[llamada++].promesa,
			mensajeDeError: () => 'falló'
		});

		const vieja = paginado.cargar(1, null);
		const nueva = paginado.cargar(2, null);

		pedidos[1].resolver(una(2, ['nueva']));
		await nueva;
		pedidos[0].resolver(una(1, ['vieja']));
		await vieja;

		expect(paginado.estado.pagina?.elementos).toEqual(['nueva']);
		expect(paginado.estado.cargando).toBe(false);
	});

	it('un error viejo no pisa la página nueva, y uno vigente se muestra', async () => {
		const pedidos = [diferido<Pagina<string>>(), diferido<Pagina<string>>()];
		let llamada = 0;
		const paginado = crearPaginado<string, null>({
			pedir: () => pedidos[llamada++].promesa,
			mensajeDeError: (error) => String(error)
		});

		const vieja = paginado.cargar(1, null);
		const nueva = paginado.cargar(2, null);

		pedidos[1].resolver(una(2, ['nueva']));
		await nueva;
		pedidos[0].rechazar('se cayó');
		await vieja;

		expect(paginado.estado.pagina?.elementos).toEqual(['nueva']);
		expect(paginado.estado.mensajeError).toBe('');

		const fallida = crearPaginado<string, null>({
			pedir: () => Promise.reject('sin red'),
			mensajeDeError: (error) => `No se pudo: ${String(error)}`
		});
		await fallida.cargar(1, null);

		expect(fallida.estado.pagina).toBeNull();
		expect(fallida.estado.mensajeError).toBe('No se pudo: sin red');
		expect(fallida.estado.cargando).toBe(false);
	});

	it('pasa la página y los filtros al pedido', async () => {
		const recibidos: [number, { categoriaId: number | null }][] = [];
		const paginado = crearPaginado<string, { categoriaId: number | null }>({
			pedir: async (pagina, filtros) => {
				recibidos.push([pagina, filtros]);
				return una(pagina, ['a']);
			},
			mensajeDeError: () => 'falló'
		});

		await paginado.cargar(3, { categoriaId: 7 });

		expect(recibidos).toEqual([[3, { categoriaId: 7 }]]);
	});

	it('distingue una lista vacía de una página pasada del final', async () => {
		const vacia = crearPaginado<string, null>({
			pedir: async () => una(1, []),
			mensajeDeError: () => 'falló'
		});
		const pasada = crearPaginado<string, null>({
			pedir: async () => una(99, [], 8),
			mensajeDeError: () => 'falló'
		});

		await vacia.cargar(1, null);
		await pasada.cargar(99, null);

		expect(vacia.hayElementos).toBe(false);
		expect(vacia.paginaInexistente).toBe(false);
		expect(pasada.paginaInexistente).toBe(true);
		expect(pasada.ultimaPagina).toBe(2);
	});
});
