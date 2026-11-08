// Estado de la pantalla /habitos (S9): las dos listas —activos y archivados—, las categorías para
// mapear nombre, y el formulario, que sirve para el alta y para la edición. Sigue el molde de
// hoy.svelte.ts: el módulo es el dueño del estado y la pantalla lo consume.

import type { Categoria } from './api/categorias';
import {
	crear as crearHabito,
	editar as editarHabito,
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

/** El estado completo de la pantalla: lo que devuelve `estadoDeHabitos`. */
export type EstadoDeHabitos = ReturnType<typeof estadoDeHabitos>;

export function estadoDeHabitos(inicial: EstadoInicial) {
	const estado = $state({
		activos: inicial.activos,
		archivados: inicial.archivados,
		categorias: inicial.categorias,
		enviando: false,
		errores: { campos: {}, mensaje: '' } as ErroresDeFormulario,

		// El id del hábito que se está corrigiendo, o null cuando el formulario da de alta (S1).
		idEnEdicion: null as number | null,

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
		estado.idEnEdicion = null;
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

	/** Una categoría vacía viaja como null: es el caso del hábito que se edita sin categoría. */
	function categoriaElegida(categoriaId: string): number | null {
		return categoriaId === '' ? null : Number(categoriaId);
	}

	/**
	 * El cuerpo del pedido, igual para el alta y la edición. Un contador lleva meta y unidad; el resto
	 * las manda null. La frecuencia semanal lleva veces de 1 a 6; la diaria las manda null (S6).
	 */
	function datosDelFormulario(): DatosDeHabito {
		const { titulo, descripcion, categoriaId, tipo, meta, unidad, frecuencia, veces } =
			estado.formulario;

		return {
			titulo: titulo.trim(),
			descripcion: descripcion.trim() === '' ? null : descripcion.trim(),
			categoriaId: categoriaElegida(categoriaId),
			tipo,
			meta: tipo === 'contador' ? Number(meta) : null,
			unidad: tipo === 'contador' ? unidad : null,
			frecuencia:
				frecuencia === 'semanal'
					? { tipo: 'semanal', veces: Number(veces) }
					: { tipo: 'diaria', veces: null }
		};
	}

	/** El listado se muestra por título, como lo devuelve la WebAPI (S7). */
	function porTitulo(habitos: Habito[]): Habito[] {
		return [...habitos].sort((a, b) => a.titulo.localeCompare(b.titulo, 'es'));
	}

	/**
	 * Cambia el hábito editado en la lista donde esté —activo o archivado (L5)— sin moverlo de
	 * bloque: la edición no archiva ni reactiva (#70).
	 */
	function reemplazar(habito: Habito, lista: Habito[]): Habito[] {
		return porTitulo(lista.map((actual) => (actual.id === habito.id ? habito : actual)));
	}

	/** El alta pide el hábito nuevo, lo suma a la lista de activos y limpia el formulario. */
	async function crear(): Promise<void> {
		estado.enviando = true;
		estado.errores = { campos: {}, mensaje: '' };

		try {
			const habitoCreado = await crearHabito(datosDelFormulario());

			// El nuevo entra en su lugar, no al final.
			estado.activos = porTitulo([...estado.activos, habitoCreado]);

			limpiarFormulario();
		} catch (error) {
			estado.errores = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_HABITO);
		} finally {
			estado.enviando = false;
		}
	}

	/** Carga el hábito en el formulario y lo deja en modo edición (S1). */
	function prepararEdicion(habito: Habito): void {
		estado.idEnEdicion = habito.id;
		estado.formulario = {
			titulo: habito.titulo,
			descripcion: habito.descripcion ?? '',
			categoriaId: habito.categoriaId === null ? '' : String(habito.categoriaId),
			tipo: habito.tipo,
			meta: habito.meta === null ? '' : String(habito.meta),
			unidad: habito.unidad ?? '',
			frecuencia: habito.frecuencia.tipo,
			veces: habito.frecuencia.veces === null ? '' : String(habito.frecuencia.veces)
		};
		estado.errores = { campos: {}, mensaje: '' };
	}

	/** Vuelve al alta: el formulario no conserva nada de la edición abandonada. */
	function cancelar(): void {
		limpiarFormulario();
	}

	/** Guarda la corrección y deja el hábito actualizado en su lista, sin recargar la página. */
	async function guardarEdicion(): Promise<void> {
		const id = estado.idEnEdicion;

		if (id === null) {
			return;
		}

		estado.enviando = true;
		estado.errores = { campos: {}, mensaje: '' };

		try {
			const habitoEditado = await editarHabito(id, datosDelFormulario());

			estado.activos = reemplazar(habitoEditado, estado.activos);
			estado.archivados = reemplazar(habitoEditado, estado.archivados);

			limpiarFormulario();
		} catch (error) {
			// El mapeo es el mismo del alta, así que los cuatro códigos de la edición ya tienen campo.
			estado.errores = mapearErroresDeFormulario(error, CAMPOS_POR_CODIGO_DE_HABITO);
		} finally {
			estado.enviando = false;
		}
	}

	return { estado, crear, prepararEdicion, cancelar, guardarEdicion };
}
