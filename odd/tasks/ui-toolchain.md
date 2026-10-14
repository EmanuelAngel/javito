# Toolchain de UI y sistema de tokens Kanagawa — EN PAUSA

Feature sobre la rama `feat/ui-toolchain`. **Detenida por pedido del usuario**: queda planteada para
retomarla en otra sesión. Este documento es la fuente de verdad para retomar.

Estado del árbol al detenerse: **hay cambios sin commitear** (del `sv add` que corrió el usuario) y un
archivo basura en la raíz. Ver "Estado actual verificado".

## Specs

- **S1 — Add-ons de SvelteKit.** eslint, prettier, vitest, playwright y tailwindcss. Tailwind con los
  plugins `typography` y `forms`.
- **S2 — shadcn-svelte** bajo `src/lib/componentes/ui`. `ui/` es vendorizada: se excluye de prettier
  y de eslint, **no** de `svelte-check` (decisión del usuario).
- **S3 — Íconos: opción B.** Un módulo propio que re-exporta los íconos de `@solar-icons/svelte` bajo
  los nombres que shadcn-svelte espera, mapeado por alias. `ui/` queda intacto y `shadcn update` no lo
  revierte. Un solo pack de íconos.
- **S4 — Tokens Kanagawa**, claro/oscuro, con la estructura de Pomody (`~/dev/pomody/src/app.css`):
  capa de marca, capa semántica de shadcn, `@theme inline`, `@layer base`. Sin la indirección
  `data-theme` de Pomody (un solo tema: `:root` + `.dark`). Dragon para oscuro, Lotus para claro, del
  repo oficial `rebelot/kanagawa.nvim`. Los roles semánticos se mapean al miembro de la paleta que
  **pasa AA**, no al que usa el tema de neovim.
- **S5 — Interfaz en español**, incluidos aria-labels.
- **S6 — Prettier se muda al frontend.** La raíz conserva husky, commitlint y lint-staged.
- **S7 — Base color de shadcn: `stone`** (inmutable después del `init`).
- **S8 — Vitest** sólo `unit` al principio. **Playwright** sin demo.

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
- El e2e contra el backend real necesita MySQL, que llega con S2. **Sigue pendiente y por eso todavío
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

## Diseño del tema Kanagawa (calculado, listo para escribir)

Ratios WCAG medidos sobre la paleta oficial. Regla aplicada: **se mantiene el rol de Kanagawa donde
pasa AA; se desvía sólo donde falla, documentando el motivo.**

### Oscuro — Dragon, fondo `#181616`

| Token | Valor | Fuente | Ratio |
| :--- | :--- | :--- | :--- |
| `--background` | `#181616` | dragonBlack3 | — |
| `--foreground` | `#c5c9c5` | dragonWhite | **10.76** ✓ |
| `--card` | `#393836` | dragonBlack5 | **1.54** vs fondo (único escalón perceptible) |
| `--card-foreground` | `#c5c9c5` | — | 6.99 ✓ |
| `--popover` / `--secondary` / `--muted` | `#282727` | dragonBlack4 | 1.21 vs fondo |
| `--accent` | `#393836` | dragonBlack5 | — |
| `--muted-foreground` | `#a6a69c` | dragonGray | **7.34** ✓ (sobre card 4.77 ✓) |
| `--primary` | `#8ba4b0` | dragonBlue2 | 6.90 ✓ |
| `--primary-foreground` | `#181616` | — | 6.90 ✓ |
| `--destructive` | `#c4746e` | dragonRed | 5.21 ✓ |
| `--destructive-foreground` | `#181616` | — | 5.21 ✓ |
| `--border` / `--input` | `#54546D` | sumiInk6 | **1.60 contra la tarjeta**, 2.46 vs fondo (el "borde nítido") |
| `--ring` | `#8ba4b0` | — | — |
| `--chart-1..5` | `#8ba4b0` `#8a9a7b` `#c4b28a` `#a292a3` `#b6927b` | dragonBlue2, dragonGreen2, dragonYellow, dragonPink, dragonOrange | todos ≥ 6 ✓ |
| `--sidebar` | bg `#12120f`, fg `#c5c9c5`, border `#393836`, accent `#393836`, ring/primary `#8ba4b0` | dragonBlack1 | — |

**Desvío 1 (oscuro):** el tema oficial de Kanagawa usa `dragonAsh #737c73` para comentarios →
**4.17:1, no pasa AA** para texto normal. Se usa `dragonGray #a6a69c` (7.34:1).

### Claro — Lotus, fondo `#f2ecbc`

| Token | Valor | Fuente | Ratio |
| :--- | :--- | :--- | :--- |
| `--background` | `#f2ecbc` | lotusWhite3 | — |
| `--foreground` | `#545464` | lotusInk1 | **6.19** ✓ (sobre tarjeta 7.28 ✓) |
| `--card` / `--popover` | `#fffdf2` | blanco cálido, fuera de la paleta | 1.18 vs fondo |
| `--secondary` / `--muted` | `#e7dba0` | lotusWhite4 | — |
| `--accent` / `--border` / `--input` | `#d5cea3` | lotusWhite0 | border vs tarjeta 1.56 |
| `--muted-foreground` | `#5f5c51` | lotusGray2 oscurecido | **5.58** ✓ (sobre tarjeta 6.56) |
| `--primary` | `#4d699b` | lotusBlue4 (oficial) | 4.59 ✓ |
| `--primary-foreground` | `#f2ecbc` | — | 4.59 ✓ |
| `--destructive` | `#b0353f` | lotusRed oscurecido | **5.11** ✓ |
| `--destructive-foreground` | `#f2ecbc` | — | 5.11 ✓ |
| `--warning` | `#8a5514` | lotusOrange2 oscurecido | **5.16** ✓ |
| `--ring` | `#4d699b` | — | — |
| `--chart-1..5` | `#4d699b` `#6f894e` `#b35b79` `#4e8ca2` `#624c83` | lotusBlue4, lotusGreen, lotusPink, lotusTeal1, lotusViolet4 | todos ≥ 3 ✓ |
| `--sidebar` | bg `#e7dba0`, fg `#545464`, border `#d5cea3`, accent `#d5cea3`, ring/primary `#4d699b` | — | — |

