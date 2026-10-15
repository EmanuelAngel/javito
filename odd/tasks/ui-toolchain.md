# Toolchain de UI y temas de Pomody

Feature sobre la rama `feat/ui-toolchain`, en curso. Se detuvo una vez (L5) y se retomó en L6. Este
documento es la fuente de verdad.

Último cambio de requisitos: **L10** — los temas salen de Pomody y los maneja ModeWatcher, los íconos son
Lucide (se cae el shim de Solar) y no hay tests de tokens.

## Specs

- **S1 — Add-ons de SvelteKit.** eslint, prettier, vitest, playwright y tailwindcss. Tailwind con los
  plugins `typography` y `forms`.
- **S2 — shadcn-svelte** bajo `src/lib/componentes/ui`. `ui/` es vendorizada: se excluye de prettier
  y de eslint, **no** de `svelte-check` (decisión del usuario).
- **S3 — Íconos: Lucide.** Se usa `@lucide/svelte`, que es lo que `components.json` ya declara
  (`"iconLibrary": "lucide"`). Sin shim, sin alias y sin un segundo pack de íconos. `ui/` queda intacto y
  `shadcn update` no puede romper nada. Reemplaza a la opción B de Solar Icons (L10).
- **S4 — Temas de Pomody** (`~/dev/pomody/src/app.css`), con su estructura: capa de marca, capa semántica
  de shadcn, `@theme inline` y `@layer base`. Dos paletas, no tres: **Rosé Pine Dawn en `:root`** (claro)
  y **Rosé Pine OLED en `.dark`** (oscuro). Se descartan la tercera paleta (Rosé Pine base) y la
  indirección `data-theme`: el modo lo maneja **ModeWatcher** con la clase `.dark`.
- **S5 — Interfaz en español**, incluidos aria-labels.
- **S6 — Prettier se muda al frontend.** La raíz conserva husky, commitlint y lint-staged.
- **S7 — Base color de shadcn: `stone`** (inmutable después del `init`).
- **S8 — Vitest** sólo `unit` al principio. **Playwright** sin demo. **Sin tests de tokens** (L10): el CSS
  es declarativo y se verifica con `pnpm lint`, `pnpm check` y `pnpm build`. Ya se había borrado el spec en
  L9; L10 lo confirma como decisión, no como deuda.

## Estado actual verificado

Verificado con `pnpm check` (**0 errores, 0 advertencias, exit 0**) y `pnpm build` (**exit 0**), con
dos advertencias de contenido importante (ver Hallazgos).

**Commiteado** (`feat/ui-toolchain`: `bdf6474`, `b722a73`):

- Alias `$lib` restaurado en la config del plugin (`vite.config.ts`), probado con `pnpm check` y
  `pnpm build` porque `+page.svelte` importa `$lib/config`. → **pero ver Hallazgo 1: está deprecado.**
- Este documento de feature.

**Commiteado en `e6b2cab`** (`chore(web): agregar el toolchain de lint, formato y pruebas del SPA`),
verificado con `pnpm lint` (**exit 0**), `pnpm check` (**0 errores, 0 advertencias, exit 0**) y
`pnpm build` (**exit 0**):

| Área | Estado |
| :--- | :--- |
| Tailwind | `tailwindcss@4.3`, `@tailwindcss/vite`, `@tailwindcss/forms`, `@tailwindcss/typography`. `tailwindcss()` en `plugins` de `vite.config.ts`. Archivo global en **`src/routes/layout.css`** con `@import 'tailwindcss'` y los dos `@plugin`. |
| eslint | `eslint@10` + `@eslint/js`, `typescript-eslint`, `eslint-plugin-svelte`, `globals`, `eslint-config-prettier`, `eslint.config.js` |
| prettier | `prettier@3.8` + `prettier-plugin-svelte` + `prettier-plugin-tailwindcss`, `prettier.config.js`, `.prettierignore` |
| vitest | `vitest@4`, `vitest-browser-svelte`, `@vitest/browser-playwright`; configuración **dentro de** `vite.config.ts` (dos projects: `client` y `server`) |
| playwright | `playwright@1.60`, `playwright.config.ts` |
| scripts nuevos | `lint`, `format`, `test:unit`, `test`, `test:e2e` en `frontend/web/package.json` |
| limpieza | `src/routes/demo/`, `src/lib/vitest-examples/` y el `DemoLinks` de `+layout.svelte` borrados. El `pi-session-*.html` de la raíz ya no está: alguien lo sacó antes de retomar |
| prettier en la raíz | **eliminado**: dependencias, `.prettierrc`, `.prettierignore` y los scripts `format`/`check:format`. `lint-staged` delega en `pnpm --dir frontend/web exec prettier --write` |
| `ui/` | excluido de prettier (`frontend/web/.prettierignore`) y de eslint (`ignores` en `eslint.config.js`), **no** de `svelte-check` |
| pendiente | `pnpm exec vitest run` sale **1 con "No test files found"**: los únicos specs eran los de demo. T2 agrega el spec del contrato de tokens |

