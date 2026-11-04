import { flushSync, mount } from 'svelte';
import { afterEach, describe, expect, it, vi } from 'vitest';
import Paginador from './paginador.svelte';

// S4/HU-ART-04: el paginador no sabe de artículos —lo van a reusar HIS-01 y USU-01— y sus dos
// reglas son los bordes del recorrido: en la primera no se puede ir para atrás y en la última no
// se puede ir para adelante. La tercera es que quien no ve la pantalla sepa dónde está parado.
describe('paginador', () => {
	let contenedor: HTMLElement | null = null;

	afterEach(() => {
		document.body.innerHTML = '';
		contenedor = null;
	});

	function montar(props: {
		pagina: number;
		totalDePaginas: number;
		alCambiar: (pagina: number) => void;
	}) {
		contenedor = document.createElement('div');
		document.body.append(contenedor);

		mount(Paginador, { target: contenedor, props });
		flushSync();

		return contenedor;
	}

	function boton(raiz: HTMLElement, texto: string) {
		const encontrado = [...raiz.querySelectorAll('button')].find(
			(candidato) => candidato.textContent?.trim() === texto
		);

		if (!encontrado) throw new Error(`No hay un botón «${texto}».`);

		return encontrado;
	}

	it('en la primera página «anterior» está deshabilitado', () => {
		const raiz = montar({ pagina: 1, totalDePaginas: 3, alCambiar: () => {} });

		expect(boton(raiz, 'Anterior').disabled).toBe(true);
		expect(boton(raiz, 'Siguiente').disabled).toBe(false);
	});

	it('en la última página «siguiente» está deshabilitado', () => {
		const raiz = montar({ pagina: 3, totalDePaginas: 3, alCambiar: () => {} });

		expect(boton(raiz, 'Siguiente').disabled).toBe(true);
		expect(boton(raiz, 'Anterior').disabled).toBe(false);
	});

	it('anuncia en qué página está y pide la de al lado', () => {
		const alCambiar = vi.fn();
		const raiz = montar({ pagina: 2, totalDePaginas: 3, alCambiar });

		expect(raiz.textContent).toContain('Página 2 de 3');

		boton(raiz, 'Siguiente').click();
		flushSync();

		expect(alCambiar).toHaveBeenCalledWith(3);
	});
});
