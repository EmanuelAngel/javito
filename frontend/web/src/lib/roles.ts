import { redirect } from '@sveltejs/kit';
import type { Usuario } from './sesion.svelte';

export const ROL_ADMINISTRADOR = 'Administrador';

/**
 * Guard de las pantallas que sólo usa el Administrador. Se llama al inicio del `load`, antes de
 * pedir nada a la API: el `Usuario` rebota a /hoy en vez de ver una pantalla cuyas escrituras el
 * backend rechaza con 403. Lee el usuario fresco que expone el layout del grupo (app).
 */
export async function exigirAdministrador(
	parent: () => Promise<{ usuario: Usuario }>
): Promise<void> {
	const { usuario } = await parent();

	if (usuario.rol !== ROL_ADMINISTRADOR) {
		redirect(302, '/hoy');
	}
}
