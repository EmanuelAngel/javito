import { flushSync, mount } from 'svelte';
import { afterEach, describe, expect, it } from 'vitest';
import type { ArticuloDelCatalogo } from '$lib/api/articulos';
import TarjetaDeArticulo from './tarjeta-de-articulo.svelte';

// HU-ART-04: la tarjeta es lo que ve el lector de cada artículo del catálogo, y tiene que mostrar
// los cinco datos de la historia —portada, título, resumen, categoría y fecha—. La portada es
// opcional (los artículos semilla no tienen), así que su ausencia también es una regla.
describe('tarjeta de artículo', () => {
	let contenedor: HTMLElement | null = null;

	afterEach(() => {
		document.body.innerHTML = '';
		contenedor = null;
	});

	const articulo: ArticuloDelCatalogo = {
		id: 3,
		categoriaId: 2,
		titulo: 'Tortilla de papa',
		resumen: 'La receta de la abuela, sin vueltas.',
		imagenPortadaUrl: null,
		fechaPublicacion: '2026-10-30',
		activo: true
	};

	function montar(props: { articulo: ArticuloDelCatalogo; categoria: string }) {
		contenedor = document.createElement('div');
		document.body.append(contenedor);

		mount(TarjetaDeArticulo, { target: contenedor, props });
		flushSync();

		return contenedor;
	}

	it('muestra título, resumen, la categoría que recibe y la fecha como dd/mm/aaaa', () => {
		const raiz = montar({ articulo, categoria: 'Cocina' });
		const texto = raiz.textContent?.replace(/\s+/g, ' ') ?? '';

		expect(raiz.querySelector('h3')?.textContent?.trim()).toBe('Tortilla de papa');
		expect(texto).toContain('La receta de la abuela, sin vueltas.');
		expect(texto).toContain('Cocina');
		expect(texto).toContain('30/10/2026');
		expect(texto).not.toContain('2026-10-30');
	});

	it('no es un enlace todavóa: el detalle del artículo es HU-ART-05', () => {
		// La tarjeta deja de apuntar a ninguna parte hasta que exista `/articulos/[id]`: un enlace a
		// una pantalla que da 404 es peor que una tarjeta quieta.
		const raiz = montar({ articulo, categoria: 'Cocina' });

		expect(raiz.querySelector('a')).toBeNull();
		expect(raiz.querySelector('article')).not.toBeNull();
	});

	it('con portada muestra la imagen contra el origen de la WebAPI', () => {
		const raiz = montar({
			articulo: { ...articulo, imagenPortadaUrl: '/uploads/articulos/x.png' },
			categoria: 'Cocina'
		});

		const imagen = raiz.querySelector('img');
		expect(imagen?.getAttribute('src')).toBe('http://localhost:5256/uploads/articulos/x.png');
	});

	it('sin portada muestra un reemplazo neutro en vez de una imagen rota', () => {
		const raiz = montar({ articulo, categoria: 'Cocina' });

		expect(raiz.querySelector('img')).toBeNull();
		expect(raiz.querySelector('[role="img"]')?.getAttribute('aria-label')).toBe('Sin portada');
	});
});