## Hallazgos que cambian el plan

### 1. El alias `$lib` está DEPRECADO en SvelteKit 3

`pnpm check` y `pnpm build` emiten:

```
config_option_deprecated_alias
The `config.alias` option is deprecated, and will be removed in a future version of SvelteKit.
Use subpath imports instead: https://svelte.dev/docs/kit/$lib
```

**Resuelto en la reanudación, con dos experimentos.** La restauración de `$lib` no es opcional:

- Sacando la línea `alias: { $lib: 'src/lib' }` de `vite.config.ts`, el **build falla**:
`SvelteKit error: module_removed_lib` → *"`$lib` has been removed. Use `#lib` instead… To keep using
`$lib`, add `alias: { '$lib': 'src/lib' }` to your SvelteKit config."* (exit 1)
- Con la línea puesta: `pnpm check` 0 errores / 0 advertencias y `pnpm build` exit 0.

SvelteKit 3 también **borra el alias `$lib` del tsconfig generado** cuando la opción no está, así que
`alias` es el único puente vigente. La alternativa `#lib` (subpath imports de `package.json`) queda
descartada por ahora: `#lib/*` no resuelve en TypeScript si el especificador no lleva extensión
(trampa ya documentada en `AGENTS.md`) y los componentes de shadcn-svelte se importan entre sí con
especificadores sin extensión.

**Decisión (L8):** mantener `alias` y convivir con `config_option_deprecated_alias`. Es la ruta que
el propio kit documenta. Migrar a `#lib` cuando SvelteKit quite la opción o cuando el CLI de shadcn
acepte subpath imports.

### 2. `tsconfig.json` perdió `paths`

```
tsconfig_invalid
Found issues while validating `tsconfig.json`:
  - "paths" was overwritten. Imports from "$lib" may not typecheck
```

**No era fragilidad del typecheck: era el precio del `alias` deprecado.** Lo verificado:

- El tsconfig que el proyecto hereda **no vive en el repo**: SvelteKit 3 escribe
`node_modules/$app/tsconfig.json` con los `paths` de `$lib` y `$lib/*` **generados desde la opción
`alias`** (`path.resolve(root, 'src/lib')`). `.svelte-kit/tsconfig.json` ya no existe.
- Por eso `svelte-check` pasaba y sigue pasando: el typecheck de `$lib` **no** depende de nuestro
`tsconfig.json`.
- El aviso lo emite el validador del propio kit (`core/sync/write_tsconfig/validate.js`), que compara
los `paths` que espera contra los resueltos. **Sacando la opción `alias`, el aviso desaparece** — junto
con `config_option_deprecated_alias`. Es decir: `tsconfig_invalid` es consecuencia de la deprecación y
no de una mala configuración nuestra.
- Agregar `paths` a nuestro `tsconfig.json` **no** borra el aviso (comprobado), pero sí aporta algo
valioso: con el `alias` quitado, `$lib` sigue tipando (0 errores). Es la mitad durable del puente y es
además el lugar estándar donde un CLI externo busca cómo resolver `$lib`.

**Decisión (L8):** se agrega el bloque `paths` a `tsconfig.json` y se acepta el aviso
`tsconfig_invalid` mientras dure la deprecación.

### 2b. El CLI de shadcn espera `svelte.config.js`

La doc de `components.json` dice, textual: *"The CLI uses these values and the `alias` config from
your `svelte.config.js` file to place generated components in the correct location. Path aliases have
to be set up in your `svelte.config.js` file."* En este proyecto **no hay `svelte.config.js`** (en
SvelteKit 3 la configuración vive en el plugin `sveltekit()` de `vite.config.ts`), así que ese supuesto
del CLI no se cumple. Lo resuelve el bloque `paths` de `tsconfig.json`, que es el otro lugar donde el
CLI mira.

### 3. El archivo global es `src/routes/layout.css`, no `src/app.css`

Los tokens Kanagawa van **ahí**. Y el `init` de shadcn debe recibir `--css src/routes/layout.css`. No
crear un segundo archivo global: la skill de shadcn lo prohíbe explícitamente.

### 4. `playwright.config.ts` viene con tres cosas a corregir

```ts
webServer: { command: 'npm run build && npm run preview', port: 4173 },
testMatch: '**/*.e2e.{ts,js}'
```

