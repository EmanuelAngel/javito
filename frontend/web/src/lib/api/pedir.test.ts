import { afterEach, describe, expect, it, vi } from 'vitest';
import { ErrorDeApi, pedir } from './pedir';

describe('pedir', () => {
	afterEach(() => vi.unstubAllGlobals());

	it('un 204 sin cuerpo no intenta leer JSON', async () => {
		// El DELETE de un registro responde 204: si `pedir` siempre parsea, el desmarcado explota con
		// un SyntaxError aunque el servidor haya hecho todo bien.
		vi.stubGlobal(
			'fetch',
			vi.fn(async () => new Response(null, { status: 204 }))
		);

		await expect(
			pedir('/api/habitos/1/registros/2001-01-01', { method: 'DELETE' })
		).resolves.toBeUndefined();
	});

	it('un ProblemDetails se convierte en ErrorDeApi con su codigo', async () => {
		vi.stubGlobal(
			'fetch',
			vi.fn(
				async () =>
					new Response(JSON.stringify({ codigo: 'habito_archivado', detail: 'Archivado.' }), {
						status: 400,
						headers: { 'Content-Type': 'application/problem+json' }
					})
			)
		);

		await expect(pedir('/api/habitos/17/registros/2001-01-01')).rejects.toMatchObject({
			name: 'ErrorDeApi',
			estado: 400,
			codigo: 'habito_archivado'
		});
	});

	it('un error sin cuerpo de ProblemDetails igual llega como ErrorDeApi', async () => {
		vi.stubGlobal(
			'fetch',
			vi.fn(async () => new Response('<html>proxy</html>', { status: 502 }))
		);

		await expect(pedir('/api/x')).rejects.toBeInstanceOf(ErrorDeApi);
	});

	it('un cuerpo JSON viaja con Content-Type application/json', async () => {
		const fetchFalso = vi.fn<(ruta: string, opciones: RequestInit) => Promise<Response>>(
			async () => new Response('{}')
		);
		vi.stubGlobal('fetch', fetchFalso);

		await pedir('/api/categorias', { method: 'POST', body: JSON.stringify({ nombre: 'x' }) });

		const [, opciones] = fetchFalso.mock.calls[0];
		expect(new Headers(opciones.headers).get('Content-Type')).toBe('application/json');
	});

	it('un cuerpo FormData no lleva Content-Type JSON: el boundary lo pone el navegador', async () => {
		// El avatar viaja como multipart. Si `pedir` estampa application/json, el servidor no puede
		// parsear el formulario y toda subida falla con un 400 que no explica nada.
		const fetchFalso = vi.fn<(ruta: string, opciones: RequestInit) => Promise<Response>>(
			async () => new Response('{}')
		);
		vi.stubGlobal('fetch', fetchFalso);

		const formulario = new FormData();
		formulario.append('archivo', new Blob([new Uint8Array([1, 2, 3])]), 'foto.png');

		await pedir('/api/perfil/avatar', { method: 'POST', body: formulario });

		const [, opciones] = fetchFalso.mock.calls[0];
		expect(new Headers(opciones.headers).has('Content-Type')).toBe(false);
	});
});
