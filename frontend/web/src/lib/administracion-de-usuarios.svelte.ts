// Estado de la pantalla /admin/usuarios: la página del listado con el paginado genérico de
// `paginado.svelte.ts` y los filtros que viajan en la URL, para que volver conserve la búsqueda. La
// pantalla lee la URL y compone; esto es lo que sabe de usuarios (HU-USU-01).

import { goto } from '$app/navigation';
import { ErrorDeApi } from './api/pedir';
import { listar, type UsuarioAdministrado } from './api/usuarios';
import { armarConsulta, crearPaginado } from './paginado.svelte';

/** Los filtros tal como viven en la URL: `activo` es el texto del parámetro, no un booleano. */
export type FiltrosDeLaPantalla = {
	q: string;
	activo: string | null;
};

export type Estado = 'todos' | 'activos' | 'desactivados';

/**
 * El valor de `activo` que viaja a la API. Una URL escrita a mano (`?activo=quizas`) se lee como
 * «todos» en vez de romper la pantalla, igual que `leerPagina` con `?pagina=abc`.
 */
export function leerActivo(valor: string | null): boolean | null {
	if (valor === 'true') return true;
	if (valor === 'false') return false;

	return null;
}

/** El estado que muestra el filtro, para marcar la opción elegida. */
export function leerEstado(valor: string | null): Estado {
	if (valor === 'true') return 'activos';
	if (valor === 'false') return 'desactivados';

	return 'todos';
}

export function estadoDeLaAdministracion(rutaDelListado: string) {
	const listado = crearPaginado<UsuarioAdministrado, FiltrosDeLaPantalla>({
		pedir: (pagina, filtros) =>
			listar({ pagina, q: filtros.q, activo: leerActivo(filtros.activo) }),
		mensajeDeError: (error) =>
			error instanceof ErrorDeApi ? error.message : 'No se pudo cargar el listado de usuarios.'
	});

	/** Reemplaza la entrada del historial: «atrás» sale del listado, no recorre las páginas vistas. */
	function navegar(pagina: number, filtros: FiltrosDeLaPantalla): void {
		const destino = `${rutaDelListado}${armarConsulta({ q: filtros.q, activo: filtros.activo }, pagina)}`;

		// `resolve()` no puede tipar esta URL: se le agrega la query de la página.
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		void goto(destino, { replace: true, reset: false });
	}

	return { listado, navegar };
}
