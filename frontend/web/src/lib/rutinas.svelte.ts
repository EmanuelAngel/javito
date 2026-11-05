// Estado de la pantalla /rutinas: la lista de rutinas propias, la que está elegida con sus hábitos y
// el buscador asíncrono con el que se agregan (HU-RUT-02). Sigue el molde de habitos.svelte.ts: el
// módulo es el dueño del estado y la pantalla lo consume.

import { buscar as buscarHabitos, type Habito } from './api/habitos';
import { mapearErroresDeFormulario, type ErroresDeFormulario } from './api/errores';
import {
	agregarHabito,
	crear as crearRutina,
	detalle as detalleDeRutina,
	quitarHabito,
	type Rutina,
	type RutinaDetalle
} from './api/rutinas';
import { crearBusqueda } from './busqueda';

type EstadoInicial = {
	rutinas: Rutina[];
	detalle: RutinaDetalle | null;
};

export type EstadoDeRutinas = ReturnType<typeof estadoDeRutinas>;

export function estadoDeRutinas(inicial: EstadoInicial) {
	const estado = $state({
		rutinas: inicial.rutinas,
		detalle: inicial.detalle,
		enviando: false,
		errores: { campos: {}, mensaje: '' } as ErroresDeFormulario,
		formulario: { nombre: '', descripcion: '' },
		busqueda: {
			texto: '',
			resultados: [] as Habito[],
			buscando: false,
			mensaje: ''
		}
	});

	// La espera es corta a propósito: la historia pide buscar mientras se escribe y el pedido sale al
	// servidor, no sobre una lista ya bajada.
	const buscador = crearBusqueda<Habito[]>({
		espera: 250,
		buscar: (texto, signal) => buscarHabitos(texto, signal),
		alLlegar: (resultados) => {
			estado.busqueda.resultados = resultados;
			estado.busqueda.buscando = false;
		},
		alFallar: () => {
			estado.busqueda.buscando = false;
			estado.busqueda.mensaje = 'No se pudo buscar. ¿Está levantada la WebAPI?';
		}
	});

	function reiniciarBusqueda(): void {
		buscador.cancelar();
		estado.busqueda = { texto: '', resultados: [], buscando: false, mensaje: '' };
	}

	function limpiarFormulario(): void {
		estado.formulario = { nombre: '', descripcion: '' };
		estado.errores = { campos: {}, mensaje: '' };
	}

	async function cargarDetalle(rutinaId: number): Promise<void> {
		estado.detalle = await detalleDeRutina(rutinaId);
	}

	/** Alta (HU-RUT-01): la rutina nace activa y vacía, y queda elegida para cargarle hábitos. */
	async function crear(): Promise<void> {
		const nombre = estado.formulario.nombre.trim();
		const descripcion = estado.formulario.descripcion.trim();

		estado.enviando = true;
		estado.errores = { campos: {}, mensaje: '' };

		try {
			const creada = await crearRutina({
				nombre,
				descripcion: descripcion === '' ? null : descripcion
			});

			estado.rutinas = [...estado.rutinas, creada];
			limpiarFormulario();
			await elegir(creada.id);
		} catch (error) {
			estado.errores = mapearErroresDeFormulario(error);
		} finally {
			estado.enviando = false;
		}
	}

	async function elegir(rutinaId: number): Promise<void> {
		reiniciarBusqueda();
		await cargarDetalle(rutinaId);
	}

	/** Cada tecla pasa por acá: el debounce y el abort los resuelve `crearBusqueda`. */
	function buscar(texto: string): void {
		estado.busqueda.texto = texto;
		estado.busqueda.mensaje = '';

		const termino = texto.trim();

		if (termino === '') {
			buscador.cancelar();
			estado.busqueda.resultados = [];
			estado.busqueda.buscando = false;
			return;
		}

		estado.busqueda.buscando = true;
		buscador.pedir(termino);
	}

	async function agregar(habitoId: number): Promise<void> {
		const rutinaId = estado.detalle?.id;

		if (rutinaId === undefined) return;

		try {
			await agregarHabito(rutinaId, habitoId);
			reiniciarBusqueda();
			// El detalle se relee: el orden lo asigna el servidor y es lo que se muestra.
			await cargarDetalle(rutinaId);
		} catch (error) {
			estado.busqueda.mensaje = mensajeDe(error);
		}
	}

	async function quitar(habitoId: number): Promise<void> {
		const rutinaId = estado.detalle?.id;

		if (rutinaId === undefined) return;

		try {
			await quitarHabito(rutinaId, habitoId);
			await cargarDetalle(rutinaId);
		} catch (error) {
			estado.errores = { campos: {}, mensaje: mensajeDe(error) };
		}
	}

	return { estado, crear, elegir, buscar, agregar, quitar };
}

function mensajeDe(error: unknown): string {
	return error instanceof Error ? error.message : 'No se pudo completar la operación.';
}
