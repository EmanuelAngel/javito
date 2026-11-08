// Módulo de usuarios de la administración (HU-USU-01 y HU-USU-02): declara el contrato y envuelve
// `pedir`. Sin `fetch` propio ni validación de runtime: el contrato lo define la WebAPI, que es
// nuestra.

import type { Pagina } from './pagina';
import { pedir } from './pedir';

export type { Pagina } from './pagina';

/** Los dos roles que acepta la base (`ENUM` de `usuario.rol`). */
export type RolDeUsuario = 'Administrador' | 'Usuario';

/**
 * Los roles en el orden en que los ofrece el alta. Es la misma lista que valida la API: el
 * desplegable no inventa un valor que el servidor vaya a rechazar con `rol_invalido`.
 */
export const ROLES: readonly RolDeUsuario[] = ['Administrador', 'Usuario'];

/**
 * Una cuenta tal como la ve la administración: con el estado a la vista y **sin** el hash de la
 * contraseña, que no sale del servidor. No es el `Usuario` de la sesión: ése no lleva `activo`.
 */
export type UsuarioAdministrado = {
	id: number;
	nombre: string;
	email: string;
	rol: RolDeUsuario;
	activo: boolean;
};

/** Los filtros del listado. El que no viaja no se manda: el default lo decide el servidor. */
export type FiltrosDeUsuarios = {
	pagina?: number;
	tamanioDePagina?: number;
	/** Pedazo del nombre o del email. Vacío no filtra. */
	q?: string | null;
	/** `null` trae los dos estados: es lo que muestra la pantalla sin filtro. */
	activo?: boolean | null;
};

/**
 * Una página de cuentas (HU-USU-01). Sólo Administración: un Usuario recibe 403, y el SPA lo saca de
 * la pantalla antes de pedir nada con `exigirAdministrador`.
 */
export function listar(filtros: FiltrosDeUsuarios = {}): Promise<Pagina<UsuarioAdministrado>> {
	const parametros = new URLSearchParams();

	if (filtros.pagina !== undefined) parametros.set('pagina', String(filtros.pagina));
	if (filtros.tamanioDePagina !== undefined) {
		parametros.set('tamanioDePagina', String(filtros.tamanioDePagina));
	}
	if (filtros.q) parametros.set('q', filtros.q);
	if (filtros.activo !== undefined && filtros.activo !== null) {
		parametros.set('activo', String(filtros.activo));
	}

	const consulta = parametros.toString();

	return pedir<Pagina<UsuarioAdministrado>>(
		`/api/usuarios${consulta === '' ? '' : `?${consulta}`}`
	);
}

/**
 * El cuerpo del alta (HU-USU-02). La contraseña inicial la elige el administrador y la comunica él:
 * el sistema no manda emails (S6). Viaja en claro hacia la API y no vuelve nunca: no hay campo donde
 * recibirla.
 */
export type AltaDeUsuario = {
	nombre: string;
	email: string;
	password: string;
	rol: RolDeUsuario;
};

/**
 * Los códigos de negocio del alta, cada uno en su campo: un email repetido es un error del email y no
 * del formulario entero. `rol_invalido` no debería llegar desde el desplegable, pero si llega se
 * muestra donde se eligió el rol.
 */
export const CAMPOS_POR_CODIGO_DE_USUARIO: Record<string, string> = {
	email_repetido: 'email',
	rol_invalido: 'rol'
};

/**
 * Crea una cuenta desde administración. Sólo Administración: un Usuario recibe 403, y el SPA lo saca
 * de la pantalla antes de pedir nada con `exigirAdministrador`.
 */
export function crear(datos: AltaDeUsuario): Promise<UsuarioAdministrado> {
	return pedir<UsuarioAdministrado>('/api/usuarios', {
		method: 'POST',
		body: JSON.stringify(datos)
	});
}
