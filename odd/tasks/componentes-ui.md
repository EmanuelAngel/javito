# Componentes de UI: shadcn-svelte en su lugar

Adoptar shadcn-svelte como capa de presentación y refactorizar las cuatro pantallas que hoy son HTML
nativo con clases Tailwind. **No** se adopta superforms ni Zod: la validación sigue teniendo una sola
fuente, la WebAPI, y las razones están en L2.

Carpeta definitiva: **`frontend/web/src/lib/components/`** (inglés), con los vendorizados en
`components/ui/`. El alias de `components.json` se corrige para apuntar ahí; no se renombra la carpeta a
`componentes`.

## Specs

**S1 — Un solo árbol de componentes.** `components.json` declara `aliases.components: $lib/components`,
`aliases.ui: $lib/components/ui`, `aliases.utils: $lib/utils` y `aliases.hooks: $lib/hooks`. La carpeta
`frontend/web/src/lib/componentes/` no existe y no se crea. La instancia de shadcn-svelte ya está en
`devDependencies` del proyecto web (`shadcn-svelte@1.7.0`): se corre con `pnpm exec` desde
`frontend/web`, nunca con `npx`.

**S2 — Los componentes entran por CLI, no a mano.** Se agregan con
`pnpm exec shadcn-svelte@latest add button input label textarea native-select table badge alert empty separator field spinner`.
Los archivos que escribe el CLI en `components/ui/` **no se editan a mano** y no se reformatean.

**S3 — Exclusión de formato y lint.** `.prettierignore` y la configuración de eslint del proyecto web
excluyen `src/lib/components/ui/**` (hoy ignoran la ruta vieja `componentes/ui`). `svelte-check` **sí**
los revisa: un componente vendorizado roto tiene que romper el check.

**S4 — Selects nativos en formularios.** Los desplegables dentro de un formulario usan `native-select`
(un `<select>` real): conservan la validación `required` del navegador y el `selectOption` de Playwright.
`Select` de bits-ui queda para donde haga falta render personalizado de opciones, y no puede aparecer en
una pantalla que hoy tenga e2e con `selectOption` sin actualizar ese e2e en el mismo commit.

**S5 — Layout de formulario.** Todo formulario usa `Field.FieldGroup` + `Field.Field` +
`Field.FieldLabel`, y muestra el error con `Field.Field`, `data-invalid` y `aria-invalid` en el control,
más `Field.Error` inmediatamente después. Nada de `div` con `space-y-*` ni de `<label>` a mano.

**S6 — Estados de pantalla con componentes.** Los avisos usan `Alert` (con `role="alert"`), los estados
vacíos `Empty`, los estados `Badge` con `variant` y las tablas `Table`. Los colores salen de tokens
(`bg-card`, `text-muted-foreground`, `bg-primary`, `text-destructive`), nunca de colores sueltos.

**S7 — El refactor no cambia comportamiento.** El e2e existente (`categorias.e2e.ts`, `habitos.e2e.ts`)
pasa **sin modificaciones** al terminar cada slice. Si un e2e necesita cambiar, el cambio es de selector
por una decisión de componente (S4) y va explicado en el body del PR.

**S8 — Documentación alineada.** `AGENTS.md` deja de mencionar `src/lib/componentes/ui` y documenta
`src/lib/components/ui` como el lugar de los vendorizados, con la misma advertencia de no editarlos.

## Tasks

