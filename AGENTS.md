# Javito — instrucciones para agentes

Monorepo de una sola entrega: WebAPI **ASP.NET Core (.NET 10)** en `backend/` + SPA **SvelteKit** en
`frontend/web/`, con **MySQL 8.0** y **JWT**. Un solo repositorio Git, en la raíz. Todo corre en
`localhost`; no hay despliegue.

Alcance completo, cronograma y consignas: `docs/`. Los documentos de trabajo en curso: `odd/tasks/`.

## Mapa

| Ruta | Qué es |
| :--- | :--- |
| `backend/Javito.slnx` | solución .NET |
| `backend/Javito.Api/` | WebAPI: `Program.cs`, `Controllers/` |
| `backend/Javito.Api.Tests/` | xUnit + `Microsoft.AspNetCore.Mvc.Testing` |
| `backend/scripts/` | `check.sh`, `format.sh`, `lint.sh` |
| `frontend/web/` | SPA SvelteKit con `adapter-static` |
| `database.sql` | estructura + datos semilla (S2, pendiente) |
| `docs/` | consignas de cátedra, propuesta, cronograma, brief de UX |
| `odd/tasks/` | documentos de feature del trabajo trackeado |

## Comandos

```sh
# Backend
./backend/scripts/check.sh                                  # dotnet build + dotnet test
./backend/scripts/format.sh                                 # aplica formato
./backend/scripts/lint.sh                                   # verifica formato sin escribir
dotnet run --project backend/Javito.Api --launch-profile http

# Frontend
pnpm --dir frontend/web dev                                 # http://localhost:5173
pnpm --dir frontend/web check                               # svelte-check
pnpm --dir frontend/web lint                                # prettier --check + eslint
pnpm --dir frontend/web format                              # prettier --write
pnpm --dir frontend/web test:unit                           # vitest (watch)
pnpm --dir frontend/web exec vitest run                     # vitest una vez
pnpm --dir frontend/web build
```

Las pruebas del backend usan `dotnet test`. El frontend tiene vitest (projects `client` y `server`) y
playwright, pero **todavía no hay pruebas de e2e**: necesitan la WebAPI con MySQL, que llega con S2. Los
navegadores de playwright se instalan una vez con `pnpm --dir frontend/web test:e2e:install`.

Prettier y eslint viven en el frontend, no en la raíz: la raíz conserva husky, commitlint y lint-staged,
y `lint-staged` delega en el prettier del frontend. Por eso no hay `.prettierrc` en la raíz ni scripts
de formato propios.

## Puertos y vínculo entre capas

- WebAPI: `http://localhost:5256` (perfil `http` de `launchSettings.json`).
- SPA: `http://localhost:5173`.
- El backend permite exactamente ese origen en `Cors:AllowedOrigins`
  (`backend/Javito.Api/appsettings.Development.json`). La política es fail-closed: sin orígenes
  configurados no habilita ninguno.
- La URL de la API vive en un único lugar: `frontend/web/src/lib/config.ts`.
- Endpoint de humo: `GET /api/health`.

## Convenciones

- **Commits**: Conventional Commits **en español**, verbo en infinitivo, minúsculas, sin punto final
  (`feat(api): agregar repositorio de hábitos`). Lo verifica `commitlint` en el hook `commit-msg`.
- **Idioma**: español para nombres de prueba, comentarios y el contrato JSON de la API (`estado`,
  `servicio`), coherente con el dominio en español del DER; inglés para identificadores que siguen
  convenciones del framework (`HealthController`, `Program`).
- **Pruebas**: cada regla nueva llega con su prueba, y se observa RED antes del fix. Una prueba por
  regla, verificando todos sus efectos observables.
- **Unidades de trabajo**: cada commit es una unidad revisable y lleva el código junto a sus pruebas
  y su documentación.
- **Sin ORM**: acceso a datos con ADO.NET puro (`MySqlConnector`), SQL a mano.

## Interfaz y estilo

- **Tokens Kanagawa** en `frontend/web/src/routes/layout.css`: capa de marca con la paleta cruda y capa
  semántica con los roles de shadcn, mapeados a utilidades en `@theme inline`. Claro (Lotus) en `:root`
  y oscuro (Dragon) en `.dark`. **No usar colores sueltos** (`bg-blue-500`): usar los tokens
  (`bg-background`, `text-muted-foreground`, `border-border`, …).
- `frontend/web/src/lib/componentes/ui/` es **vendorizada** de shadcn-svelte: la escribe el CLI. Queda
  fuera de prettier y de eslint, pero **sí** entra en `svelte-check`. No la reformatees a mano.
- Las utilidades y variantes que esos componentes necesitan llegan por
  `@import 'shadcn-svelte/tailwind.css'`. No crees un segundo archivo global de CSS.
- El claro/oscuro se resuelve con el script previo al pintado de `frontend/web/src/app.html` y un botón
  en la página. No se usa `mode-watcher`.

## Trampas conocidas de SvelteKit 3

Verificado a golpes en este repo; no está en la doc evidente.

- **`$lib` fue eliminado.** El error es `module_removed_lib`. Este repo lo conserva con la opción
  `alias: { $lib: 'src/lib' }` del plugin de SvelteKit en `vite.config.ts`, que el propio kit documenta
  en ese error y que está **deprecada a propósito**: sacarla rompe el build y shadcn-svelte importa
  `$lib` en todo lo que genera. Las advertencias `config_option_deprecated_alias` y `tsconfig_invalid`
  de `pnpm check` y `pnpm build` son consecuencia esperada de esa decisión, no un error a arreglar.
  `$lib` también está mapeado en `paths` de `tsconfig.json`, que es donde el CLI de shadcn lo resuelve
  (no hay `svelte.config.js`).
- **El alias `#lib`** (subpath imports de `frontend/web/package.json`) existe para los assets, pero
  `#lib/*` no resuelve en TypeScript si el especificador no lleva extensión. Para módulos `.ts`, usá
  import relativo.
- **El patrón `#lib/*` no resuelve en TypeScript** si el especificador no lleva extensión.
  `#lib/assets/favicon.svg` funciona porque la tiene; para un módulo `.ts`, usá import relativo.
- **`$env/*` fue reemplazado** por `src/env.ts` + `defineEnvVars`, expuesto desde `$app/env/public`.
  `$env/dynamic/public` sobrevive como shim sin tipos y con advertencia de deprecación. Hoy la
  configuración vive en `src/lib/config.ts`.
- **La config del adapter está en `vite.config.ts`**, no en `svelte.config.js`.
- **`pnpm build` verde no implica `pnpm check` verde.** Corré los dos.

## Alcance y límite

No es offline ni app móvil nativa · sin integración con calendarios externos · sin notificaciones
push ni temporizadores. El módulo de artículos es un agregado para cubrir los requisitos de cátedra
de manejo de archivos y funciones por rol.
