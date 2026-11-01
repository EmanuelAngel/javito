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
