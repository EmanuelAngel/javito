// Estado del día con lo que REG-01 pide: el toque se pinta al instante y viaja en segundo plano. Si
// el pedido falla, la pantalla nunca se queda mostrando el estado optimista: manda el servidor.

import { desmarcar, marcar, obtener, type Dia, type HabitoDelDia } from './api/dias';

const AVISO = 'No se pudo guardar. Probá de nuevo.';

export type EstadoDelDia = ReturnType<typeof estadoDelDia>;

export function estadoDelDia(inicial: Dia) {
	let dia = $state(inicial);
	let aviso = $state('');

	function pintar(habito: HabitoDelDia, valor: number | null): void {
		dia = {
			...dia,
			habitos: dia.habitos.map((actual) =>
				actual.id === habito.id ? conValor(actual, valor) : actual
			)
		};
	}

	// Un solo envío en vuelo por hábito, y el último valor pedido esperando turno. Sin esto, dos
	// toques seguidos dejan dos PUT compitiendo y la base se queda con el que llega último, no con el
	// que se tocó último (issue #42).
	// No son estado de la interfaz —no se leen en la plantilla, sólo ordenan los envíos— así que no
	// hace falta que sean reactivos.
	// eslint-disable-next-line svelte/prefer-svelte-reactivity
	const enVuelo = new Set<number>();
	// eslint-disable-next-line svelte/prefer-svelte-reactivity
	const pendiente = new Map<number, number | null>();

	async function enviar(habito: HabitoDelDia, valor: number | null, anterior: Dia): Promise<void> {
		try {
			if (valor === null) {
				await desmarcar(habito.id, dia.fecha);
			} else {
				await marcar(habito.id, dia.fecha, valor);
			}
		} catch {
			// Revertir a ciegas sería mentir al revés: un timeout puede haber guardado del otro lado.
			// Primero se pregunta qué quedó; si ni eso se puede, se vuelve a la foto anterior al toque.
			aviso = AVISO;

			try {
				dia = await obtener(anterior.fecha);
			} catch {
				dia = anterior;
			}
		}
	}

	async function guardar(habito: HabitoDelDia, valor: number | null): Promise<void> {
		// La foto de antes del primer toque: si todo falla, es a donde se vuelve.
		const anterior = dia;
		aviso = '';
		pintar(habito, valor);
		pendiente.set(habito.id, valor);

		// Ya hay un envío en curso para este hábito: el bucle de abajo va a tomar este valor cuando
		// termine, así que este toque no gasta un pedido propio.
		if (enVuelo.has(habito.id)) return;

		enVuelo.add(habito.id);

		try {
			let siguiente = pendiente.get(habito.id);

			while (siguiente !== undefined) {
				pendiente.delete(habito.id);
				await enviar(habito, siguiente, anterior);
				siguiente = pendiente.get(habito.id);
			}
		} finally {
			enVuelo.delete(habito.id);
		}
	}

	return {
		get dia(): Dia {
			return dia;
		},

		get aviso(): string {
			return aviso;
		},

		/** Marca o desmarca un sí/no o una abstinencia: un solo toque, sin formulario. */
		alternar(habito: HabitoDelDia): Promise<void> {
			return guardar(habito, habito.registro ? null : 1);
		},

		subir(habito: HabitoDelDia): Promise<void> {
			return guardar(habito, (habito.registro?.valor ?? 0) + 1);
		},

		/** En cero el `−` no hace nada: no hay marca negativa ni pedido que gastar. */
		bajar(habito: HabitoDelDia): Promise<void> {
			const actual = habito.registro?.valor ?? 0;

			return actual <= 0 ? Promise.resolve() : guardar(habito, actual - 1);
		},

		descartarAviso(): void {
			aviso = '';
		}
	};
}

/** La fila con su marca nueva y el conteo semanal movido un lugar, sin volver a pedir el día. */
function conValor(habito: HabitoDelDia, valor: number | null): HabitoDelDia {
	const meta = habito.meta ?? 1;
	const antes = habito.registro?.completado ?? false;
	const registro = valor === null ? null : { valor, completado: valor >= meta };
	const despues = registro?.completado ?? false;
	const salto = despues === antes ? 0 : despues ? 1 : -1;

	return {
		...habito,
		registro,
		semana: { ...habito.semana, cumplidos: Math.max(0, habito.semana.cumplidos + salto) }
	};
}