La tarjeta clara no gana por superficie (1.18:1, todas las candidatas de la paleta dan 1.1–1.33) sino
**por contraste de texto**: con `#dcd5ac` el texto quedaba en 5.0 y el muted rozando 4.51, con
`#fffdf2` suben a 7.28 y 6.56.

**Desvíos (claro):**

| Token | Oficial | Ratio | Reemplazo | Ratio |
| :--- | :--- | :--- | :--- | :--- |
| muted-foreground | `lotusGray2 #716e61` | 4.26 ✗ | `#5f5c51` | 5.58 ✓ |
| destructive | `lotusRed #c84053` | 4.06 ✗ | `#b0353f` | 5.11 ✓ |
| warning | `lotusOrange2 #e98a00` | 2.16 ✗ | `#8a5514` | 5.16 ✓ |

### Comunes

- `--radius: 0.625rem` (como Pomody).
- `@custom-variant dark (&:is(.dark *))`, `@theme inline` con las dos capas mapeadas, y `@layer base`
  con `@apply border-border outline-ring/50` y el scrollbar fino, como Pomody.
- **Tipografías: Inter Variable + JetBrains Mono Variable**, vía `@fontsource-variable/inter` y
  `@fontsource-variable/jetbrains-mono`. Es el precedente de Pomody y se decidió en L6.
- El claro/oscuro **no usa `mode-watcher`**: un script propio en `app.html` resuelve el tema antes del
  primer pintado y un botón en la página lo alterna, guardando la preferencia en `localStorage`. Para un
  SPA estático son unas pocas líneas y una dependencia menos (L9).

## Tasks (replanteadas sobre el estado real)

Orden vigente desde L6. El commit de cada unidad de trabajo va en la rama `feat/ui-toolchain`.

| ID | Tarea | Estado |
| :--- | :--- | :--- |
| T1 | Tailwind con `typography` + `forms` | Hecho (se commitea en T8) |
| T8 | **Limpieza + commit del toolchain.** Borrar `src/routes/demo/`, `src/lib/vitest-examples/` y el `DemoLinks` de `+layout.svelte`; commitear el estado del `sv add`. Absorbe el T9 original: era un commit de dos líneas y el toolchain debe llegar sin andamios de demo | **Hecho** en `e6b2cab`, junto con T3 y T9 |
| T3 | eslint y prettier en el frontend: efectivo, borrar el prettier de la raíz, que lint-staged delegue, y `ignores` de `ui/` en eslint + `.prettierignore` | **Hecho dentro de T8** (L7): sin esto no se podía commitear nada del SPA |
| T6 | vitest y playwright: arreglar `playwright.config.ts` (pnpm, puerto, no reinstalar navegadores a cada corrida) | **Hecho** en `5ea34e4` (L8) |
| T4 | shadcn-svelte con `--css src/routes/layout.css` y alias a `src/lib/componentes/ui`; base color `stone`. **Adelantado antes de T2** (L6): el `init` escribe su propio bloque de tokens en el CSS y pisaría el de Kanagawa | **Hecho** en `b4e6a61` (L8), con una parte pendiente: el `init` no pudo cerrarse sin TTY |
| T2 | Sistema de tokens Kanagawa en `src/routes/layout.css` + claro/oscuro + reescribir `+page.svelte` con tokens. Valores ya calculados arriba | **Hecho** en `905b0c1`. Sin `mode-watcher` y **sin spec de tokens** (L9) |
| T5 | Shim de íconos Solar (`@solar-icons/svelte@2.3.2`) | Pendiente |
| T7 | Integración y documentación: scripts de raíz, `AGENTS.md`, README | **Hecho** (L9) |
| T9 | — | **Absorbido por T8** (L6) |

`T5` queda **bloqueada a propósito**: hasta que un componente generado por el CLI importe un ícono no
existe conjunto de nombres que mapear, y un shim parcial tiene más chances de romper el primer `add`
que de servir. Primer paso, cuando llegue ese momento: instalar `@solar-icons/svelte`, crear un módulo
bajo `src/lib/` que re-exporte los íconos con los nombres que usa el componente generado y aliñar
`@lucide/svelte/icons/*` a ese módulo desde `vite.config.ts`, sin tocar `ui/`.

## Decisiones pendientes

1. **`$lib` deprecado** (Hallazgo 1): ¿mantener el alias deprecado o migrar los alias de shadcn a
   `#lib`? Recomiendo mantener por ahora y documentarlo; migrar cuando SvelteKit lo exija, porque el
   CLI de shadcn todavía espera `$lib`.
2. **De dónde sale `paths` en `tsconfig.json`** (Hallazgo 2): hay que resolverlo antes de T4.
3. **Tipografías** del tema (Hallazgo del diseño): ¿Inter + JetBrains Mono como Pomody, u otras?
4. **Datos de producto ya decididos que no cambian:** interfaz en español, `ui/` fuera de formato y
   lint pero con typecheck, base color `stone`, íconos por shim, contraste por encima de fidelidad.

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
