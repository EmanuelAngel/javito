// Cliente HTTP del SPA: el único archivo que llama a `fetch`. Hace lo mínimo para no repetirlo
// en cada llamada —base URL, Bearer, Content-Type y ProblemDetails → ErrorDeApi— y nada más.
// No valida el JSON en runtime: el contrato lo define la WebAPI, que es nuestra.

import { API_URL } from '../config';
import { sesion } from '../sesion.svelte';

/** Error de la API. El cliente decide por `codigo`, nunca comparando textos. */
export class ErrorDeApi extends Error {
	constructor(
		readonly estado: number,
		readonly codigo: string,
		mensaje: string,
		readonly errores?: Record<string, string[]>
	) {
		super(mensaje);
		this.name = 'ErrorDeApi';
	}
}

export type OpcionesDePedido = RequestInit & { sinSesion?: boolean };

type Problema = {
	codigo?: string;
	detail?: string;
	errors?: Record<string, string[]>;
};

export async function pedir<T>(ruta: string, opciones: OpcionesDePedido = {}): Promise<T> {
	const { sinSesion = false, ...resto } = opciones;
	const cabeceras = new Headers(resto.headers);

	if (sesion.token) cabeceras.set('Authorization', `Bearer ${sesion.token}`);
	if (resto.body) cabeceras.set('Content-Type', 'application/json');

	const respuesta = await fetch(`${API_URL}${ruta}`, { ...resto, headers: cabeceras });

	if (!respuesta.ok) {
		// 401 es sesión vencida: se cierra acá. El login pasa `sinSesion` porque su 401 es un error
		// del formulario. El 403 NO cierra: es la evidencia de que el rol no alcanza.
		if (respuesta.status === 401 && !sinSesion) sesion.cerrar();

		// Si el cuerpo no es JSON (un proxy, un 500 de Kestrel), no hay nada que leer.
		const problema = (await respuesta.json().catch(() => ({}))) as Problema;

		throw new ErrorDeApi(
			respuesta.status,
			problema.codigo ?? 'error_inesperado',
			problema.detail ?? `La WebAPI respondió ${respuesta.status}.`,
			problema.errors
		);
	}

	return (await respuesta.json()) as T;
}
