# Scaffold del monorepo Javito

Feature del hito **S1** del cronograma (`docs/proyecto/cronograma.md`), más la higiene de repo y
entorno de agente acordada antes del primer write. Rama: `feat/scaffold-monorepo`.
Punto de retorno: commit inicial `2f5d818` en `main`.

## Specs

- **S1 — Alcance de la tanda.** El usuario eligió: "S1 + higiene de repo y agente".
  Raíz: `git init`, `.gitignore`, `AGENTS.md`, `package.json` (tooling), `husky`, `lint-staged`,
  `.pi/{settings,mcp}.json`, `odd/tasks/`. `backend/`: `Javito.slnx`, `Javito.Api` (OpenAPI+CORS),
  `Javito.Api.Tests`, `scripts/`, `wwwroot/uploads/`. `frontend/web/`: SvelteKit `adapter-static`
  + 1 ruta que consume la API. Más el fix de las 2 inconsistencias del `README.md`.
- **S2 — Repo Git.** Un solo repositorio en la raíz. `main` conserva el snapshot previo al scaffold
  como punto de retorno; el scaffold se desarrolla en `feat/scaffold-monorepo` con un commit por
  unidad de trabajo, en Conventional Commits español, verbo en infinitivo, minúsculas y sin punto
  final (convención del `README.md`: `feat(repo): agregar repositorio de hábitos`).
- **S3 — Entregable S1 del cronograma.** "`backend/` (WebAPI .NET 10 con OpenAPI), `frontend/`
  (SvelteKit `adapter-static`), `.gitignore`, repo Git inicializado." SDK disponible: .NET 10.0.302,
  Node 24.20, pnpm 10.26.1, MySQL 8.0.46.
- **S4 — Tooling Node.** Dos `package.json` sueltos: raíz con tooling (commitlint, lint-staged,
  husky) y `frontend/web/` con las dependencias de SvelteKit. Sin pnpm workspace.
- **S5 — Conexión dev.** CORS directo: el SPA consume `http://localhost:<puerto>` con CORS
  configurado en el backend. Sin proxy de Vite.
- **S6 — Skills en git.** Del árbol de skills entra sólo `skills-lock.json`. `.agents/skills/` y
  `.atl/` quedan fuera del control de versiones.
- **S7 — Inconsistencias de documentación a corregir.** Cantidad de tablas: "Javito define **8**"
  en `docs/consignas/consigna-y-definiciones.md` contradice las **7** del `README.md`,
  `docs/proyecto/propuesta.md` y la ficha técnica; manda 7. Rutas del backend: `backend/src/Auth` y
  `backend/src/Controllers` de la Matriz de Evidencias contradicen el árbol del `README.md`; manda
  `backend/Javito.Api/…`.
- **S8 — Criterio de cierre.** `dotnet build` y `dotnet test` verdes; `pnpm build` del frontend
  verde; el SPA obtiene datos del backend en el navegador vía CORS; `git status` limpio respecto de
  `bin/`, `obj/`, `.svelte-kit/` y `node_modules/`; README con la sección "Puesta en marcha"
  completada.

## Tasks

| ID | Tarea | Specs | Ruta | Commit |
| :--- | :--- | :--- | :--- | :--- |
| T1 | Scaffold del backend: solución, WebAPI con OpenAPI y CORS, proyecto de tests, `scripts/`, `wwwroot/uploads/` | S3, S8 | inline | `feat(api): scaffoldear WebAPI .NET 10 con OpenAPI y CORS` |
| T2 | Scaffold del frontend: SvelteKit con `adapter-static` y una ruta que consume la API | S3, S5, S8 | inline | `feat(web): scaffoldear SPA SvelteKit con adapter-static` |
| T3 | Higiene de raíz: `AGENTS.md`, `package.json` de tooling, husky + lint-staged, `.pi/` | S1, S2, S4 | inline | `chore(repo): agregar convenciones de agente y verificación de commits` |
| T4 | Corrección de las dos inconsistencias documentales | S7 | inline | `docs(readme): corregir cantidad de tablas y rutas del backend` |
| T5 | Verificación end-to-end y "Puesta en marcha" del README | S8 | inline | `docs(readme): documentar la puesta en marcha del monorepo` |

