import { flushSync, mount } from 'svelte';
import { afterEach, describe, expect, it } from 'vitest';
import Markdown from './markdown.svelte';

// HU-ART-01: la vista previa del alta y el detalle del artículo comparten este renderizador. La
// primera regla es que el Markdown se vea; la segunda, y por la que existe el componente y no un
// `{@html}` suelto, es que lo que no es Markdown no llegue al DOM.
describe('markdown', () => {
	let contenedor: HTMLElement | null = null;

	afterEach(() => {
		document.body.innerHTML = '';
		contenedor = null;
	});

	function montar(contenido: string) {
		contenedor = document.createElement('div');
		document.body.append(contenedor);

		mount(Markdown, { target: contenedor, props: { contenido } });
		flushSync();

		return contenedor;
	}

	it('renderiza títulos, listas y negritas', () => {
		const raiz = montar('# Un título\n\n- primero\n- segundo\n\nCon **negritas**.');

		expect(raiz.querySelector('h1')?.textContent).toBe('Un título');
		expect(raiz.querySelectorAll('li')).toHaveLength(2);
		expect(raiz.querySelector('strong')?.textContent).toBe('negritas');
	});

	it('descarta el script que venga dentro del contenido', () => {
		const raiz = montar('# Hola\n\n<script>alert("xss")</script>');

		expect(raiz.querySelector('script')).toBeNull();
		expect(raiz.textContent).not.toContain('alert');
	});

	it('descarta los manejadores de eventos y los enlaces javascript:', () => {
		const raiz = montar(
			'<img src="x" onerror="alert(1)">\n\n[tocalo](javascript:alert(1))\n\n<p onclick="alert(2)">texto</p>'
		);

		expect(raiz.querySelector('[onerror]')).toBeNull();
		expect(raiz.querySelector('[onclick]')).toBeNull();

		const enlace = raiz.querySelector('a');
		expect(enlace?.getAttribute('href') ?? '').not.toContain('javascript:');
	});

	it('descarta SVG con onload, iframes y style', () => {
		// Vectores que `marked` deja pasar como HTML crudo: si DOMPurify fallara en uno solo, el
		// artículo publicado sería un XSS almacenado en el navegador de cada lector.
		const raiz = montar(
			'<svg onload="alert(1)"><circle r="1" /></svg>\n\n<iframe src="https://example.com"></iframe>\n\n<style>body{display:none}</style>\n\n[a](JaVaScRiPt:alert(1))'
		);

		expect(raiz.querySelector('svg[onload]')).toBeNull();
		expect(raiz.querySelector('iframe')).toBeNull();
		expect(raiz.querySelector('style')).toBeNull();
		expect(raiz.querySelector('a')?.getAttribute('href') ?? '').not.toMatch(/javascript:/i);
	});

	it('sin contenido no rompe ni deja el contenedor vacío de estilo', () => {
		const raiz = montar('');

		expect(raiz.querySelector('.prose')).not.toBeNull();
		expect(raiz.textContent?.trim()).toBe('');
	});
});
