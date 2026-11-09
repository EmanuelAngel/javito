// Módulo de usuarios de la administración (HU-USU-01): declara el contrato y envuelve `pedir`. Sin
// `fetch` propio ni validación de runtime: el contrato lo define la WebAPI, que es nuestra.

import type { Pagina } from './pagina';
import { pedir } from './pedir';

export type { Pagina } from './pagina';

/** Los dos roles que acepta la base (`ENUM` de `usuario.rol`). */
export type RolDeUsuario = 'Administrador' | 'Usuario';

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
