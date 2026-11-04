// Módulo del día: sólo declara el contrato y envuelve `pedir`. La fecha viaja en la URL porque la
// sabe el navegador, no el servidor: el día del usuario depende de su huso horario.

import { pedir } from './pedir';

export type TipoDeHabito = 'si_no' | 'contador' | 'abstinencia';

export type Frecuencia = {
	tipo: 'diaria' | 'semanal';
	veces: number | null;
};

export type Semana = {
	cumplidos: number;
	objetivo: number;
	// Los siete días de la semana en curso, de lunes a domingo. Los que todavía no llegaron viajan
	// sin completar, igual que un día pasado sin marcar: distinguirlos es de la pantalla.
	dias: DiaDeLaSemana[];
};

export type DiaDeLaSemana = {
	fecha: string;
	completado: boolean;
};

export type RegistroDelDia = {
	valor: number;
	completado: boolean;
};

export type HabitoDelDia = {
	id: number;
	titulo: string;
	categoria: string | null;
	tipo: TipoDeHabito;
	meta: number | null;
	unidad: string | null;
	frecuencia: Frecuencia;
	semana: Semana;
	registro: RegistroDelDia | null;
};

export type Dia = {
	fecha: string;
	habitos: HabitoDelDia[];
};

export function obtener(fecha: string): Promise<Dia> {
	return pedir<Dia>(`/api/dias/${fecha}`);
}

/** Lo que devuelve un toque: el servidor fija la meta y deriva el completado. */
export type RegistroGuardado = {
	valor: number;
	metaObjetivo: number;
	completado: boolean;
};

/** Crea o reemplaza la marca del par (hábito, fecha). Idempotente: reintentar no duplica. */
export function marcar(habitoId: number, fecha: string, valor: number): Promise<RegistroGuardado> {
	return pedir<RegistroGuardado>(`/api/habitos/${habitoId}/registros/${fecha}`, {
		method: 'PUT',
		body: JSON.stringify({ valor })
	});
}

/** Desmarca: el registro deja de existir, no queda en cero. */
export function desmarcar(habitoId: number, fecha: string): Promise<void> {
	return pedir<void>(`/api/habitos/${habitoId}/registros/${fecha}`, { method: 'DELETE' });
}
