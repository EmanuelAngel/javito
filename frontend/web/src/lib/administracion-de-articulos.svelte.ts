// Estado de la pantalla /admin/articulos: la página del listado de administración (con el paginado
// genérico de `paginado.svelte.ts`), las categorías para nombrar cada fila y la reactivación. La
// pantalla lee la URL y compone; esto es lo que sabe de artículos (HU-ART-03).

import { goto } from '$app/navigation';
import { cambiarActivo, listarParaAdministracion, type ArticuloDelCatalogo } from './api/articulos';
import { listar as listarCategorias, type Categoria } from './api/categorias';
import { ErrorDeApi } from './api/pedir';
import { armarConsulta, crearPaginado } from './paginado.svelte';

function mensajeDeError(error: unknown, porDefecto: string): string {
	return error instanceof ErrorDeApi ? error.message : porDefecto;
}

export function estadoDeLaAdministracion(rutaDelListado: string) {
	// Este listado no tiene filtros: la página es lo único que viaja.
	const listado = crearPaginado<ArticuloDelCatalogo, null>({
		pedir: (pagina) => listarParaAdministracion({ pagina }),
		mensajeDeError: (error) => mensajeDeError(error, 'No se pudo cargar el listado.')
	});

	const categorias = $state({ lista: [] as Categoria[], error: '' });
	const reactivacion = $state({ enCurso: null as number | null, error: '' });

	// Todas, no sólo las activas: un artículo retirado o publicado antes de que se desactivara su
	// categoría tiene que poder nombrarla (ux-brief.md: las categorías son pocas).
	async function cargarCategorias(): Promise<void> {
		categorias.error = '';

		try {
			categorias.lista = await listarCategorias();
		} catch (error) {
			categorias.error = mensajeDeError(error, 'No se pudieron cargar las categorías.');
		}
	}

	/**
	 * Reactiva sin confirmación (es reversible) y actualiza la fila en su lugar: la página, el orden y
	 * el scroll quedan como estaban, y sólo cambia el estado de esa fila.
	 */
	async function reactivar(id: number): Promise<void> {
		reactivacion.enCurso = id;
		reactivacion.error = '';

		try {
			await cambiarActivo(id, true);

			const fila = listado.estado.pagina?.elementos.find((actual) => actual.id === id);
			if (fila) fila.activo = true;
		} catch (error) {
			reactivacion.error = mensajeDeError(error, 'No se pudo reactivar el artículo.');
		} finally {
			reactivacion.enCurso = null;
		}
	}

	/** Reemplaza la entrada del historial: «atrás» sale del listado, no recorre las páginas vistas. */
	function navegar(pagina: number): void {
		// `resolve()` no puede tipar esta URL: se le agrega la query de la página.
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		void goto(`${rutaDelListado}${armarConsulta({}, pagina)}`, { replace: true, reset: false });
	}

	return {
		listado,
		categorias,
		reactivacion,
		cargarCategorias,
		reactivar,
		navegar,
		nombreDe: (categoriaId: number) =>
			categorias.lista.find((categoria) => categoria.id === categoriaId)?.nombre ?? 'Sin categoría'
	};
}
