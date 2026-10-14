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
export const API_URL = 'http://localhost:5256';
