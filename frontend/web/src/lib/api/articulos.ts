// Módulo de artículos: sólo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni validación
// de runtime: el contrato lo define la WebAPI, que es nuestra.

import type { Pagina } from './pagina';
import { pedir } from './pedir';

export type { Pagina } from './pagina';

export type Articulo = {
	id: number;
	categoriaId: number;
	titulo: string;
	resumen: string;
	/** Markdown crudo. Se renderiza siempre con `markdown.svelte`, que sanitiza. */
	contenidoMarkdown: string;
	imagenPortadaUrl: string | null;
	/** aaaa-mm-dd: el servidor no manda hora porque ninguna pantalla la muestra. */
	fechaPublicacion: string;
	activo: boolean;
};

/**
 * Un artículo tal como lo ve el catálogo: <b>sin</b> `contenidoMarkdown`. Traer el cuerpo de seis
 * artículos para pintar seis tarjetas sería exactamente lo que el paginado vino a evitar.
 */
export type ArticuloDelCatalogo = {
	id: number;
	/** La API manda el id: el nombre de la categoría lo resuelve la pantalla con su catálogo. */
	categoriaId: number;
	titulo: string;
	resumen: string;
	imagenPortadaUrl: string | null;
	/** aaaa-mm-dd, igual que el resto de la API. */
	fechaPublicacion: string;
	activo: boolean;
};

/**
 * Lo que se le puede pedir al catálogo. Todo opcional: sin nada, el servidor decide (página 1, seis
 * por página, todas las categorías). Un parámetro que no se manda no viaja: el default vive en la
 * WebAPI, no duplicado acá.
 */
export type FiltrosDelCatalogo = {
	pagina?: number;
	tamanioDePagina?: number;
	categoriaId?: number | null;
};

export type DatosDeArticulo = {
	titulo: string;
	resumen: string;
	contenidoMarkdown: string;
	categoriaId: number;
};

/** Tope de la portada, el mismo que aplica el servidor: acá sirve para no subir 20 MB al pedo. */
export const TAMANO_MAXIMO_DE_LA_PORTADA = 5 * 1024 * 1024;

/** Formatos que acepta la API: la lista sale de las firmas que reconoce el almacén. */
export const FORMATOS_DE_LA_PORTADA = 'image/jpeg,image/png,image/webp';

/**
 * Publica un artículo (HU-ART-01). El cuerpo es `FormData` porque la portada es un archivo y puede
 * no venir: sin portada el artículo se publica igual, y el `Content-Type` multipart lo arma el
 * navegador.
 */
export function publicar(datos: DatosDeArticulo, portada: File | null): Promise<Articulo> {
	return pedir<Articulo>('/api/articulos', { method: 'POST', body: formularioDe(datos, portada) });
}

/**
 * Corrige un artículo publicado (HU-ART-02). Sin `portada` el servidor conserva la que tenía; con
 * una, la reemplaza y borra la anterior. La fecha de publicación no cambia.
 */
export function editar(
	id: number,
	datos: DatosDeArticulo,
	portada: File | null
): Promise<Articulo> {
	return pedir<Articulo>(`/api/articulos/${id}`, {
		method: 'PUT',
		body: formularioDe(datos, portada)
	});
}

/** El mismo multipart para el alta y la edición: un campo nuevo se agrega en un solo lugar. */
function formularioDe(datos: DatosDeArticulo, portada: File | null): FormData {
	const formulario = new FormData();
	formulario.append('titulo', datos.titulo);
	formulario.append('resumen', datos.resumen);
	formulario.append('contenidoMarkdown', datos.contenidoMarkdown);
	formulario.append('categoriaId', String(datos.categoriaId));

	if (portada) formulario.append('imagenPortada', portada);

	return formulario;
}

/**
 * El catálogo (HU-ART-04, S5): sólo artículos activos, del más nuevo al más viejo, paginado y
 * opcionalmente filtrado por categoría. Una página pasada del final vuelve vacía con la metadata
 * tal como se pidió, y el servidor no la corrige: quien la pidió se equivocó y hay que decirlo.
 */
export function listar(filtros: FiltrosDelCatalogo = {}): Promise<Pagina<ArticuloDelCatalogo>> {
	const parametros = new URLSearchParams();

	if (filtros.pagina !== undefined) parametros.set('pagina', String(filtros.pagina));
	if (filtros.tamanioDePagina !== undefined) {
		parametros.set('tamanioDePagina', String(filtros.tamanioDePagina));
	}
	if (filtros.categoriaId !== undefined && filtros.categoriaId !== null) {
		parametros.set('categoriaId', String(filtros.categoriaId));
	}

	const consulta = parametros.toString();

	return pedir<Pagina<ArticuloDelCatalogo>>(
		`/api/articulos${consulta === '' ? '' : `?${consulta}`}`
	);
}

/** Un artículo por id. Un artículo retirado responde 404, no un 403 (HU-ART-03). */
export function obtener(id: number): Promise<Articulo> {
	return pedir<Articulo>(`/api/articulos/${id}`);
}

/**
 * Retira (`false`) o reactiva (`true`) un artículo sin borrarlo (HU-ART-03). Sólo Administración: un
 * Usuario recibe 403. Responde el artículo con su estado nuevo, con el cuerpo incluido.
 */
export function cambiarActivo(id: number, activo: boolean): Promise<Articulo> {
	return pedir<Articulo>(`/api/articulos/${id}/activo`, {
		method: 'PUT',
		body: JSON.stringify({ activo })
	});
}

/**
 * El listado de administración (S25): activos **e inactivos**, con el mismo paginado y el mismo orden
 * que el catálogo. Es el único camino para ver lo retirado y poder reactivarlo.
 */
export function listarParaAdministracion(
	filtros: Pick<FiltrosDelCatalogo, 'pagina' | 'tamanioDePagina'> = {}
): Promise<Pagina<ArticuloDelCatalogo>> {
	const parametros = new URLSearchParams();

	if (filtros.pagina !== undefined) parametros.set('pagina', String(filtros.pagina));
	if (filtros.tamanioDePagina !== undefined) {
		parametros.set('tamanioDePagina', String(filtros.tamanioDePagina));
	}

	const consulta = parametros.toString();

	return pedir<Pagina<ArticuloDelCatalogo>>(
		`/api/articulos/administracion${consulta === '' ? '' : `?${consulta}`}`
	);
}
