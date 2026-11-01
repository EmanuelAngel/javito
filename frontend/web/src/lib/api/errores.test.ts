import { describe, expect, it } from 'vitest';
import { CAMPOS_POR_CODIGO_DE_HABITO, mapearErroresDeFormulario } from './errores';
import { ErrorDeApi } from './pedir';

describe('mapearErroresDeFormulario', () => {
	it('un error de validación por campo cae en el campo que le corresponde', () => {
		// La validación de MVC llega con claves PascalCase y a veces en ruta anidada.
		const error = new ErrorDeApi(400, 'error_inesperado', 'La solicitud no es válida.', {
			Titulo: ['El campo Título es obligatorio.'],
			'Frecuencia.Veces': ['El campo Veces debe estar entre 1 y 6.']
		});

		const mapeo = mapearErroresDeFormulario(error);

		expect(mapeo.campos['titulo']).toBe('El campo Título es obligatorio.');
		expect(mapeo.campos['veces']).toBe('El campo Veces debe estar entre 1 y 6.');
		expect(mapeo.mensaje).toBe('');
	});

	it('un código de negocio cae en el campo que el mapa le asigna', () => {
		const error = new ErrorDeApi(
			400,
			'meta_requerida',
			'Un contador necesita una meta mayor a cero.'
		);

		const mapeo = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_HABITO);

		expect(mapeo.campos['meta']).toBe('Un contador necesita una meta mayor a cero.');
		expect(mapeo.mensaje).toBe('');
	});

	it('un código sin mapeo cae en el mensaje general', () => {
		const error = new ErrorDeApi(400, 'habito_archivado', 'El hábito está archivado.');

		const mapeo = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_HABITO);

		expect(mapeo.campos).toEqual({});
		expect(mapeo.mensaje).toBe('El hábito está archivado.');
	});

	it('un fallo que no es de la API cae en el mensaje general', () => {
		const mapeo = mapearErroresDeFormulario(new TypeError('Failed to fetch'));

		expect(mapeo.campos).toEqual({});
		expect(mapeo.mensaje).toBe('No se pudo contactar a la WebAPI. ¿Está levantada?');
	});
});
