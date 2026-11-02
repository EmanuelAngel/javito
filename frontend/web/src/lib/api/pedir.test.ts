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
});