- Usa `npm` en un proyecto `pnpm`. → **corregido**.
- El puerto 4173 es el de `vite preview`, no el 5173 del SPA. → **el motivo real es más fuerte:** la
política CORS del backend es fail-closed y sólo autoriza `http://localhost:5173`
(`Cors:AllowedOrigins`), así que desde 4173 toda prueba contra la API real fallaría por CORS y no por
la aplicación. El servidor de e2e se sirve en 5173 y el config expone `baseURL`.
- El script `test:e2e` corre `playwright install` en cada ejecución (baja navegadores). → se separó en
`test:e2e:install`, que se corre una vez.
- El e2e contra el backend real necesita MySQL, que todavía no existe. **Sigue pendiente y por eso todavío
no hay pruebas de e2e.**

**Resuelto en `5ea34e4`.** Evidencia: `pnpm preview --port 5173` responde 200 en 5173 y no escucha en
4173; `playwright test --list` carga el config (0 tests); `pnpm lint` y `pnpm check` en verde.

### 5. Duplicación de prettier, y las dos configs NO son equivalentes

La raíz conserva `prettier`, `prettier-plugin-svelte`, `.prettierrc` y `.prettierignore`, y
`.lintstagedrc.json` sigue llamando al prettier **de la raíz** para archivos del frontend. La config
del frontend tiene `prettier-plugin-tailwindcss` y `tailwindStylesheet`, **la de la raíz no**: el hook
formatearía las clases de Tailwind sin el plugin y quedaría distinto de lo que produce `pnpm lint` del
frontend. Hay que hacer efectiva la decisión S6 (mover prettier al frontend y que lint-staged delegue).

### 6. eslint ignora vía el `.gitignore` del frontend

`eslint.config.js` usa `includeIgnoreFile(.gitignore)`. Para excluir `ui/` de eslint **no** conviene
meterla en `.gitignore` (porque `ui/` sí se commitea): va un `ignores` explícito en `eslint.config.js`.
Para prettier, va en `frontend/web/.prettierignore`.

### 7. Operativa del CLI `sv` (aprendido a golpes)

- **`sv add` exige el árbol de git limpio.** Con archivos sin trackear falla el preflight y pregunta
  "¿continuar?"; en un job no interactivo contesta *No* y **sale con exit 0 sin hacer nada**. Un exit 0
  de `sv add` no prueba que haya hecho algo: hay que verificar el resultado.
- **Sintaxis de opciones:** `addon=opcion:valor1,valor2`. Los valores van **separados por coma**;
  el `+` separa *opciones distintas*, no valores. `plugins:typography+forms` da
  `Malformed arguments: 'forms' are missing their option name or value`.
- Sin argumentos, `sv add tailwindcss` **abre un prompt** y se cuelga en un job no interactivo.
- Comando que funcionó: `pnpm dlx sv@latest add 'tailwindcss=plugins:typography,forms'`
  (que es lo que terminó corriendo el usuario, más los otros cuatro add-ons).

## Tema: qué se copia de Pomody

Fuente: `~/dev/pomody/src/app.css`. De sus tres paletas se copian dos, mapeadas al modo que ModeWatcher
maneja (L10):

| Bloque de Pomody | Dónde va | Por qué |
| :--- | :--- | :--- |
| `[data-theme='dawn']` | `:root` | Es la paleta clara: **Dawn**. |
| `[data-theme='oled']` | `.dark` | Es la oscura de la elección del usuario: **OLED**. |
| `:root` / `[data-theme='dark']` (Rosé Pine base) | no se copia | ModeWatcher sólo distingue claro de oscuro. |

Se copia lo que Pomody define en cada tema: la capa de marca (`--bg-base`, `--bg-surface`, `--bg-overlay`,
`--text-muted`, `--text-subtle`, `--text-primary`, `--accent-love/rose/pine/foam/iris/gold`), la capa
semántica de shadcn, `--radius: 0.625rem` y el mapeo de `@theme inline`.

No se copia, y por qué:

- Los tres `--mode-*-text` (focus, short-break, long-break): son tokens del dominio de pomodoro de Pomody.
- `@import 'tw-animate-css'`: no está instalado y no hace falta.
- `user-select: none` en `body`: Pomody es un temporizador; en Javito copiar texto es legítimo.
- `chart-1..5` y `sidebar-*`: los definía la paleta Kanagawa que se reemplaza; las paletas de Pomody no los
  traen. Se agregan el día que se instale `Chart` o `Sidebar`.

Se mantienen `@custom-variant dark (&:is(.dark *))`, las tipografías Inter Variable + JetBrains Mono
Variable vía `@fontsource-variable/*` (L6) y el scrollbar fino de `@layer base`.

