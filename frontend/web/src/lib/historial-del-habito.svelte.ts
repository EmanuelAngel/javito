// Estado del detalle histórico de un hábito (HU-HIS-02): el detalle, la carga y el error. Sigue el
// molde de hoy.svelte.ts —el módulo es dueño del estado y la pantalla compone—, así la ruta del
// detalle queda en composición.

import { detalle as pedirDetalle, type DetalleDelHistorial } from './api/historial';
import { ErrorDeApi } from './api/pedir';
import { fechaDeHoy } from './fechas';

/** El estado completo del detalle: lo que devuelve `estadoDelDetalle`. */
export type EstadoDelDetalle = ReturnType<typeof estadoDelDetalle>;

function mensajeDeError(error: unknown): string {
	return error instanceof ErrorDeApi ? error.message : 'No se pudo cargar el hábito.';
}

export function estadoDelDetalle() {
	const estado = $state({
		detalle: null as DetalleDelHistorial | null,
		cargando: true,
		mensajeError: ''
	});

	// Cambiar de período dispara un pedido nuevo sin esperar al anterior: el turno descarta la
	// respuesta que llega tarde, para que dos clics seguidos no dejen el período viejo en pantalla.
	let turno = 0;

	/**
	 * El detalle de la ventana que termina en `hasta`. Por defecto, hoy: la API pide el día del
	 * navegador porque el servidor corre en UTC (`fechaDeHoy`), igual que `/api/dias`.
	 */
	async function cargar(
		habitoId: number,
		dias: number,
		hasta: string = fechaDeHoy()
	): Promise<void> {
		const propio = ++turno;

		estado.cargando = true;
		estado.mensajeError = '';

		// Otro hábito: no se sigue mostrando el de antes mientras llega el nuevo.
		if (estado.detalle?.habitoId !== habitoId) estado.detalle = null;

		try {
			const respuesta = await pedirDetalle(habitoId, dias, hasta);

			if (propio === turno) estado.detalle = respuesta;
		} catch (error) {
			if (propio !== turno) return;

			estado.detalle = null;
			estado.mensajeError = mensajeDeError(error);
		} finally {
			if (propio === turno) estado.cargando = false;
		}
	}

	return { estado, cargar };
}
