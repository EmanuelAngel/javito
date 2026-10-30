/**
 * Acepta el `?destino=` crudo y devuelve una ruta interna segura: sólo rutas que empizan con `/`
 * y no con `//` (protocolo relativo) ni `/\` (barra invertida). Cualquier otra cosa —una URL
 * absoluta, `//otro.host`, `/\otro.host`, un valor vacío— vuelve a `/hoy`.
 */
export function destinoSeguro(valor: string | null): string {
	if (!valor) return '/hoy';

	if (valor.startsWith('/') && !valor.startsWith('//') && !valor.startsWith('/\\')) {
		return valor;
	}

	return '/hoy';
}

/**
 * La URL del login recordando a dónde iba la persona (?destino=, S10). El guard del grupo (app)
 * la escribe en cada redirect: el formato vive acá y no repartido por las rutas.
 */
export function rutaDeLogin(destino: string): string {
	return `/login?destino=${encodeURIComponent(destino)}`;
}
