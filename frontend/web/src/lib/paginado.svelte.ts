// Estado de una lista paginada por servidor con su página y sus filtros en la URL (HU-ART-05 pide
// que volver conserve dónde se estaba). No sabe de artículos: el catálogo, el historial (HIS-01) y
// el listado de usuarios (USU-01) le pasan su propio pedido y sus propios filtros (#63).
//
// La pantalla sigue siendo la dueña de la URL: lee los parámetros con `leerPagina` y
// `leerEnteroPositivo`, arma la query con `armarConsulta` y llama a `cargar` desde un `$effect`.

import type { Pagina } from './api/pagina';

/**
 * Un entero positivo, o `null` si el valor no lo es. Una URL escrita a mano (`?pagina=0`,
 * `?categoriaId=abc`) no tiene que romper la pantalla: se lee como si no estuviera.
 */
export function leerEnteroPositivo(valor: string | null): number | null {
	const numero = Number(valor);

	return valor !== null && Number.isInteger(numero) && numero > 0 ? numero : null;
}

/** La página pedida en la URL; sin una válida, la primera. El servidor rechazaría la 0 con un 400. */
export function leerPagina(valor: string | null): number {
	return leerEnteroPositivo(valor) ?? 1;
}

/**
 * La query de la lista: los filtros en el orden en que llegan y la página al final, para que la
 * misma vista dé siempre la misma URL. La primera página y un filtro en `null` son la ausencia del
 * parámetro, no un valor más.
 */
export function armarConsulta(
	filtros: Record<string, string | number | null>,
	pagina: number
): string {
	const partes: string[] = [];

	for (const [nombre, valor] of Object.entries(filtros)) {
		if (valor !== null && valor !== '') partes.push(`${nombre}=${encodeURIComponent(valor)}`);
	}

	if (pagina > 1) partes.push(`pagina=${pagina}`);

	return partes.length === 0 ? '' : `?${partes.join('&')}`;
}

type Opciones<T, F> = {
	pedir: (pagina: number, filtros: F) => Promise<Pagina<T>>;
	mensajeDeError: (error: unknown) => string;
};

export function crearPaginado<T, F>({ pedir, mensajeDeError }: Opciones<T, F>) {
	const estado = $state({
		pagina: null as Pagina<T> | null,
		cargando: true,
		mensajeError: ''
	});

	// Cada cambio de URL dispara un pedido nuevo sin esperar al anterior: el turno descarta la
	// respuesta que llegó tarde, para que dos clics seguidos no dejen la página vieja en pantalla.
	let turno = 0;

	async function cargar(pagina: number, filtros: F): Promise<void> {
		const propio = ++turno;

		estado.cargando = true;
		estado.mensajeError = '';

		try {
			const respuesta = await pedir(pagina, filtros);

			if (propio === turno) estado.pagina = respuesta;
		} catch (error) {
			if (propio !== turno) return;

			estado.pagina = null;
			estado.mensajeError = mensajeDeError(error);
		} finally {
			if (propio === turno) estado.cargando = false;
		}
	}

	return {
		estado,
		cargar,

		get hayElementos(): boolean {
			return (estado.pagina?.elementos.length ?? 0) > 0;
		},

		/**
		 * La página pedida quedó más allá del final —un enlace viejo, o se borraron registros con la
		 * lista abierta—. El servidor devuelve vacío con la metadata honesta, y la pantalla lo dice en
		 * vez de mentir con un «no hay nada».
		 */
		get paginaInexistente(): boolean {
			return (
				estado.pagina !== null &&
				estado.pagina.elementos.length === 0 &&
				estado.pagina.totalDeRegistros > 0
			);
		},

		/** La última página que existe, para el atajo que sale de una página pasada del final. */
		get ultimaPagina(): number {
			return estado.pagina?.totalDePaginas ?? 1;
		}
	};
}
