// Estado de la pantalla /habitos (S9): las dos listas —activos y archivados—, las categorías para
// mapear nombre, y el formulario de alta con sus errores por campo. Sigue el molde de hoy.svelte.ts:
// el módulo es el dueño del estado y la pantalla lo consume.

import type { Categoria } from './api/categorias';
import {
	crear as crearHabito,
	type DatosDeHabito,
	type Habito,
	type TipoDeHabito
} from './api/habitos';
import {
	CAMPOS_POR_CODIGO_DE_HABITO,
	mapearErroresDeFormulario,
	type ErroresDeFormulario
} from './api/errores';

type EstadoInicial = {
	activos: Habito[];
	archivados: Habito[];
	categorias: Categoria[];
};

export type EstadoDeHabitos = ReturnType<typeof estadoDeHabitos>;

export function estadoDeHabitos(inicial: EstadoInicial) {
	const estado = $state({
		activos: inicial.activos,
		archivados: inicial.archivados,
		categorias: inicial.categorias,
		enviando: false,
		errores: { campos: {}, mensaje: '' } as ErroresDeFormulario,

		// Los campos que el navegador trabaja como texto (select, number) viajan como string
		// y la conversión la hace el alta.
		formulario: {
			titulo: '',
			descripcion: '',
			categoriaId: '',
			tipo: 'si_no' as TipoDeHabito,
			meta: '',
			unidad: '',
			frecuencia: 'diaria' as 'diaria' | 'semanal',
			veces: ''
		}
	});

	function limpiarFormulario(): void {
		estado.formulario = {
			titulo: '',
			descripcion: '',
			categoriaId: '',
			tipo: 'si_no',
			meta: '',
			unidad: '',
			frecuencia: 'diaria',
			veces: ''
		};
		estado.errores = { campos: {}, mensaje: '' };
	}

	/** El alta pide el hábito nuevo, lo suma a la lista de activos y limpia el formulario. */
	async function crear(): Promise<void> {
		const { titulo, descripcion, categoriaId, tipo, meta, unidad, frecuencia, veces } =
			estado.formulario;

		estado.enviando = true;
		estado.errores = { campos: {}, mensaje: '' };

		try {
			// Un contador lleva meta y unidad; el resto las manda null. La frecuencia semanal
			// lleva veces de 1 a 6; la diaria las manda null (S6).
			const datos: DatosDeHabito = {
				titulo: titulo.trim(),
				descripcion: descripcion.trim() === '' ? null : descripcion.trim(),
				categoriaId: Number(categoriaId),
				tipo,
				meta: tipo === 'contador' ? Number(meta) : null,
				unidad: tipo === 'contador' ? unidad : null,
				frecuencia:
					frecuencia === 'semanal'
						? { tipo: 'semanal', veces: Number(veces) }
						: { tipo: 'diaria', veces: null }
			};

			const habitoCreado = await crearHabito(datos);

			// El listado se muestra por título, como lo devuelve la WebAPI (S7): el nuevo entra
			// en su lugar, no al final.
			estado.activos = [...estado.activos, habitoCreado].sort((a, b) =>
				a.titulo.localeCompare(b.titulo, 'es')
			);

			limpiarFormulario();
		} catch (error) {
			estado.errores = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_HABITO);
		} finally {
			estado.enviando = false;
		}
	}

	return { estado, crear };
}
