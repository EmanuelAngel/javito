# Javito · Frontend (SPA)

SPA en SvelteKit con `adapter-static`. Consume la WebAPI de `backend/` por HTTP con CORS.

Este es un paquete Node **independiente** del `package.json` de la raíz: en la raíz viven sólo las
herramientas de verificación de commits y formato.

## Comandos

```sh
pnpm install
pnpm dev      # servidor de desarrollo en http://localhost:5173
pnpm build    # build estático en build/
pnpm preview  # sirve el build
pnpm check    # svelte-check
```

## Configuración

La URL de la WebAPI vive en un único lugar: `src/lib/config.ts`.

```ts
export const API_URL = 'http://localhost:5256';
```

El valor por defecto corresponde al perfil `http` del backend
(`backend/Javito.Api/Properties/launchSettings.json`). El backend debe permitir el origen
`http://localhost:5173` en `Cors:AllowedOrigins`
(`backend/Javito.Api/appsettings.Development.json`).

### Pendiente: variables de entorno de SvelteKit 3

SvelteKit 3 reemplazó `$env/static/public` y `$env/dynamic/public` por una declaración explícita en
`src/env.ts` con `defineEnvVars`, expuestas desde `$app/env/public`. El módulo viejo sigue
funcionando como shim, pero emite una advertencia de deprecación y **ya no recibe tipos**, por lo que
`svelte-check` lo rechaza.

La migración queda pendiente para el hito de interfaz, cuando se pueda verificar el valor en el
navegador. Mientras tanto, `src/lib/config.ts` es el único punto de cambio.

## Modo SPA

`adapter-static` con `fallback: 'index.html'` y `ssr = false` en `src/routes/+layout.ts`: no hay
renderizado en servidor y el ruteo ocurre en el cliente. Ver
[docs/proyecto/propuesta.md §3.5](../../docs/proyecto/propuesta.md).

La puesta en marcha completa está en el [README raíz](../../README.md).

> Generado con `sv create --template minimal --types ts`.
