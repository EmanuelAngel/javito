// Módulo de perfil: sólo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni validación
// de runtime: el contrato lo define la WebAPI, que es nuestra.

import type { Usuario } from '../sesion.svelte';
import { pedir } from './pedir';

/** Tope de la imagen, el mismo que aplica el servidor: acá sirve para no subir 20 MB al pedo. */
export const TAMANO_MAXIMO_DEL_AVATAR = 5 * 1024 * 1024;

/** Formatos que acepta la API: la lista sale de las firmas que reconoce el almacén. */
export const FORMATOS_DEL_AVATAR = 'image/jpeg,image/png,image/webp';

/**
 * Sube el avatar y devuelve la cuenta ya actualizada, para que el header se repinte sin recargar
 * (HU-PER-02). El archivo viaja como `FormData`: el `Content-Type` multipart lo arma el navegador.
 */
export function subirAvatar(archivo: File): Promise<Usuario> {
	const formulario = new FormData();
	formulario.append('archivo', archivo);

	return pedir<Usuario>('/api/perfil/avatar', { method: 'POST', body: formulario });
}
