// Cliente HTTP del SPA sobre fetch plano. Suma el `Authorization: Bearer` de la sesión,
// convierte el ProblemDetails del backend en `ErrorDeApi` y, en 401, cierra la sesión.
//
// No navega: el redirect a /login lo hace el `$effect` centralizado en el shell de (app),
// lo único que este módulo hace es desloguear. Por eso no importa `$app/navigation`.
// Ver odd/tasks/acc-02.md, S5 y S11: el 403 avisa y conserva la sesión; el login NO usa
// `pedir` (fetch crudo), porque su 401 es un error del formulario y no una sesión vencida.

import { API_URL } from '../config';
import { sesion } from '../sesion.svelte';

export class ErrorDeApi extends Error {
	readonly estado: number;
	// Campo propio además de `message`: los consumidores deciden por `codigo` y muestran `mensaje`.
	readonly codigo: string;
	readonly mensaje: string;
	readonly errores?: Record<string, string[]>;

	constructor(estado: number, codigo: string, mensaje: string, errores?: Record<string, string[]>) {
		super(mensaje);
		this.name = 'ErrorDeApi';
		this.estado = estado;
		this.mensaje = mensaje;
		this.codigo = codigo;
		this.errores = errores;
	}
}

type RespuestaLeida = {
	codigo: string;
	mensaje: string;
	errores?: Record<string, string[]>;
};

function esObjeto(valor: unknown): valor is Record<string, unknown> {
	return typeof valor === 'object' && valor !== null;
}

function esMapeoDeStrings(valor: unknown): valor is Record<string, string[]> {
	return (
		esObjeto(valor) &&
		Object.values(valor).every(
			(lista) => Array.isArray(lista) && lista.every((e) => typeof e === 'string')
		)
	);
}

function texto(valor: unknown): string | undefined {
	return typeof valor === 'string' && valor.length > 0 ? valor : undefined;
}

// Lee el ProblemDetails del error. Si el cuerpo no es JSON válido o falta algún campo,
// llena con valores por defecto: la respuesta nunca se queda sin `codigo`.
function leerProblema(respuesta: Response): Promise<RespuestaLeida> {
	const respuestaPorDefecto = (): RespuestaLeida => ({
		codigo: 'error_inesperado',
		mensaje: `La WebAPI respondió ${respuesta.status}.`
	});

	return respuesta
		.json()
		.then((cuerpo: unknown) => {
			if (!esObjeto(cuerpo)) {
				return respuestaPorDefecto();
			}

			return {
				codigo: texto(cuerpo.codigo) ?? 'error_inesperado',
				mensaje:
					texto(cuerpo.detail) ?? texto(cuerpo.title) ?? `La WebAPI respondió ${respuesta.status}.`,
				...(esMapeoDeStrings(cuerpo.errors) ? { errores: cuerpo.errors } : {})
			};
		})
		.catch(() => respuestaPorDefecto());
}

export type OpcionesDePedido = RequestInit & {
	/**
	 * No cierra la sesión cuando la API responde 401. Es para el login: su 401 es un error del
	 * formulario y no una sesión vencida, así que desloguear ahí se comería el mensaje.
	 */
	sinSesion?: boolean;
};

export async function pedir<T>(ruta: string, opciones: OpcionesDePedido = {}): Promise<T> {
	// `sinSesion` es una decisión del cliente, no una opción de `fetch`: se saca antes de armar
	// el pedido para no mandarla al servidor como si fuera parte del protocolo.
	const { sinSesion = false, ...resto } = opciones;

	// `Headers` y no un objeto plano: así también funcionan los arrays de pares y los `Headers`
	// que pase el llamador, que un spread descartaría en silencio.
	const cabeceras = new Headers(resto.headers);
	const token = sesion.token;

	if (token) {
		cabeceras.set('Authorization', `Bearer ${token}`);
	}

	// Un cuerpo JSON sin su Content-Type se rechaza con 415 en la WebAPI.
	if (resto.body !== undefined && !cabeceras.has('Content-Type')) {
		cabeceras.set('Content-Type', 'application/json');
	}

	const respuesta = await fetch(`${API_URL}${ruta}`, { ...resto, headers: cabeceras });

	if (!respuesta.ok) {
		const problema = await leerProblema(respuesta);

		// El 401 termina la sesión: o venció el token (8 hs, sin refresh) o el token no es
		// válido. El redirect lo hace el shell; acá sólo deslogueamos (S14).
		if (respuesta.status === 401 && !sinSesion) {
			sesion.cerrar();
		}

		throw new ErrorDeApi(respuesta.status, problema.codigo, problema.mensaje, problema.errores);
	}

	// El OK siempre trae JSON en esta API (health, categorías, /yo). Si el cuerpo viene
	// vacío, devolvemos un objeto vacío tipado para no romper al que espera un dato.
	return (await respuesta.json()) as unknown as T;
}