El modo lo resuelve **ModeWatcher** (`mode-watcher@1.1.0`, peer `svelte ^5.27`): aplica y saca `.dark` en
el `<html>`, arranca con la preferencia del sistema operativo y la persiste. Reemplaza al script propio
anti-FOUC que vivía en `app.html`.

## Tasks (replanteadas sobre el estado real)

Orden vigente desde L6. El commit de cada unidad de trabajo va en la rama `feat/ui-toolchain`.

| ID | Tarea | Estado |
| :--- | :--- | :--- |
| T1 | Tailwind con `typography` + `forms` | Hecho (se commitea en T8) |
| T8 | **Limpieza + commit del toolchain.** Borrar `src/routes/demo/`, `src/lib/vitest-examples/` y el `DemoLinks` de `+layout.svelte`; commitear el estado del `sv add`. Absorbe el T9 original: era un commit de dos líneas y el toolchain debe llegar sin andamios de demo | **Hecho** en `e6b2cab`, junto con T3 y T9 |
| T3 | eslint y prettier en el frontend: efectivo, borrar el prettier de la raíz, que lint-staged delegue, y `ignores` de `ui/` en eslint + `.prettierignore` | **Hecho dentro de T8** (L7): sin esto no se podía commitear nada del SPA |
| T6 | vitest y playwright: arreglar `playwright.config.ts` (pnpm, puerto, no reinstalar navegadores a cada corrida) | **Hecho** en `5ea34e4` (L8) |
| T4 | shadcn-svelte con `--css src/routes/layout.css` y alias a `src/lib/componentes/ui`; base color `stone`. **Adelantado antes de T2** (L6): el `init` escribe su propio bloque de tokens en el CSS y pisaría el del tema | **Hecho** en `b4e6a61` (L8), con una parte pendiente: el `init` no pudo cerrarse sin TTY |
| T2 | Sistema de tokens en `src/routes/layout.css` + claro/oscuro + reescribir `+page.svelte` con tokens | **Hecho** en `905b0c1` y **reemplazado por T10**: la paleta Kanagawa se descartó en L10 |
| T5 | Shim de íconos Solar (`@solar-icons/svelte@2.3.2`) | **Cancelada** en L10: los íconos son Lucide, no hay nada que mapear |
| T7 | Integración y documentación: scripts de raíz, `AGENTS.md`, README | **Hecho** (L9). `AGENTS.md` se vuelve a tocar en T11 por L10 |
| T9 | — | **Absorbido por T8** (L6) |
| T10 | Temas de Pomody (Dawn en `:root`, OLED en `.dark`) + ModeWatcher + íconos Lucide en el botón de tema. Reemplaza a T2 y cierra T5 | **Hecho** en la cadena de L11 |
| T11 | Documentación de L10: `AGENTS.md` y este documento | **Hecho** en la cadena de L11 |

Lo que queda pendiente de verdad, después de T2, T5 y T10:

- **`pnpm exec vitest run` sale 1 con `No test files found`.** No hay ningún spec en `frontend/web` y por
  L10 tampoco lo va a haber para los tokens. Es estado preexistente: no lo introduce T10.
- **Pruebas de e2e: no existe ninguna.** El e2e contra el backend real necesita MySQL, que todavía no existe.
- **La parte pendiente de T4** (el `init` sin TTY) quedó cubierta por diseño: `layout.css` importa
  `shadcn-svelte/tailwind.css` en vez de copiar el bloque. Al primer `add`, el CLI va a pedir
  `tailwind-variants` (L8).

## Decisiones pendientes

Ninguna abierta. Resueltas: el alias `$lib` deprecado → se mantiene y se documenta (L8); los `paths` de
`tsconfig.json` → bloque propio en `tsconfig.json` (L8); las tipografías → Inter Variable + JetBrains Mono
Variable (L6); los íconos → Lucide, sin shim (L10); las paletas → Dawn en claro y OLED en oscuro (L10).

Datos de producto vigentes: interfaz en español (incluidos los aria-label), `ui/` fuera de prettier y
eslint pero con typecheck, y base color `stone`.

## Log

### L1 — Pedido original (verbatim)

