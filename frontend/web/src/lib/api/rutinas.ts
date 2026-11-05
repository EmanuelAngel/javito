// Módulo de rutinas: sólo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni validación
// de runtime: el contrato lo define la WebAPI, que es nuestra.

import { pedir } from './pedir';

export type Rutina = {
	id: number;
	nombre: string;
	descripcion: string | null;
	activo: boolean;
};

/** Un hábito dentro de una rutina, con el lugar que ocupa en ella. */
export type HabitoEnRutina = {
	/** Id del hábito, no el de la relación: es lo que se manda al quitarlo. */
	id: number;
	titulo: string;
	orden: number;
};

export type RutinaDetalle = Rutina & { habitos: HabitoEnRutina[] };

/** El alta: el nombre es obligatorio y la descripción opcional (HU-RUT-01). */
export type DatosDeRutina = {
	nombre: string;
	descripcion: string | null;
};

export function listar(): Promise<Rutina[]> {
	return pedir<Rutina[]>('/api/rutinas');
}

export function detalle(id: number): Promise<RutinaDetalle> {
	return pedir<RutinaDetalle>(`/api/rutinas/${id}`);
}

export function crear(datos: DatosDeRutina): Promise<Rutina> {
	return pedir<Rutina>('/api/rutinas', {
		method: 'POST',
		body: JSON.stringify(datos)
	});
}

export function agregarHabito(rutinaId: number, habitoId: number): Promise<HabitoEnRutina> {
	return pedir<HabitoEnRutina>(`/api/rutinas/${rutinaId}/habitos`, {
		method: 'POST',
		body: JSON.stringify({ habitoId })
	});
}

/** Quitar sólo cae la relación: el hábito sigue activo (HU-RUT-02). */
export function quitarHabito(rutinaId: number, habitoId: number): Promise<void> {
	return pedir<void>(`/api/rutinas/${rutinaId}/habitos/${habitoId}`, { method: 'DELETE' });
}
