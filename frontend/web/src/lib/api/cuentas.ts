import type { Sesion } from '../sesion.svelte';
import { pedir } from './pedir';

/**
 * El cuerpo del registro público (HU-ACC-01). Sin rol ni avatar: la cuenta nace `Usuario` y activa, y
 * la API no tiene dónde recibir otra cosa.
 */
export type Registro = {
	nombre: string;
	email: string;
	password: string;
};

/** Mínimo de la contraseña: el mismo que valida la API (`ReglasDeUsuarios`). */
export const LONGITUD_MINIMA_DE_LA_CONTRASENA = 8;

/** `email_repetido` es un error del campo email y no del formulario entero. */
export const CAMPOS_POR_CODIGO_DE_REGISTRO: Record<string, string> = {
	email_repetido: 'email'
};

/**
 * Crea la cuenta y devuelve la sesión ya iniciada. `sinSesion`: el visitante no tiene token, y un 4xx
 * del registro es un error del formulario que no debe cerrar ni redirigir nada.
 */
export function registrar(datos: Registro): Promise<Sesion> {
	return pedir<Sesion>('/api/cuentas', {
		method: 'POST',
		body: JSON.stringify(datos),
		sinSesion: true
	});
}
