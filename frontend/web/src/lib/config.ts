// URL base de la WebAPI desacoplada. Único punto de cambio de la configuración del SPA.
//
// Nota de migración: SvelteKit 3 reemplazó las variables de entorno por una declaración explícita
// en `src/env.ts` con `defineEnvVars`, expuestas desde `$app/env/public`. El módulo de SvelteKit 2,
// `$env/dynamic/public`, sigue existiendo pero sólo como shim: emite una advertencia de
// deprecación en runtime y ya no recibe declaraciones de tipo, así que `svelte-check` lo rechaza.
//
// Este scaffold no adopta todavía la API nueva. La migración se evalúa en el hito de interfaz,
// cuando haya un entorno donde se pueda verificar el valor en el navegador.
// Ver odd/tasks/scaffold-monorepo.md, L9.
/**
 * La API corre en el mismo equipo que el SPA, así que su host es el de la página: `localhost` en la
 * PC y la IP de la PC cuando el SPA se abre desde el celular (`pnpm dev:web -- --host`). Si el host
 * quedara fijo en `localhost`, desde el celular apuntaría al propio celular. El puerto no se adivina.
 */
export function urlDeApi(host: string): string {
	return `http://${host}:5256`;
}

const host = typeof location === 'undefined' ? 'localhost' : location.hostname;

export const API_URL = urlDeApi(host);

/**
 * Las rutas del almacén (`/uploads/avatares/…`) viajan relativas porque la base no sabe de puertos.
 * Acá se vuelven absolutas contra la API: un `<img>` no puede resolverlas contra el origen del SPA.
 */
export function urlDeArchivo(ruta: string): string {
	return `${API_URL}${ruta}`;
}