> Me olvidé de decirte que SvelteKit no lo quería simple, lo quería con algunas cosas que ofrece el mismo `sv create` que entiendo se pueden agregar con `sv add`: - eslint. - prettier. - vitest. - playwright. - tailwindcss. Fuera de lo que ofrece SvelteKit quiero tres cosas: - shadcn-svelte (tenés la skill disponible) es la herramienta para construir las interfaces bajo src/lib/componentes/ui (ui/ pasaría a ser una carpeta vendor, así que excluíla de los checks). - Solar Icons Svelte: https://www.npmjs.com/package/@solar-icons/svelte (aunque no sé qué sucede con shadcn-svelte siendo que en el template creo que pregunta qué pack de íconos querés, no sé en qué afecta eso realmente). - Crear un sistema de tokens css como el de Pomody (~/dev/pomody/src/app.css) pero en lugar de tener tres temas basados en Rosé Pine, para Javito quiero uno con tema claro/oscuro y con la paleta de colores Kanagawa Dragon.

### L2 — Decisiones del usuario

Íconos: opción B (shim). Interfaz: en español. Prettier: al frontend. `ui/`: con typecheck. Contraste:
"lo que recomiendes y mejor sea para contraste".

### L3 — Restricciones de la corrida

"no tardes tanto esta vez, no es algo tan serio y estas cosas no son tan complicadas. Y sin
gentle-verify esta vez". Sin verificación independiente.

### L4 — Verificación independiente que igual conviene mirar

`pnpm check` y `pnpm build` en verde (ver arriba) son evidencia propia, no externa. Quedan dos
advertencias reales sin resolver que son las de los Hallazgos 1 y 2.

### L5 — Parada

> Deten la implementación por completo. Mejor escribe la tarea en odd/ así dejamos planteado qué hay que hacer así lo retomamos en otro momento.

Parada en seco con el árbol sucio y sin commitear. **Punto de partida para retomar: revisar
`git status` en `feat/ui-toolchain` antes de cualquier otra cosa.**

### L6 — Reanudación autorizada y decisiones de producto

El usuario volvió a abrir el documento (mensaje: `@odd/tasks/ui-toolchain.md`) y eligió, ante la
pregunta de alcance, **retomar el plan completo en el orden del documento**, con la tipografía del
tema definida como **Inter Variable + JetBrains Mono Variable** vía `@fontsource-variable/*` (el
precedente de Pomody). Con eso:

- **Decisiones pendientes 3 (tipografías): resuelta.** Las otras dos siguen abiertas y se resuelven
dentro de T4, contra evidencia del CLI, no de memoria.
- **T8 absorbe T9.** El commit de limpieza era de dos líneas; el toolchain entra al repo en un solo
work unit sin los andamios de demo. T9 queda cerrado como absorbido, no como tarea hecha.
- **T4 se adelanta antes de T2.** El `init` de shadcn escribe su propio bloque de tokens en el CSS
(`--css src/routes/layout.css`); si T2 corriera primero, el init pisaría Kanagawa y habría que
rehacerlo. El orden aprobado se respeta en todo lo demás.
- **Entrega: cadena sobre la rama de feature.** Elegiste la opción que dice "incluye commits de unidad
de trabajo en la rama". No hay remoto (`git remote -v` vacío), así que la creación de PR no aplica y
no se pregunta por estrategias de encadenado. Push, PR y merge siguen siendo decisión tuya.
- **Sin verificación independiente** (restricción vigente de L3), pero **sí** las advertencias reales de
los Hallazgos 1 y 2: no se cierran por cansancio. RDD está apagado en este harness.
- Evidencia de reanudación: `git remote -v` vacío; `pi-session-*.html` ya no existe en la raíz;
`src/lib/` contiene `assets/`, `config.ts`, `index.ts`, `vitest-examples/`.

### L7 — T8 ejecutado: el hook estaba roto y T3 se le adelantó

T8 salió en `e6b2cab`, pero **no como estaba planeado**, y el motivo es un hallazgo duro:

**El primer `git commit` falló.** El hook `pre-commit` (`pnpm exec lint-staged`) reventó con
`Cannot find package 'prettier-plugin-tailwindcss' imported from .../javito/noop.js`. Causa: prettier
resuelve sus plugins contra el **directorio de trabajo**, no contra el directorio de la configuración
que descubre. Como `lint-staged` corre en la raíz, prettier encontraba `frontend/web/prettier.config.js`
para cada archivo del SPA, pero intentaba importar los plugins desde `node_modules` de la raíz, donde
`prettier-plugin-tailwindcss` no está.

Consecuencia que cambia el plan: **con el estado que dejó el `sv add`, ningún commit del frontend pasa
el hook.** No era "deuda de higiene" (Hallazgo 5); era un bloqueo. Los dos commits anteriores pasaron
porque todavía no existía un `prettier.config.js` en el frontend.

Por eso T3 se adelantó y se resolvió **dentro** del mismo work unit que T8, antes del primer commit:
no tiene sentido dejar un commit intermedio que se sabe roto, y `--no-verify` habría ocultado el
problema en lugar de arreglarlo. Qué se hizo:

