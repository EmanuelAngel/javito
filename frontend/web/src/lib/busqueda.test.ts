import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { crearBusqueda } from './busqueda';

describe('crearBusqueda', () => {
	beforeEach(() => {
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it('no consulta hasta que pasa la espera', async () => {
		const buscar = vi.fn(async (texto: string) => [texto]);
		const alLlegar = vi.fn();
		const buscador = crearBusqueda({ espera: 250, buscar, alLlegar });

		buscador.pedir('ma');
		await vi.advanceTimersByTimeAsync(249);
		expect(buscar).not.toHaveBeenCalled();

		await vi.advanceTimersByTimeAsync(1);
		expect(buscar).toHaveBeenCalledExactlyOnceWith('ma', expect.any(AbortSignal));
		expect(alLlegar).toHaveBeenCalledWith(['ma']);
	});

	it('una tecla nueva reinicia la espera en vez de sumar un pedido', async () => {
		const buscar = vi.fn(async (texto: string) => [texto]);
		const buscador = crearBusqueda({ espera: 250, buscar, alLlegar: vi.fn() });

		buscador.pedir('m');
		await vi.advanceTimersByTimeAsync(200);
		buscador.pedir('ma');
		await vi.advanceTimersByTimeAsync(200);
		expect(buscar).not.toHaveBeenCalled();

		await vi.advanceTimersByTimeAsync(50);
		expect(buscar).toHaveBeenCalledExactlyOnceWith('ma', expect.any(AbortSignal));
	});

	it('aborta el pedido anterior cuando se sigue escribiendo', async () => {
		const señales: AbortSignal[] = [];
		const buscar = vi.fn((_texto: string, signal: AbortSignal) => {
			señales.push(signal);
			return new Promise<string[]>(() => {});
		});
		const buscador = crearBusqueda({ espera: 250, buscar, alLlegar: vi.fn() });

		buscador.pedir('a');
		await vi.advanceTimersByTimeAsync(250);
		expect(señales[0].aborted).toBe(false);

		buscador.pedir('ab');
		await vi.advanceTimersByTimeAsync(250);

		expect(señales).toHaveLength(2);
		expect(señales[0].aborted).toBe(true);
		expect(señales[1].aborted).toBe(false);
	});

	it('una respuesta vieja no pisa a la nueva', async () => {
		const pendientes: Array<{ texto: string; resolver: (valor: string[]) => void }> = [];
		const buscar = (texto: string) =>
			new Promise<string[]>((resolver) => pendientes.push({ texto, resolver }));
		const alLlegar = vi.fn();
		const buscador = crearBusqueda({ espera: 250, buscar, alLlegar });

		buscador.pedir('a');
		await vi.advanceTimersByTimeAsync(250);
		buscador.pedir('ab');
		await vi.advanceTimersByTimeAsync(250);

		expect(pendientes.map((pedido) => pedido.texto)).toEqual(['a', 'ab']);

		// La nueva contesta primero y la vieja llega tarde: sólo manda la nueva.
		pendientes[1].resolver(['ab']);
		await vi.runAllTimersAsync();
		pendientes[0].resolver(['a']);
		await vi.runAllTimersAsync();

		expect(alLlegar).toHaveBeenCalledTimes(1);
		expect(alLlegar).toHaveBeenCalledWith(['ab']);
	});
});
