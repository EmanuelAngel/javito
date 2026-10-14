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
| `frontend/web/` | SPA SvelteKit con `adapter-static` |
| `database.sql` | estructura + datos semilla (S2, pendiente) |
| `docs/` | consignas de cátedra, propuesta, cronograma, brief de UX |
| `odd/tasks/` | documentos de feature del trabajo trackeado |
| `package.json` (raíz) | punto de entrada único de comandos: `pnpm <script>` |
| `.editorconfig` | fuente de verdad de `dotnet format`: finales de línea LF e indentación |
| `lint-staged.config.js` | reglas del hook `pre-commit` (ver "Hooks de Git") |

## Comandos

Punto de entrada único, desde la raíz. `pnpm run` lista todo.

```sh
pnpm dev:api        # WebAPI en http://localhost:5256 (dotnet run, perfil http)
pnpm dev:web        # SPA en http://localhost:5173

pnpm check          # check:api + check:web
pnpm check:api      # dotnet build + dotnet test
pnpm check:web      # svelte-check

pnpm lint           # lint:api + lint:web
pnpm lint:api       # dotnet format --verify-no-changes
pnpm lint:web       # prettier --check + eslint

pnpm format         # format:api + format:web
pnpm format:api     # dotnet format
pnpm format:web     # prettier --write

pnpm test:web       # vitest una vez
pnpm build:web      # build estático (adapter-static -> frontend/web/build)
```

`package.json` es la **única** fuente de estos comandos: no hay wrappers `.sh`. pnpm ejecuta los scripts
con `cmd.exe` en Windows, así que un `.sh` invocado desde `package.json` falla.

Las pruebas del backend usan `dotnet test`. El frontend tiene vitest (projects `client` y `server`) y
playwright, pero **todavía no hay pruebas de e2e**: necesitan la WebAPI con MySQL, que llega con S2. Los
navegadores de playwright se instalan una vez con `pnpm --dir frontend/web test:e2e:install`.

`pnpm test:web` corre vitest una vez. Los `*.test.ts` van al project `server` (entorno node) y los
`*.svelte.test.ts` al project `client`, que necesita los navegadores de playwright.

`lint:web` y `check:web` cachean a disco: `prettier --cache` (en `node_modules/.cache/prettier`),
`eslint --cache` (`frontend/web/.eslintcache`) y `svelte-check --incremental` (en `.svelte-kit`). Todo
eso está ignorado por git. Si una caché quedó mintiendo, borrá el archivo y volvé a correr. `vitest run`
no gana nada con `--cache` (medido), y `dotnet format` **no tiene caché**: `pnpm lint:api` tarda ~10s
siempre.

Prettier y eslint viven en el frontend, no en la raíz: la raíz conserva husky, commitlint y lint-staged.
Por eso no hay `.prettierrc` en la raíz: los scripts de la raíz solo delegan.

## Hooks de Git

`pre-commit` corre `lint-staged` sobre lo que está en stage, con el alcance de `pnpm lint` acotado a los
archivos que se commitean:

- `frontend/web/**` → `prettier --write` (incluye `css`, `html` y `md`) y `eslint --fix` (`ts`, `js`,
  `svelte`). Los dos con `--dir frontend/web`, porque prettier resuelve sus plugins contra el directorio
  de trabajo, no contra el de la configuración. Si el SPA suma un lenguaje (`.scss`, `.mjs`), hay que
  agregar la extensión al glob: pretty no los formatea solo por estar en el directorio.
- `backend/**/*.cs` → `dotnet format --include` sobre los archivos staged.

`dotnet format --include` **solo** acepta rutas relativas al directorio de trabajo: con una absoluta no
formatea nada y termina con código 0, sin avisar. Por eso la regla del backend es una función en
`lint-staged.config.js`, que traduce las rutas absolutas que pasa `lint-staged`. `svelte-check` y las
pruebas quedan afuera a propósito: son verificaciones de cierre (`pnpm check`), no de commit.

`commit-msg` corre `commitlint`.

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
- **Finales de línea**: LF en el repositorio y en el árbol de trabajo. Lo impone `.gitattributes`, y
  `.editorconfig` lo repite porque `dotnet format` lo lee como fuente de verdad: sin esa regla escribe
  CRLF en las líneas que corrige y el archivo queda sucio para git.
- **Unidades de trabajo**: cada commit es una unidad revisable y lleva el código junto a sus pruebas
  y su documentación.
- **Sin ORM**: acceso a datos con ADO.NET puro (`MySqlConnector`), SQL a mano.

## Interfaz y estilo

- **Temas Rosé Pine** en `frontend/web/src/routes/layout.css`, copiados de Pomody: capa de marca con la
  paleta cruda y capa semántica con los roles de shadcn, mapeados a utilidades en `@theme inline`. Claro
  (Dawn) en `:root` y oscuro (OLED) en `.dark`. **No usar colores sueltos** (`bg-blue-500`): usar los
  tokens (`bg-background`, `text-muted-foreground`, `border-border`, …).
- `frontend/web/src/lib/componentes/ui/` es **vendorizada** de shadcn-svelte: la escribe el CLI. Queda
  fuera de prettier y de eslint, pero **sí** entra en `svelte-check`. No la reformatees a mano.
- Las utilidades y variantes que esos componentes necesitan llegan por
  `@import 'shadcn-svelte/tailwind.css'`. No crees un segundo archivo global de CSS.
- **Modo claro/oscuro con `mode-watcher`**: `<ModeWatcher />` en `+layout.svelte` aplica y saca la clase
  `.dark` en el `<html>`, sigue la preferencia del sistema operativo y la persiste. El botón de la página
  usa `toggleMode`. No hay script propio en `app.html`.
- **Íconos: Lucide** (`@lucide/svelte`, con `"iconLibrary": "lucide"` en `components.json`). Un solo pack,
  sin shim ni alias.

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