- `lint-staged` delega: `"frontend/web/**/*.{ts,js,svelte,json,yml,yaml}": "pnpm --dir frontend/web exec prettier --write"`. Con `--dir`, el cwd del proceso es `frontend/web`, así que los plugins y el
`tailwindStylesheet` de `prettier.config.js` resuelven. Comprobado: el hook passó en `e6b2cab`.
- Dato que hizo falta: `lint-staged@17.6.0` pasa **rutas absolutas** por defecto (`relative: false`),
así que no hay que traducir rutas ni usar un `lint-staged.config.js` con funciones.
- Prettier sale de la raíz por completo: `prettier` y `prettier-plugin-svelte` fuera de
`devDependencies`, `.prettierrc` y `.prettierignore` borrados, y los scripts `format`/`check:format`
que quedaron fuera también. La raíz queda con husky, commitlint y lint-staged (S6).
- Los archivos de configuración de la raíz dejan de formatearse con prettier. Es lo que dice S6 y
achica el sistema; el README y `AGENTS.md` lo reflejan en T7.
- `ui/` quedó excluido de prettier y eslint ya en este commit, aunque todavía no exista. Es el
contrato de S2 y evita que T4 tenga que acordarse.
- `pnpm lint` del frontend **fallaba en 10 archivos** antes de esto; después, exit 0. También se
confirmó que prettier **no** toca `.svelte-kit/` ni `build/`, así que no hicieron falta exclusiones
nuevas para artefactos generados.

Verificación observada de T8 (`e6b2cab`): `pnpm lint` exit 0 · `pnpm check` 0 errores / 0
advertencias, exit 0 · `pnpm build` exit 0 · hook `pre-commit` en verde. **No verificado:**
`pnpm exec vitest run` sale 1 con `No test files found` porque los únicos specs eran los de demo.
Lo cubre T2 con el spec del contrato de tokens.

Nota de higiene: `git status` muestra archivos del frontend como modificados aunque su md5 coincida
exactamente con el blob de HEAD. Es la caché de stat de `core.autocrlf=true`, no un cambio real;
`git add` de esos paths lo normaliza.

### L8 — T6, T4 y la letra chica del CLI de shadcn

**Higiene de finales de línea (`dc632e4`).** `pnpm lint` empezó a fallar en `layout.css`, un archivo que
no se había tocado. Causa medida: quedaba con **CRLF** en el árbol de trabajo y prettier exige LF. No
era prettier, era **git**: con `core.autocrlf=true` (el valor por defecto en Windows) y sin
`.gitattributes`, `git checkout` reescribe el archivo con CRLF. Verificado en dos pasos: `prettier
--write` lo deja en LF y un `git checkout` posterior lo devuelve a CRLF. Se agrega `.gitattributes`
(`* text=auto eol=lf`); `git check-attr` confirma `text: auto`, `eol: lf` y el checkout forzado ya
entrega LF. Sin esto, el lint del frontend dependía de qué archivo hubiera vuelto a tocar git.

**Hallazgos 1 y 2: cerrados con evidencia.** El detalle está en sus apartados; el resumen es que el
`alias` deprecado es obligatorio (sin él el build muere con `module_removed_lib`) y que
`tsconfig_invalid` lo emite el validador del propio kit contra los `paths` que él mismo genera. La
mitad durable —el bloque `paths` en `tsconfig.json`— queda puesta.

**T6 (`5ea34e4`).** Además de lo que decía el Hallazgo 4, el motivo de peso para no usar 4173 es el CORS
fail-closed del backend. Comprobado levantando `vite preview` con `--port 5173`: responde 200 en 5173 y
no escucha en 4173.

**T4 (`b4e6a61`) y la letra chica del CLI.** Lo que costó averiguar y no conviene volver a averiguar:

- `shadcn-svelte init` **no puede completarse sin TTY**. Tiene dos confirmaciones encadenadas —
  *"Updates to your src/routes/layout.css are required… Continue?"* y *"Would you like to overwrite
  all existing files?"*— y **ningún flag las saltea**: `--skip-preflight` no alcanza, y con stdin por
  pipe el CLI imprime el prompt y aborta. Alcanzó a escribir `components.json` y `src/lib/utils.ts`.
- Los presets **no son nombres**: `--preset nova` responde *"is not a valid preset"*. Son configs
  empaquetadas por bits en base62 con prefijo `b` (`encodePreset`/`decodePreset` de `dist/preset`). El
  que se usó es `bJMSkfJo` = nova + stone + stone + stone + lucide + inter + inherit + radius medium,
  verificado por round-trip con el decodificador del mismo paquete.
