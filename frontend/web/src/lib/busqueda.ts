// El buscador asíncrono de HU-RUT-02: espera a que la persona deje de escribir y aborta el pedido
// anterior. Vive en un módulo propio y sin runes para poder probarlo con temporizadores falsos.

export type OpcionesDeBusqueda<T> = {
	/** Cuánto se espera desde la última tecla. */
	espera?: number;

	/** El pedido real. Recibe el `signal` para poder abortarlo. */
	buscar: (texto: string, signal: AbortSignal) => Promise<T>;

	alLlegar: (resultado: T) => void;
	alFallar?: (error: unknown) => void;
};

export type Busqueda = {
	pedir: (texto: string) => void;
	cancelar: () => void;
};

/**
 * Un pedido por pausa, nunca uno por tecla, y sólo la última respuesta puede pintar: si la primera
 * contesta después de que se escribió más, se descarta. Sin eso, la lista que se ve no corresponde
 * a lo que dice el input.
 */
export function crearBusqueda<T>({
	espera = 250,
	buscar,
	alLlegar,
	alFallar
}: OpcionesDeBusqueda<T>): Busqueda {
	let temporizador: ReturnType<typeof setTimeout> | undefined;
	let enCurso: AbortController | undefined;

	function cancelar(): void {
		clearTimeout(temporizador);
		temporizador = undefined;
		enCurso?.abort();
		enCurso = undefined;
	}

	function pedir(texto: string): void {
		cancelar();

		temporizador = setTimeout(() => {
			const propio = new AbortController();
			enCurso = propio;

			buscar(texto, propio.signal).then(
				(resultado) => {
					if (enCurso === propio) alLlegar(resultado);
				},
				(error: unknown) => {
					// Un pedido abortado porque se siguió escribiendo no es un error que mostrar.
					if (enCurso === propio) alFallar?.(error);
				}
			);
		}, espera);
	}

	return { pedir, cancelar };
}
