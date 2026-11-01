import type { PageLoad } from './$types';
import { obtener } from '$lib/api/dias';
import { fechaDeHoy } from '$lib/fechas';

export const ssr = false;

export const load: PageLoad = async () => {
	return { dia: await obtener(fechaDeHoy()) };
};