## Log

### L1 — Pedido original

> Una cosa nada más: ¿Podés hacer un git init e initial commit ahora? Por si quiero volver atrás. Ahí sí mandate a hacer el scaffold.

### L2 — Decisiones de alcance

Respuestas del usuario a las tres preguntas de planificación:

1. Alcance: **"S1 + higiene de repo y agente"**.
2. Tooling Node: **"Dos sueltos: raíz + frontend/web"**.
3. Conexión dev: **"CORS directo"**.

### L3 — Decisión sobre skills

> solo el lock y asegurate que .atl/ tampoco se suba

### L4 — Snapshot inicial

`git init` en la raíz con rama `main`; commit `2f5d818 chore(repo): inicializar repositorio con
documentación base` (14 archivos, 911 líneas). `.gitignore` extendido con `.agents/skills/` y
`.atl/`. Verificado con `git check-ignore` que ninguna de las dos rutas entra al índice.

### L5 — Decisión sobre las inconsistencias (S7)

Se aplica la opción consistente con la mayoría de la documentación: **7 tablas** (el rol no se
guarda en tabla, según `propuesta.md` §2.2 y §3.4) y **`backend/Javito.Api/…`** (el árbol del
`README.md`). Se corrige la línea divergente, no las mayoritarias. Reversible en un commit si el
usuario prefiere lo contrario.

### L6 — Evidencia test-first de T1

La regla que cubre T1 es la de S8: el SPA obtiene datos del backend por la API con CORS por origen.
Las dos pruebas se escribieron **antes** de la implementación. Única infraestructura previa:
`public partial class Program { }`, porque la clase `Program` de las instrucciones de nivel superior
es interna y el proyecto de pruebas necesita alojar la API.

- **RED** (`job-4`): `/api/health` → `404 NotFound`; respuesta sin cabecera
  `Access-Control-Allow-Origin`. **2 fallos, 1 pasa** (el test vacío de la plantilla).
- **GREEN** (`job-5`): **2 pasan, 0 fallan**.

Se eliminó `UnitTest1.cs`, que no asertaba nada.

### L7 — Hallazgo de seguridad durante la verificación

El build de GREEN emitió `NU1903`: `Microsoft.OpenApi` **2.0.0**, vulnerabilidad de gravedad **alta**
(GHSA-v5pm-xwqc-g5wc), arrastrada de forma transitiva por `Microsoft.AspNetCore.OpenApi` **10.0.10**,
la versión que fija la plantilla del SDK.

Corrección: bump de `Microsoft.AspNetCore.OpenApi` a **10.0.13**, que declara
`Microsoft.OpenApi [2.12.2, 3.0.0)`. Re-auditoría con
`dotnet list package --vulnerable --include-transitive` → "no tiene paquetes vulnerables".

`scripts/check.sh`: 0 advertencias, 0 errores, 2/2 pruebas (`job-6`, exit 0).
`scripts/lint.sh`: limpio (`job-6`, exit 0).

### L8 — Convención de idioma

El proyecto no tenía código, así que no había convención que extender. Se adopta: **español** para
nombres de prueba, comentarios y el contrato JSON de la API (`estado`, `servicio`), coherente con el
dominio en español del DER (`Usuario`, `Habito`, `titulo`) y con la defensa oral; **inglés** para los
identificadores que siguen convenciones del framework (`HealthController`, `Program`). Reversible.

### L9 — Hallazgos de SvelteKit 3 durante T2

El build pasaba, pero `svelte-check` quedaba en rojo. Con SvelteKit 3 eso son dos cosas distintas y
hay que correr las dos. Tres cambios de API, ninguno obvio en la doc:

1. **`$lib` fue eliminado.** Error literal: `SvelteKit error: module_removed_lib` — "`$lib` has been
   removed. Use `#lib` instead". El reemplazo son subpath imports de Node en `package.json`:
   `{"#lib": "./src/lib/index.js", "#lib/*": "./src/lib/*"}`.
