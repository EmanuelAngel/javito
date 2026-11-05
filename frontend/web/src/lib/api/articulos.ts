// Módulo de artículos: sólo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni validación
// de runtime: el contrato lo define la WebAPI, que es nuestra.

import { pedir } from './pedir';

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
 * Una página de cualquier catálogo paginado del servidor. Los nombres son los del JSON —español—
 * porque son los que viajan: `elementos`, `pagina`, `tamanioDePagina`, `totalDeRegistros` y
 * `totalDePaginas`. Los reusan el catálogo de hábitos y el de usuarios.
 */
export type Pagina<T> = {
	elementos: T[];
	pagina: number;
	tamanioDePagina: number;
	totalDeRegistros: number;
	totalDePaginas: number;
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
	const formulario = new FormData();
	formulario.append('titulo', datos.titulo);
	formulario.append('resumen', datos.resumen);
	formulario.append('contenidoMarkdown', datos.contenidoMarkdown);
	formulario.append('categoriaId', String(datos.categoriaId));

	if (portada) formulario.append('imagenPortada', portada);

	return pedir<Articulo>('/api/articulos', { method: 'POST', body: formulario });
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
