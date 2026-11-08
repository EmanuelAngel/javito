import { describe, expect, it } from 'vitest';
import type {
	DetalleDelHistorial,
	DiaDelHistorial,
	EstadoDelDiaDelHistorial
} from './api/historial';
import { cuadriculaDeDias, totalesDelDetalle } from './cuadricula';

// #83, S7 y S8: la cuadrícula del detalle y sus totales. Se prueban sin montar la pantalla, igual
// que la fila del historial, para que el texto que se lee quede fijado acá.

function unDia(estado: EstadoDelDiaDelHistorial, fecha = '2001-06-04'): DiaDelHistorial {
	return { fecha, estado };
}

function unDetalle(parcial: Partial<DetalleDelHistorial> = {}): DetalleDelHistorial {
	return {
		habitoId: 15,
		habito: 'Leer',
		tipo: 'contador',
		frecuencia: { tipo: 'diaria', veces: null },
		dias: 30,
		desde: '2001-06-01',
		hasta: '2001-06-30',
		cumplidos: 12,
		evaluables: 30,
		porcentaje: 40.0,
		cuadricula: [unDia('cumplido')],
		...parcial
	};
}

describe('la cuadrícula del detalle', () => {
	it('distingue los tres estados sin depender del color (S8)', () => {
		const [cumplido] = cuadriculaDeDias([unDia('cumplido')]);
		const [vacio] = cuadriculaDeDias([unDia('vacio')]);
		const [neutro] = cuadriculaDeDias([unDia('neutro')]);

		// Las tres marcas son distintas entre sí: la celda no se apoya en el color.
		expect(new Set([cumplido.marca, vacio.marca, neutro.marca]).size).toBe(3);

		// Y la etiqueta dice el estado y la fecha en español, así lo lee el lector de pantalla.
		expect(cumplido.etiqueta).toBe('lunes 4 de junio: cumplido');
		expect(vacio.etiqueta).toBe('lunes 4 de junio: vacío');
		expect(neutro.etiqueta).toBe('lunes 4 de junio: neutro');
	});

	it('arma una celda por día, en el orden en que llegaron', () => {
		const celdas = cuadriculaDeDias([
			unDia('cumplido', '2001-06-01'),
			unDia('vacio', '2001-06-02'),
			unDia('neutro', '2001-06-03')
		]);

		expect(celdas.map((celda) => celda.fecha)).toEqual(['2001-06-01', '2001-06-02', '2001-06-03']);
		expect(celdas.map((celda) => celda.estado)).toEqual(['cumplido', 'vacio', 'neutro']);
	});
});

describe('los totales del detalle', () => {
	it('habla de días en un hábito diario (S7)', () => {
		expect(totalesDelDetalle(unDetalle())).toEqual({
			total: '12 de 30 días cumplidos',
			porcentaje: '40 %'
		});
	});

	it('habla de semanas con la cuota en uno semanal (S7, L4)', () => {
		expect(
			totalesDelDetalle(
				unDetalle({
					frecuencia: { tipo: 'semanal', veces: 3 },
					cumplidos: 3,
					evaluables: 5,
					porcentaje: 60.0
				})
			)
		).toEqual({ total: '3 de 5 semanas con la cuota', porcentaje: '60 %' });
	});

	it('escribe el porcentaje como en casa: con coma decimal y con el signo (S7)', () => {
		expect(
			totalesDelDetalle(unDetalle({ cumplidos: 10, evaluables: 30, porcentaje: 33.3 }))
		).toEqual({ total: '10 de 30 días cumplidos', porcentaje: '33,3 %' });
	});

	it('sin días evaluables lo dice en vez de mostrar un 0 % (L5)', () => {
		expect(
			totalesDelDetalle(unDetalle({ cumplidos: 0, evaluables: 0, porcentaje: 0, cuadricula: [] }))
		).toEqual({ total: 'todavía no hay días evaluables', porcentaje: null });
	});
});
