import { afterEach, describe, expect, it, vi } from 'vitest';
import { CAMPOS_POR_CODIGO_DE_REGISTRO, registrar } from './cuentas';
import { mapearErroresDeFormulario } from './errores';
import { ErrorDeApi } from './pedir';

afterEach(() => vi.unstubAllGlobals());

describe('registrar', () => {
	it('manda el cuerpo por POST a /api/cuentas y devuelve la sesión', async () => {
		const sesion = { token: 't', usuario: { id: 1, nombre: 'Ana' } };
		const fetchFalso = vi.fn().mockResolvedValue(
			new Response(JSON.stringify(sesion), {
				status: 201,
				headers: { 'Content-Type': 'application/json' }
			})
		);
		vi.stubGlobal('fetch', fetchFalso);

		const devuelta = await registrar({ nombre: 'Ana', email: 'a@x.com', password: 'Secreta2026' });

		const [url, opciones] = fetchFalso.mock.calls[0];
		expect(String(url)).toMatch(/\/api\/cuentas$/);
		expect(opciones.method).toBe('POST');
		expect(JSON.parse(opciones.body)).toEqual({
			nombre: 'Ana',
			email: 'a@x.com',
			password: 'Secreta2026'
		});
		expect(devuelta).toEqual(sesion);
	});

	it('un 409 email_repetido se muestra en el campo email', async () => {
		vi.stubGlobal(
			'fetch',
			vi
				.fn()
				.mockResolvedValue(
					new Response(
						JSON.stringify({ codigo: 'email_repetido', detail: 'Ya existe una cuenta.' }),
						{ status: 409, headers: { 'Content-Type': 'application/problem+json' } }
					)
				)
		);

		const error = await registrar({ nombre: 'A', email: 'a@x.com', password: 'Secreta2026' }).catch(
			(e: unknown) => e
		);

		expect(error).toBeInstanceOf(ErrorDeApi);
		const { campos } = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_REGISTRO);
		expect(campos.email).toBeTruthy();
	});
});
