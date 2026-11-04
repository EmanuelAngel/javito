import { flushSync, mount } from 'svelte';
import { afterEach, describe, expect, it } from 'vitest';
import Avatar from './avatar.svelte';

// HU-PER-02: las dos caras del avatar. El reemplazo (la inicial del nombre) es una regla de la
// historia, no un detalle de estilo: en la base el avatar nace NULL y hay que verlo bien.
describe('avatar', () => {
	let contenedor: HTMLElement | null = null;

	afterEach(() => {
		document.body.innerHTML = '';
		contenedor = null;
	});

	function montar(props: { nombre: string; avatarUrl: string | null }) {
		contenedor = document.createElement('div');
		document.body.append(contenedor);

		mount(Avatar, { target: contenedor, props });
		flushSync();

		return contenedor;
	}

	it('sin avatar muestra la inicial del nombre', () => {
		const raiz = montar({ nombre: 'otra cuenta', avatarUrl: null });

		expect(raiz.textContent?.trim()).toBe('O');
		expect(raiz.querySelector('img')).toBeNull();
		expect(raiz.querySelector('[role="img"]')?.getAttribute('aria-label')).toBe(
			'Avatar de otra cuenta'
		);
	});

	it('con avatar muestra la imagen contra el origen de la WebAPI', () => {
		const raiz = montar({ nombre: 'Otra cuenta', avatarUrl: '/uploads/avatares/x.png' });

		const imagen = raiz.querySelector('img');
		expect(imagen?.getAttribute('src')).toBe('http://localhost:5256/uploads/avatares/x.png');
		expect(imagen?.getAttribute('alt')).toBe('Avatar de Otra cuenta');
	});
});
