// La cuadrícula del detalle histórico y sus totales (#83, S7/S8): lo que la pantalla pinta, ya
// escrito. Es una función pura a propósito: la celda y el total se prueban sin montar la pantalla, y
// el componente no repite formatos.

import type {
	DetalleDelHistorial,
	DiaDelHistorial,
	EstadoDelDiaDelHistorial
} from './api/historial';
import { fechaLocalDesdeIso } from './fechas';

/** Una celda lista para pintar: la marca que se ve y la etiqueta que se lee. */
export type CeldaDelHistorial = {
	/** La identidad de la celda: hay un solo día por fecha en el período. */
	clave: string;
	fecha: string;
	estado: EstadoDelDiaDelHistorial;
	/** Lo que va adentro de la celda. Distingue los tres estados sin depender del color (S8). */
	marca: string;
	/** «lunes 4 de junio: cumplido», lo que anuncia el lector de pantalla. */
	etiqueta: string;
};

/**
 * La marca de cada estado: cumplido lleva tilde, vacío un guion y neutro un punto. No es color, así
 * que la celda se lee igual en blanco y negro; la palabra la termina de decir la etiqueta y la
 * leyenda.
 */
const MARCAS: Record<EstadoDelDiaDelHistorial, string> = {
	cumplido: '✓',
	vacio: '–',
	neutro: '·'
};

const PALABRAS: Record<EstadoDelDiaDelHistorial, string> = {
	cumplido: 'cumplido',
	vacio: 'vacío',
	neutro: 'neutro'
};

/** La leyenda de la cuadrícula: las mismas tres marcas que usan las celdas, con su palabra. */
export const ESTADOS_DE_LA_CUADRICULA = (['cumplido', 'vacio', 'neutro'] as const).map(
	(estado) => ({
		estado,
		marca: MARCAS[estado],
		palabra: PALABRAS[estado]
	})
);

const FECHA_CORTA = new Intl.DateTimeFormat('es-AR', {
	weekday: 'long',
	day: 'numeric',
	month: 'long'
});

/** «lunes 4 de junio»: `es-AR` escribe una coma en el formato largo y acá la frase sigue con dos puntos. */
function fechaDeLaCelda(iso: string): string {
	return FECHA_CORTA.format(fechaLocalDesdeIso(iso)).replace(',', '');
}

/** Una celda por día, en el orden en que llegaron: el servidor ya los ordenó del más viejo al más nuevo. */
export function cuadriculaDeDias(dias: DiaDelHistorial[]): CeldaDelHistorial[] {
	return dias.map((dia) => ({
		clave: dia.fecha,
		fecha: dia.fecha,
		estado: dia.estado,
		marca: MARCAS[dia.estado] ?? MARCAS.neutro,
		etiqueta: `${fechaDeLaCelda(dia.fecha)}: ${PALABRAS[dia.estado] ?? PALABRAS.neutro}`
	}));
}

/** Los totales del período (S7, L4/L7): el total y el porcentaje hablan de la misma unidad. */
export type TotalesDelDetalle = {
	/** «12 de 30 días cumplidos», «3 de 5 semanas con la cuota», o que todavía no hay nada evaluable. */
	total: string;
	/** «40 %», o `null` cuando no hay nada evaluable: no se miente con un 0 % (L5). */
	porcentaje: string | null;
};

const PORCENTAJE = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 1 });

/**
 * El total de veces cumplido y el porcentaje del período. Un hábito diario cuenta días; uno semanal,
 * semanas que alcanzaron su cuota (L4), así que las dos cifras nunca mezclan unidades.
 */
export function totalesDelDetalle(detalle: DetalleDelHistorial): TotalesDelDetalle {
	const esSemanal = detalle.frecuencia.tipo === 'semanal';
	const { cumplidos, evaluables } = detalle;

	// Sin días evaluables no hay porcentaje que mostrar: el período empieza en la primera marca (L5).
	if (evaluables === 0) {
		return {
			total: esSemanal ? 'todavía no hay semanas evaluables' : 'todavía no hay días evaluables',
			porcentaje: null
		};
	}

	const total = esSemanal
		? `${cumplidos} de ${evaluables} ${evaluables === 1 ? 'semana con la cuota' : 'semanas con la cuota'}`
		: `${cumplidos} de ${evaluables} ${evaluables === 1 ? 'día cumplido' : 'días cumplidos'}`;

	return { total, porcentaje: `${PORCENTAJE.format(detalle.porcentaje)} %` };
}