- **`add` no toca `layout.css`.** Comprobado con `add --yes --no-deps badge`: instaló el componente y
  el CSS quedó intacto. El bloque de utilidades y variantes sólo lo escribe `init`.
- Pero **no hace falta copiarlo**. `dist/utils/css.mjs` muestra el mecanismo previsto: la plantilla
  global que el CLI crea es `@import "tailwindcss";` + `@custom-variant dark (&:is(.dark *));`, y las
  utilidades y variantes se traen con `@import "shadcn-svelte/tailwind.css"`, que el paquete exporta
  como `./tailwind.css`. Por eso `shadcn-svelte@1.7.0` queda como dependencia de desarrollo del
  frontend: fija la versión del CLI ya validada y habilita ese import sin meter 15 KB de CSS ajeno en
  el repositorio de la cátedra. **T2 escribe `layout.css` con esa forma.**
- El CLI **validó los alias** (*"Alias paths validated"*) leyendo el bloque `paths` de `tsconfig.json`.
  Es el reemplazo del `svelte.config.js` que la doc supone y que este proyecto no tiene.

**Abierto, para no perderlo:** con el primer componente, `add` va a pedir `tailwind-variants` (el CLI
lo avisó) y las utilidades de `shadcn-svelte/tailwind.css` tienen que estar importadas en
`layout.css`. Si ese import diera problemas de resolución, el plan B es copiar el bloque con una nota
de procedencia.

### L9 — Corrección del usuario: menos ceremonia, y `mode-watcher` afuera

El usuario cortó la delegación de T2 enojado, y tiene razón. Sus palabras, citadas en lo sustantivo y
sin repetir los insultos personales (esto se commitea en el repo de la cátedra):

> ¿qué mierda es mode watcher? ¿una dependencia para observar el cambio de tema?

> Estoy viendo que hizo un test... ¿¡para los tokens de color!? [...] ¡mantené las cosas simples! ¡no
> puede ser que tardes tanto en hacer 8 tareas de mierda!

Qué se cambió:

- **Fuera el spec de tokens.** Yo había delegado a un worker un `src/lib/tokens.spec.ts` con chequeos de
  contraste WCAG de los pares documentados. Se borró. Es CSS estático y su contrato se lee mejor en el
  propio archivo: acá la verificación útil es `pnpm lint`, `pnpm check` y `pnpm build`. Inventar una
  prueba para un archivo declarativo fue ceremonia, no cobertura.
- **Fuera `mode-watcher`.** Era una dependencia para poner y sacar la clase `.dark`. Se reemplazó por un
  script en `frontend/web/src/app.html` que resuelve el tema antes del primer pintado —para que no haya
  destello del tema equivocado— y un botón en `+page.svelte` que alterna la clase y guarda la
  preferencia en `localStorage`. S4 queda corregida en consecuencia.
- **Fuera la forense del CLI.** T4 se llevó la mayor parte del tiempo: reimplementar el `encodePreset`
  en base62 para poder pasar un preset y averiguar por qué `init` no cierra sin TTY. El resultado sirve
  y está en L8, pero costó mucho más de lo que valía para una feature de cátedra. **Lección:** frente a
  una herramienta de terceros, dos intentos y después camino alternativo documentado; no arqueología.

Verificación observada de T2 (`905b0c1`): `pnpm run lint` exit 0 · `pnpm check` 0 errores / 0
advertencias · `pnpm build` exit 0. Nada más: así lo pidió el usuario.

### L10 — Pedido del usuario: vuelta a los temas de Pomody, Lucide, y ModeWatcher adentro

Pedido verbatim:

> Me arrepentí sobre los temas y los íconos.
>
> - Para los temas: copiá los 3 temas de pomody (~/dev/pomody/src/app.css). Creo que shadcn-svelte recomienda usar ModeWatcher, que no sé qué es, pero si lo recomienda y es fácil de implementar, hazlo.
> - Respecto a los íconos: usemos Lucide.
> - Sin tests de tokens.

Antes de escribir se verificaron las tres afirmaciones que el pedido apoya, porque dos son falsas y una es
cierta:

1. **shadcn-svelte sí recomienda `mode-watcher`** para el modo oscuro (`docs/dark-mode/svelte`: instalar
   `mode-watcher`, `<ModeWatcher />` en el layout raíz, `toggleMode`/`setMode` en el botón). Es fácil.
2. **`mode-watcher` no puede expresar 3 temas.** Su API sólo conoce `light`, `dark` y `system`
   (`userPrefersMode`), y aplica o saca la clase `.dark` en el `<html>`. Pomody, en cambio, maneja sus tres
   paletas con un atributo `data-theme` propio (`dark`, `dawn`, `oled`) y no usa `mode-watcher`.
