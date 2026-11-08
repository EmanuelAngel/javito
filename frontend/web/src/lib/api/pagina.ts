/**
 * Una página de cualquier catálogo paginado del servidor. Los nombres son los del JSON —español—
 * porque son los que viajan: `elementos`, `pagina`, `tamanioDePagina`, `totalDeRegistros` y
 * `totalDePaginas`. Los reusan el catálogo de artículos, el historial y el de usuarios.
 */
export type Pagina<T> = {
	elementos: T[];
	pagina: number;
	tamanioDePagina: number;
	totalDeRegistros: number;
	totalDePaginas: number;
};