2. **`$env/*` fue reemplazado** por una declaración explícita en `src/env.ts` con `defineEnvVars` de
   `@sveltejs/kit/env`, expuesta desde `$app/env/public`. El módulo viejo, `$env/dynamic/public`,
   sobrevive como shim que emite `env_module_deprecated` en runtime y **ya no recibe tipos**.
3. **El patrón `#lib/*` no resuelve en TypeScript sin extensión en el especificador.**
   `#lib/assets/favicon.svg` funcionaba porque la extensión estaba en el import; `#lib/config` lo
   resuelve el bundler pero no `tsc`.

Además: `svelte.config.js` ya no existe. `sv create` deja el adapter dentro de `vite.config.ts`, en
`sveltekit({ adapter })`, y el fallback SPA se activa con `adapter({ fallback: 'index.html' })`.

### L10 — Decisión sobre variables de entorno del SPA

SvelteKit 3 ya no tiene una forma de entorno tipada y verificable para este caso sin adoptar
`src/env.ts`. Se intentó esa vía y **se descartó para el scaffold**: la forma exacta del módulo
`$app/env/public` sólo se pudo confirmar por código generado, y la verificación real requiere un
navegador.

Decisión: la URL de la API queda en `src/lib/config.ts` como **único punto de cambio**, documentada
con la nota de migración. No se acopla el scaffold a una API cuya forma no está verificada, y la
migración se evalúa en el hito de interfaz, cuando se pueda verificar el valor en el navegador.

### L11 — `.env.example` bloqueado por política

Se intentó crear `frontend/web/.env.example`. La política de seguridad bloquea esa ruta por patrón,
y **el bloqueo persistió aun con autorización explícita del usuario**: no es falta de consentimiento,
es una restricción de herramienta. No se rodeó el guardrail por otra vía.

El archivo quedó **innecesario**: al no adoptarse la API de entorno de SvelteKit 3, no hay variables
que documentar. La configuración vive en `src/lib/config.ts`.

### L12 — Evidencia de T2

`pnpm check`: `svelte-check found 0 errors and 0 warnings` (exit 0). `pnpm build`: `Wrote site to
"build"` (exit 0), con `build/index.html` como fallback SPA y el bundle en `build/_app`.

### L13 — Evidencia de T3

Instalado en la raíz: husky 9.1.7, lint-staged 17.6.0, @commitlint/cli 21.2.3,
@commitlint/config-conventional 21.2.3, prettier 3.9.10, prettier-plugin-svelte 4.1.1.

`pnpm add` **no ejecuta el script `prepare` del proyecto raíz**, así que husky no se inicializó solo:
hubo que correr `pnpm run prepare` a mano para generar `.husky/_`. `.husky/_/` se auto-ignora con su
propio `.gitignore` (`*`) y no entra al repo, que es el diseño de husky v9.

Commitlint verificado en los dos sentidos: mensaje válido `exit=0`, mensaje inválido `exit=1`.
Los hooks corrieron en el commit real y se ve en la salida: `lint-staged` ejecutó
`prettier --write` sobre los archivos staged y re-stageó los cambios.

Decisiones de alcance del hook:

- `.prettierignore` excluye `backend/` (el C# se formatea con `dotnet format`) y la documentación en
  español, que está cortada a mano y prettier rearmaría. También excluye los lockfiles generados.
- El `pre-commit` **no** corre `dotnet format`: haría lento cada commit por un `dotnet build`
  completo. Queda en `backend/scripts/lint.sh` para correr a mano.

### L14 — Correcciones de T4

- `docs/consignas/consigna-y-definiciones.md`: la tabla de reconciliación decía "Javito define
  **8**" → **7**, consistente con `README.md`, `propuesta.md` y la ficha técnica.
- `README.md`, filas 2 y 3 de la Matriz de Evidencias: `backend/src/Auth` y `backend/src/Controllers`
  → `backend/Javito.Api/Auth` y `backend/Javito.Api/Controllers`, consistente con el árbol del propio
  README y con el layout ya scaffoldeado.

Se corrigieron las líneas divergentes, no las mayoritarias. Verificado por grep: no queda ninguna
aparición de `backend/src` ni de "Javito define **8**".
