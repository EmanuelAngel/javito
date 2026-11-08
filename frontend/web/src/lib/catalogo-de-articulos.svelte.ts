// Estado de la pantalla /articulos: las categorías del filtro, la página del catálogo (con el
// paginado genérico de `paginado.svelte.ts`) y la navegación que escribe ambas cosas en la URL.
// La pantalla lee la URL y compone; esto es lo que sabe de artículos (#63).

import { goto } from '$app/navigation';
import { resolve } from '$app/paths';
import { listar, type ArticuloDelCatalogo } from './api/articulos';
import { listar as listarCategorias, type Categoria } from './api/categorias';
import { ErrorDeApi } from './api/pedir';
import { armarConsulta, crearPaginado } from './paginado.svelte';

export type DestinoDelCatalogo = { categoriaId: number | null; pagina: number };

function mensajeDeError(error: unknown): string {
	return error instanceof ErrorDeApi ? error.message : 'No se pudo cargar el catálogo.';
}

export function estadoDelCatalogo(rutaDelCatalogo: string) {
	const catalogo = crearPaginado<ArticuloDelCatalogo, number | null>({
		pedir: (pagina, categoriaId) => listar({ pagina, categoriaId }),
		mensajeDeError
	});

	const categorias = $state({ lista: [] as Categoria[], cargando: true, error: '' });

	// Las categorías no dependen de la página ni del filtro: se piden una vez. El filtro ofrece sólo
	// las activas, las únicas que clasifican algo nuevo (HU-CAT-04).
	async function cargarCategorias(): Promise<void> {
		categorias.cargando = true;
		categorias.error = '';

		try {
			categorias.lista = await listarCategorias(true);
		} catch (error) {
			categorias.error = mensajeDeError(error);
		} finally {
			categorias.cargando = false;
		}
	}

	/**
	 * Reemplaza la entrada del historial (el «atrás» sale del catálogo, no recorre las páginas
	 * vistas) y no resetea scroll ni foco: sólo cambia la lista. SvelteKit 3 unificó
	 * `keepFocus`/`noScroll` en `reset`.
	 */
	function navegar(destino: DestinoDelCatalogo): void {
		const consulta = armarConsulta({ categoriaId: destino.categoriaId }, destino.pagina);

		// `resolve()` no puede tipar esta URL: se le agrega una query armada con el estado actual.
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		void goto(`${rutaDelCatalogo}${consulta}`, { replace: true, reset: false });
	}

	/**
	 * El detalle de un artículo con la página y el filtro de ahora en la query: así su «Volver al
	 * catálogo» vuelve acá mismo (HU-ART-05) sin estado escondido, y el enlace se puede copiar.
	 */
	function detalleDe(articuloId: number, desde: DestinoDelCatalogo): string {
		const detalle = resolve('/(app)/articulos/[id]', { id: String(articuloId) });

		return `${detalle}${armarConsulta({ categoriaId: desde.categoriaId }, desde.pagina)}`;
	}

	return {
		catalogo,
		detalleDe,
		categorias,
		cargarCategorias,
		navegar,
		// Son pocas (ux-brief.md): buscar en la lista alcanza y no hace falta un índice.
		nombreDe: (categoriaId: number) =>
			categorias.lista.find((categoria) => categoria.id === categoriaId)?.nombre ?? 'Sin categoría'
	};
}