- **T1 — Paso 0: el alias y los componentes** (`S1`–`S3`) · inline · rama `feat/ui-componentes` ·
  commit `chore(web): mover el alias de shadcn a la carpeta de componentes` · **Hecho** en `899d9d6`
  (issue #28). El commit se llamó `chore(web): mover el alias de shadcn a components y vendorizar los
  componentes base`.
- **T2 — Categorías** (`S5`–`S7`) · inline · commit `refactor(web): rearmar el ABM de categorías con componentes de ui`
- **T3 — Formulario de hábito** (`S4`, `S5`, `S7`) · inline · commit `refactor(web): rearmar el alta de hábitos con componentes de ui`
- **T4 — Listas y el día** (`S6`, `S7`) · inline · commit `refactor(web): rearmar listas y el día con componentes de ui`
- **T5 — Login y docs** (`S5`, `S8`) · inline · commit `refactor(web): rearmar el login con componentes de ui`

Cada tarea cierra con `pnpm lint`, `pnpm check`, `pnpm test:web` y `pnpm test:e2e` en verde. Los PRs se
abren cuando el autor lo pida. Un issue por slice, como en la feature anterior.

## Log

**L1** — «Para la UI teníamos que usar shadcn-svelte ya que nos deja los componentes listos, pero hasta
ahora lo hemos hecho con nativos. […] mi idea acá es saber si hacer el refactor usando esas cosas nos
simplificaría las cosas, mejoraría la mantenibilidad, legibilidad y uso.»

**L2** — Investigación (shadcn-svelte 1.7.0, superforms 3.1.0, formsnap 2.1.0). Hallazgos que deciden:
- El componente `form` (formsnap + superforms) **sigue en el registro pero su página de docs devuelve
  404**: `shadcn-svelte.com/docs/components/form`; el índice lista `field`, no `form`. La guía vigente
  usa `Field` + `Input`/`Select` con `data-invalid`/`aria-invalid`.
- **`npx shadcn-svelte add form` falla en SvelteKit 3** (`ERESOLVE`: el registro fija
  `sveltekit-superforms@^2.28.1`, cuyo peer es `@sveltejs/kit 1.x || 2.x`), issue
  huntabyte/shadcn-svelte#2915, abierta y etiquetada `blocked-by-dep`.
- **Superforms 3.1.0 sí soporta SvelteKit 3** (`peer: ^2.12.0 || ^3.0.0`), y el modo SPA (`SPA: true` +
  `validators` + `onUpdate`) está documentado para APIs externas. Pero en esta arquitectura
  (`adapter-static`, `ssr = false`, API en otro origen) se pierde todo lo que lo justifica: no hay
  progressive enhancement, ni `superValidate(request)`, ni `applyAction`.
- Adapters de Zod: `zod` (Zod 3) y `zod4` (Zod 4); Zod se instala aparte, el CLI no lo trae.
- Decisión del autor: **no se adopta librería de forms**. Con 3 formularios chicos y la validación
  autorizada en la WebAPI, un schema en el cliente es una segunda fuente de verdad que se
  desincroniza. Si aparece un formulario con >10 campos o varios pasos, se revisa.
- Queda disponible, barato y sin librería: un validador puro por formulario
  (`lib/api/validar-habito.ts`) que devuelve el mismo `ErroresDeFormulario` de `lib/api/errores.ts`. No
  entra en alcance salvo que se pida.

**L3** — «¿Hacemos paso 0 y 1 en esta sesión que ya tiene 30% del contexto o empezamos otra diciéndole
qué hacer y por qué? Me interesa hacer paso 0 y 1 (que quede la carpeta /components)». Se corta acá y el
trabajo sigue en sesión nueva, leyendo este documento y el issue. La carpeta definitiva queda en inglés
(`src/lib/components/`), no `componentes/`.

**L4** — Riesgos que el ejecutor tiene que respetar:
- Los componentes del CLI son vendorizados: `dotnet format` no aplica, prettier y eslint no los tocan,
  y `svelte-check` sí.
- `add` toca `package.json` del proyecto web (bits-ui, tailwind-variants, @internationalized/date). Ese
  cambio va en T1, no mezclado con el refactor de una pantalla.
- El e2e es la red: si `getByLabel('Categoría').selectOption(...)` deja de funcionar, es que se usó
  `Select` de bits-ui donde iba `native-select` (S4).
- El repo no tiene CI: la barrera es la Definition of Done de `docs/proceso/issue-tracking.md`.

**L5** — «Leé el issue #26 y odd/tasks/componentes-ui.md (hasta ## Log). Ejecutá T1: corregir el alias
  de components.json a $lib/components, correr pnpm exec shadcn-svelte@latest add button input label
  textarea native-select table badge alert empty separator field spinner desde frontend/web, ajustar
  .prettierignore/eslint y AGENTS.md, y cerrar con pnpm lint, pnpm check, pnpm test:web y pnpm test:e2e
  verdes. Sin commits a main, sin push: rama feat/ui-componentes e issue por slice como la vez pasada.»

**L6** — Evidencia de T1 (rama `feat/ui-componentes`, issue #28, commit `899d9d6`):
- **`pnpm exec shadcn-svelte@latest add …` no existe.** `pnpm exec` no acepta especificador de versión:
  muere con `ERR_PNPM_RECURSIVE_EXEC_FIRST_FAIL`. Se usó el binario que fija `devDependencies`:
  `pnpm exec shadcn-svelte add --yes …` (v1.7.0). `@latest` sí sería válido con `pnpm dlx`, que no es lo
  que pide S1.
- **`add` sin `--yes` no instala nada sin TTY**: frena en «Ready to install components and dependencies?»
  y sale sin escribir archivos.
- El CLI sumó `bits-ui`, `tailwind-variants` y `@internationalized/date`, y subió `@lucide/svelte` de
  `^1.57.0` a `^1.66.0`. El lockfile cambia en el mismo commit.
- Verificado: `pnpm lint` 0 · `pnpm check:web` 0 errores y 0 advertencias (svelte-check sí revisa
  `components/ui`) · `pnpm test:web` 22/22 · `pnpm test:e2e` 2/2 **sin tocar** `categorias.e2e.ts` ni
  `habitos.e2e.ts`.
- **`pnpm check` no queda verde por una causa ajena a T1**:
  `DiasEndpointTests.La_semana_cuenta_de_lunes_a_domingo_y_solo_los_dias_completados` espera 2 cumplidos
  esta semana para «Ejercicio pesado» (12) y obtiene 1. El seed arma la semana con
  `@lunes = CURDATE() - INTERVAL WEEKDAY(CURDATE()) DAY` y filtra `<= CURDATE()`, así que un lunes o un
  martes la fila del miércoles todavía no existe y la historia «va 2 de 3» no se puede contar. `backend/`
  y `database.sql` quedaron idénticos a `main` en este slice: el defecto es preexistente y depende del
  día en que se corre. No se tocó: queda a decisión del autor.
