// Mapeo compartido de errores de la WebAPI a mensajes de formulario: convierte un `ErrorDeApi`
// (ProblemDetails por campo con claves PascalCase, o un `codigo` de regla de negocio) en mensajes
// para pintar bajo cada input. Lo usan los formularios de alta, que repiten el mismo molde.

import { ErrorDeApi } from './pedir';

export type ErroresDeFormulario = {
	/** Mensaje por campo, con la clave camelCase del campo del formulario. */
	campos: Record<string, string>;

	/** Lo que no correspondió a ningún campo: un fallo de red, un 500, un código sin mapeo. */
	mensaje: string;
};

/**
 * Código de negocio -> campo del formulario. Es específico del alta de hábitos (S6); cada
 * formulario nuevo pasa su propio mapa. Cualquier código que no esté acá cae al mensaje general.
 */
export const CAMPOS_POR_CODIGO_DE_HABITO: Record<string, string> = {
	categoria_requerida: 'categoriaId',
	categoria_inexistente: 'categoriaId',
	categoria_inactiva: 'categoriaId',
	meta_requerida: 'meta',
	unidad_requerida: 'unidad',
	frecuencia_fuera_de_rango: 'veces'
};

/**
 * La pregunta canónica: ¿qué mensajes muestro y dónde? Nunca compara textos: decide por
 * `codigo` y por la clave de campo, que es lo estable del contrato.
 */
export function mapearErroresDeFormulario(
	error: unknown,
	camposPorCodigo: Record<string, string> = {}
): ErroresDeFormulario {
	if (!(error instanceof ErrorDeApi)) {
		return { campos: {}, mensaje: 'No se pudo contactar a la WebAPI. ¿Está levantada?' };
	}

	const campos: Record<string, string> = {};

	// El 400 de validación trae los errores por campo, con claves PascalCase (convención de MVC):
	// `Titulo` -> `titulo`, `Frecuencia.Veces` -> `veces`.
	if (error.estado === 400 && error.errores) {
		for (const [clave, mensajes] of Object.entries(error.errores)) {
			const campo = aClaveCamel(clave);

			if (mensajes.length > 0 && !(campo in campos)) campos[campo] = mensajes[0];
		}
	}

	// Una regla de negocio se mapea a su campo si el formulario le dio lugar, y no pisa lo que
	// la validación de campo ya puso.
	const campoDelCodigo = camposPorCodigo[error.codigo];

	if (campoDelCodigo && !(campoDelCodigo in campos)) {
		campos[campoDelCodigo] = error.message;
	}

	return {
		campos,
		mensaje: Object.keys(campos).length === 0 ? error.message : ''
	};
}

/** El último tramo de la clave, decapitalizado: `Frecuencia.Veces` -> `veces`. */
function aClaveCamel(clave: string): string {
	const ultimo = clave.slice(clave.lastIndexOf('.') + 1);

	return ultimo.charAt(0).toLowerCase() + ultimo.slice(1);
}