3. **`components.json` ya declara Lucide** (`"iconLibrary": "lucide"`): la decisión de íconos no requiere
   tocar nada de configuración.

Se le planteó el choque al usuario con el mecanismo verificado y eligió: **`mode-watcher`, con Dawn para
Light y OLED para Dark**. Consecuencias, aplicadas en S3, S4 y S8:

- Se copian **dos** de las tres paletas de Pomody, y se descarta la tercera (Rosé Pine base) junto con la
  indirección `data-theme`. El modo claro/oscuro lo manejan la clase `.dark` y ModeWatcher.
- El script anti-FOUC de `app.html` se borra: pasa a ser responsabilidad de la dependencia.
- El botón de tema pasa a `toggleMode` de ModeWatcher, con íconos Sol/Luna de `@lucide/svelte`.
- T5 (shim de Solar) queda cancelada: no hay nada que mapear.
- El spec de tokens sigue sin existir, ahora como decisión explícita (L9 lo había borrado; L10 lo confirma).

### L11 — T10 y T11 ejecutados: temas, ModeWatcher y Lucide

**T10** (`4d730dc`, `feat(web): adoptar los temas de Pomody con ModeWatcher e íconos Lucide`):

| Archivo | Qué cambió |
| :--- | :--- |
| `src/routes/layout.css` | Reescrito. Se va la paleta Kanagawa completa (dos capas, `chart-1..5` y `sidebar-*`) y entran las dos paletas de Pomody: Dawn en `:root` y OLED en `.dark`, con su capa de marca y su mapeo de `@theme inline`. |
| `src/routes/+layout.svelte` | `<ModeWatcher />`. |
| `src/app.html` | Se elimina el script anti-FOUC propio. |
| `src/routes/+page.svelte` | El botón de tema pasa a `toggleMode` de ModeWatcher, con los íconos `sun` y `moon` de `@lucide/svelte`. |
| `frontend/web/package.json` | `mode-watcher@1.1.0` y `@lucide/svelte@1.57.0` en `dependencies`. |

**Hallazgo: el toggle no se puede medir en el instante siguiente al clic.** La primera medición leyó la clase
sin cambios justo después de hacer clic. No era un defecto: ModeWatcher aplica la clase en un efecto de
Svelte, que corre después del evento. Medido de nuevo esperando el cambio, la clase cambia siempre. Sin esa
distinción habría reportado un bug inexistente.

**Verificación observada de T10.** `pnpm dev` en 5173 y Edge headless vía `playwright` (canal `msedge`,
script temporal fuera del repo, descartado después):

| Comprobación | Resultado |
| :--- | :--- |
| `pnpm lint` / `pnpm check` / `pnpm build`, sobre el árbol y después sobre el commit | 0 · 0 errores y 0 advertencias · 0 |
| Sistema en claro y sin preferencia guardada | Dawn: `--bg-base #faf4ed`, `--accent-love #b4637a`, fondo `rgb(250,244,237)` |
| Sistema en oscuro y sin preferencia guardada | OLED: `--bg-base #000000`, `--accent-love #eb6f92`, fondo `rgb(0,0,0)` |
| Clic en el botón (desde claro) | pasa a `.dark`, fondo `rgb(0,0,0)`, guarda `"dark"` |
| Clic en el botón (desde oscuro) | vuelve a claro, fondo `rgb(250,244,237)`, guarda `"light"` |
| Seguimiento del sistema | con la clave ausente quedaba `"system"` y la paleta siguió al `prefers-color-scheme` simulado |

**Lo que se pierde al sacar el script de `app.html`, medido y no supuesto.** Con `ssr = false` no hay HTML del
servidor que le lleve a ModeWatcher su script: la clase llega con el bundle. Cronología medida en `dev` (modo
oscuro): primer pintado del lienzo vacío a **344 ms**, la clase `.dark` a **362 ms**, primer pintado con
contenido a **424 ms**. Es decir: el contenido visible ya sale oscuro —no hay destello del tema—, pero el
lienzo vacío del arranque puede pintarse blanco unos milisegundos antes. En localhost no se percibe; si alguna
vez importara, se resuelve con unas líneas en `app.html` que lean la misma clave antes del pintado.

**T11.** `AGENTS.md`: sección "Interfaz y estilo" actualizada (temas Rosé Pine, `mode-watcher`, íconos Lucide).

**Riesgo declarado.** `Risk` item 5 (dependencias): `mode-watcher` y `@lucide/svelte` son dependencias nuevas
de ejecución. Sin verificación independiente, por la restricción vigente de L3/L6.
