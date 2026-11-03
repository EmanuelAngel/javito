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

**S9 — Kebab-case en los nombres de archivo.** Todo `.svelte` de la app se nombra en kebab-case
(`cambio-de-tema.svelte`, `fila-del-dia.svelte`), igual que los vendorizados de `components/ui/`. Los
identificadores importados siguen en PascalCase (`import FilaDelDia from '$lib/components/fila-del-dia.svelte'`)
y las rutas de SvelteKit (`+page.svelte`, `+layout.svelte`) no cambian.

## Tasks

- **T1 — Paso 0: el alias y los componentes** (`S1`–`S3`) · inline · rama `feat/ui-componentes` ·
  commit `chore(web): mover el alias de shadcn a la carpeta de componentes` · **Hecho** en `899d9d6`
  (issue #28, PR #31). El commit se llamó `chore(web): mover el alias de shadcn a components y vendorizar los
  componentes base`.
- **T2 — Categorías** (`S5`–`S7`) · inline · commit `refactor(web): rearmar el ABM de categorías con componentes de ui`
  · **Hecho** en `e58a47a` (issue #36, PR #37)
- **T3 — Formulario de hábito** (`S4`, `S5`, `S7`) · inline · commit `refactor(web): rearmar el alta de hábitos con componentes de ui`
- **T4 — Listas y el día** (`S6`, `S7`) · inline · commit `refactor(web): rearmar listas y el día con componentes de ui`
- **T5 — Login y docs** (`S5`, `S8`) · inline · commit `refactor(web): rearmar el login con componentes de ui`
- **T6 — Kebab-case en los nombres** (`S9`) · inline · rama `refactor/kebab-case` · commit
  `refactor(web): renombrar los componentes a kebab-case`. Va **antes** de T2: renombrar después obliga a
  reescribir los mismos archivos dos veces.

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
- **`pnpm check` estaba en rojo por una causa ajena a T1**, ya corregida: ver L7.

**L7** — «con issue aparte» (después de elegir «Opción 1: arreglarlo ahora, seed + prueba weekday-aware»).
El defecto que dejaba `pnpm check` en rojo era de la prueba del día, no de T1:
`DiasEndpointTests.La_semana_cuenta_de_lunes_a_domingo_y_solo_los_dias_completados` hardcodeaba 2
cumplidos esta semana para «Ejercicio pesado» (12) y 1 para 13 y 14, mientras el seed arma la semana en
curso con `@lunes = CURDATE() - INTERVAL WEEKDAY(CURDATE()) DAY` y descarta lo que cae en el futuro: un
lunes el miércoles no existe, y con `UNIQUE (habito_id, fecha)` dos cumplidos son inalcanzables ese día.
Se arregló en su propio issue (**#29**, rama `fix/seed-semana`, commit `74f4bad`, integrado en
`feat/ui-componentes` con el merge `69b1683`): el seed siembra la semana en curso en lunes y martes y la
prueba calcula lo esperado con los días transcurridos. RED observado con el seed viejo y la prueba nueva
(esperado 1, obtenido 0); GREEN con 73/73 un lunes.

**L8** — Cierre de T1 en `feat/ui-componentes` (`69b1683`): `pnpm lint` 0 · `pnpm check` 0 (73/73 de API
y 0 errores/0 advertencias de svelte-check) · `pnpm test:web` 22/22 · `pnpm test:e2e` 2/2 sin tocar los
e2e. PRs abiertos: **#30** (`fix/seed-semana`, base `main`) y **#31** (`feat/ui-componentes`, apilado
sobre #30: cambiar la base a `main` cuando mergee).

**L9** — «Ya mergeé #30 y #31. Además de hacer el refactor este, también haz que los nombres de archivos
de svelte sean en kebab-case, esa es la convención que usaremos acá»

**L10** — El merge de #31 quedó apuntado a su rama base (`fix/seed-semana`) y no a `main`: `main` recibió
sólo el squash de #30, así que el contenido de T1 (`components/ui/`, alias, exclusiones) nunca llegó al
default branch y #28 siguió abierto. Se abrió el **PR #32** desde `chore/ui-componentes` (la punta de
`fix/seed-semana` después de ese merge), con el mismo contenido de T1 y `Closes #28`. Las ramas del
refactor salen de ahí.

**L11** — El gate de T6 corrió al filo de la medianoche (lunes 23:59 → martes 00:00): `pnpm check` falló
2 de 73 y un minuto después dio 73/73. Causa: el fixture siembra con `CURDATE()` a las 23:59 y las pruebas
vuelven a preguntar `CURDATE()` a las 00:00, así que la base quedó sembrada para ayer. No lo causa el
slice: queda como issue **#34**.

**L12** — T2 cerrado en `refactor/categorias` (`e58a47a`, issue #36, PR #37): la pantalla de categorías
pasa a `Field`/`Input`/`Button`/`Table`/`Badge`/`Alert`/`Empty`. `categorias.e2e.ts` pasa **sin
modificaciones**. Verificado: `pnpm lint` 0 · `pnpm check` 0 (73/73 + svelte-check 0/0) · `pnpm test:web`
22/22 · `pnpm test:e2e` 2/2. Detalle: el `Input` vendorizado tipa `maxlength` como **number**, así que
`maxlength="60"` no compila; va `maxlength={60}`.
