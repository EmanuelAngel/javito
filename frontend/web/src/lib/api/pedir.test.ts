import { afterEach, describe, expect, it, vi } from 'vitest';
import { ErrorDeApi, pedir } from './pedir';
import { sesion } from '../sesion.svelte';

const USUARIO = {
	id: 1,
	nombre: 'Usuario',
	email: 'usuario@javito.local',
	rol: 'Usuario'
};

// Gerencia mínima de fetch para las pruebas: responde JSON plano con el estado dado.
function respuestaJson(estado: number, cuerpo: unknown): Response {
	return {
		ok: estado >= 200 && estado < 300,
		status: estado,
		json: async () => cuerpo
	} as unknown as Response;
}

describe('pedir', () => {
	afterEach(() => {
		vi.unstubAllGlobals();
		sesion.cerrar();
	});

	it('suma el encabezado Authorization con el token de la sesión', async () => {
		sesion.iniciar({ token: 'token-de-prueba', usuario: USUARIO });

		let initCapturado: RequestInit | undefined;
		vi.stubGlobal(
			'fetch',
			vi.fn(async (_entrada: unknown, init?: RequestInit) => {
				initCapturado = init;
				return respuestaJson(200, {});
			})
		);

		await pedir('/api/sesion/yo');

		expect(new Headers(initCapturado?.headers).get('Authorization')).toBe('Bearer token-de-prueba');
	});

	it('pone Content-Type JSON cuando hay cuerpo', async () => {
		let initCapturado: RequestInit | undefined;
		vi.stubGlobal(
			'fetch',
			vi.fn(async (_entrada: unknown, init?: RequestInit) => {
				initCapturado = init;
				return respuestaJson(200, {});
			})
		);

		await pedir('/api/categorias', { method: 'POST', body: JSON.stringify({ nombre: 'Cuerpo' }) });

		expect(new Headers(initCapturado?.headers).get('Content-Type')).toBe('application/json');
	});

	it('con sinSesion no cierra la sesión en 401', async () => {
		sesion.iniciar({ token: 'token-de-prueba', usuario: USUARIO });
		vi.stubGlobal(
			'fetch',
			vi.fn(async () =>
				respuestaJson(401, {
					codigo: 'credenciales_invalidas',
					detail: 'El email o la contraseña no son correctos.'
				})
			)
		);

		await expect(
			pedir('/api/sesion', { method: 'POST', body: '{}', sinSesion: true })
		).rejects.toBeInstanceOf(ErrorDeApi);

		expect(sesion.token).toBe('token-de-prueba');
	});

	it('convierte el ProblemDetails en ErrorDeApi con codigo y mensaje', async () => {
		vi.stubGlobal(
			'fetch',
			vi.fn(async () =>
				respuestaJson(403, {
					type: 'https://tools.ietf.org/html/rfc9110#section-15.5.4',
					title: 'Forbidden',
					status: 403,
					codigo: 'sin_permiso',
					detail: 'No tenés permiso para esta operación.'
				})
			)
		);

		// Se captura al vuelo: los errores clonados por el worker de vitest pierden las
		// propiedades propias de ErrorDeApi (estado, codigo, mensajes).
		let capturado: unknown;
		try {
			await pedir('/api/categorias');
		} catch (causa: unknown) {
			capturado = causa;
		}

		expect(capturado).toBeInstanceOf(ErrorDeApi);
		const error = capturado as ErrorDeApi;
		expect(error.estado).toBe(403);
		expect(error.codigo).toBe('sin_permiso');
		expect(error.mensaje).toBe('No tenés permiso para esta operación.');
	});

	it('en 401 cierra la sesión y lanza', async () => {
		sesion.iniciar({ token: 'token-vencido', usuario: USUARIO });

		vi.stubGlobal(
			'fetch',
			vi.fn(async () =>
				respuestaJson(401, {
					title: 'Unauthorized',
					status: 401,
					codigo: 'no_autenticado',
					detail: 'La sesión no es válida.'
				})
			)
		);

		let capturado: unknown;
		try {
			await pedir('/api/categorias');
		} catch (causa: unknown) {
			capturado = causa;
		}

		expect(capturado).toBeInstanceOf(ErrorDeApi);
		expect((capturado as ErrorDeApi).estado).toBe(401);
		expect((capturado as ErrorDeApi).codigo).toBe('no_autenticado');
		expect(sesion.actual).toBeNull();
		expect(sesion.token).toBeNull();
	});

	it('en 403 lanza pero conserva la sesión', async () => {
		sesion.iniciar({ token: 'token-viejo', usuario: USUARIO });

		vi.stubGlobal(
			'fetch',
			vi.fn(async () =>
				respuestaJson(403, {
					title: 'Forbidden',
					status: 403,
					codigo: 'sin_permiso',
					detail: 'No tenés permiso para esta operación.'
				})
			)
		);

		let capturado: unknown;
		try {
			await pedir('/api/categorias');
		} catch (causa: unknown) {
			capturado = causa;
		}

		expect(capturado).toBeInstanceOf(ErrorDeApi);
		expect((capturado as ErrorDeApi).estado).toBe(403);
		expect((capturado as ErrorDeApi).codigo).toBe('sin_permiso');
		expect(sesion.actual).not.toBeNull();
		expect(sesion.token).toBe('token-viejo');
	});
});
