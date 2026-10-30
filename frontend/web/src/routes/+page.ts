import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';

// La raíz no tiene contenido propio: hoy el arranque es /hoy (S9 del acc-02).
export const load: PageLoad = () => {
	redirect(302, '/hoy');
};
