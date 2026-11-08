// Estado de la pantalla /historial: la página del historial (con el paginado genérico de
// `paginado.svelte.ts`), los hábitos del filtro y la navegación que escribe todo en la URL. Como el
// catálogo de artículos, no sabe de la pantalla: le pasan su ruta y la pantalla compone.

import { goto } from '$app/navigation';
import { listar, type FilaDelHistorial, type FiltrosDelHistorial } from './api/historial';
import { listar as listarHabitos, type Habito } from './api/habitos';
import { ErrorDeApi } from './api/pedir';
import { armarConsulta, crearPaginado } from './paginado.svelte';

export type DestinoDelHistorial = {
	habitoId: number | null;
	desde: string | null;
	hasta: string | null;
	pagina: number;
};

function mensajeDeError(error: unknown): string {
	return error instanceof ErrorDeApi ? error.message : 'No se pudo cargar el historial.';
}

export function estadoDelHistorial(rutaDelHistorial: string) {
	const historial = crearPaginado<FilaDelHistorial, FiltrosDelHistorial>({
		pedir: (pagina, filtros) => listar(pagina, filtros),
		mensajeDeError
	});

	// Los hábitos del filtro: todos, también los archivados. Archivar no borra el historial, así que
	// un hábito archivado tiene que poder elegirse para revisarlo (HU-HAB-04, HIS-01).
	const habitos = $state({ lista: [] as Habito[], cargando: true, error: '' });

	async function cargarHabitos(): Promise<void> {
		habitos.cargando = true;
		habitos.error = '';

		try {
			habitos.lista = await listarHabitos();
		} catch (error) {
			habitos.error = mensajeDeError(error);
		} finally {
			habitos.cargando = false;
		}
	}

	/**
	 * La query con los filtros y la página: la misma vista da siempre la misma URL, y un filtro vacío
	 * es la ausencia del parámetro. La primera página tampoco viaja.
	 */
	function consulta(destino: DestinoDelHistorial): string {
		return armarConsulta(
			{ habitoId: destino.habitoId, desde: destino.desde, hasta: destino.hasta },
			destino.pagina
		);
	}

	/**
	 * Reemplaza la entrada del historial (el «atrás» sale del historial, no recorre las páginas
	 * vistas) y no resetea scroll ni foco: sólo cambia la lista. SvelteKit 3 unificó
	 * `keepFocus`/`noScroll` en `reset`.
	 */
	function navegar(destino: DestinoDelHistorial): void {
		// `resolve()` no puede tipar esta URL: se le agrega una query armada con el estado actual.
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		void goto(`${rutaDelHistorial}${consulta(destino)}`, { replace: true, reset: false });
	}

	return { historial, habitos, cargarHabitos, navegar };
}
