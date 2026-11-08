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

	function montar(props: { articulo: ArticuloDelCatalogo; categoria: string; href?: string }) {
		contenedor = document.createElement('div');
		document.body.append(contenedor);

		mount(TarjetaDeArticulo, { target: contenedor, props: { href: '/articulos/3', ...props } });
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

	it('es un enlace al detalle con la dirección que recibe (HU-ART-05)', () => {
		// La dirección la arma el catálogo, que es quien sabe en qué página y filtro está: así el
		// detalle puede ofrecer «Volver al catálogo» a ese mismo lugar.
		const raiz = montar({ articulo, categoria: 'Cocina', href: '/articulos/3?pagina=2' });
		const enlace = raiz.querySelector('a');

		expect(enlace?.getAttribute('href')).toBe('/articulos/3?pagina=2');
		expect(enlace?.querySelector('h3')?.textContent?.trim()).toBe('Tortilla de papa');
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
