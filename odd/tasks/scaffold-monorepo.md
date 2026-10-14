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
