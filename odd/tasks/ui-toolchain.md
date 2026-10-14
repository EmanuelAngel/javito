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

**Sin commitear** — todo esto lo dejó el `sv add` que corrió el usuario:

| Área | Estado |
| :--- | :--- |
| Tailwind | `tailwindcss@4.3`, `@tailwindcss/vite`, `@tailwindcss/forms`, `@tailwindcss/typography`. `tailwindcss()` en `plugins` de `vite.config.ts`. Archivo global en **`src/routes/layout.css`** con `@import 'tailwindcss'` y los dos `@plugin`. |
| eslint | `eslint@10` + `@eslint/js`, `typescript-eslint`, `eslint-plugin-svelte`, `globals`, `eslint-config-prettier`, `eslint.config.js` |
| prettier | `prettier@3.8` + `prettier-plugin-svelte` + `prettier-plugin-tailwindcss`, `prettier.config.js`, `.prettierignore` |
| vitest | `vitest@4`, `vitest-browser-svelte`, `@vitest/browser-playwright`; configuración **dentro de** `vite.config.ts` (dos projects: `client` y `server`) |
| playwright | `playwright@1.60`, `playwright.config.ts` |
| scripts nuevos | `lint`, `format`, `test:unit`, `test`, `test:e2e` en `frontend/web/package.json` |

**Lo que `sv add` dejó de más y hay que borrar:** `src/routes/demo/` (DemoLinks, playwright/+page.svelte,
page.svelte.e2e.ts), `src/lib/vitest-examples/`, y el `import DemoLinks` + `<DemoLinks />` en
`src/routes/+layout.svelte`.

**Archivo basura en la raíz del repo:** `pi-session-2026-10-14T02-56-49-716Z_01a13857-…html`. Es una
exportación de sesión de Pi que quedó tirada en el proyecto. **Borrarlo** (o ignorarlo) antes de
cualquier commit: no debe entrar al repo de la cátedra.

## Hallazgos que cambian el plan

### 1. El alias `$lib` está DEPRECADO en SvelteKit 3

`pnpm check` y `pnpm build` emiten:

```
config_option_deprecated_alias
The `config.alias` option is deprecated, and will be removed in a future version of SvelteKit.
Use subpath imports instead: https://svelte.dev/docs/kit/$lib
```

La restauración de `$lib` **funciona hoy** y es lo que el propio SvelteKit sugiere en el error
`module_removed_lib`, pero está en camino de salida. Decisión pendiente: mantenerla (funciona, avisa) o
migrar los alias de shadcn a `#lib` (durable, pero **el CLI de shadcn resuelve alias leyendo
`svelte.config.js`, que ya no existe** — hay que verificar si acepta subpath imports).

### 2. `tsconfig.json` perdió `paths`

```
tsconfig_invalid
Found issues while validating `tsconfig.json`:
  - "paths" was overwritten. Imports from "$lib" may not typecheck
```

El `tsconfig.json` actual **no tiene bloque `paths`**. `svelte-check` pasa, pero el aviso dice que el
typecheck de `$lib` es frágil. Hay que decidir de dónde debe salir ese `paths` (el propio plugin de
SvelteKit o uno explícito) y verificarlo.

### 3. El archivo global es `src/routes/layout.css`, no `src/app.css`

Los tokens Kanagawa van **ahí**. Y el `init` de shadcn debe recibir `--css src/routes/layout.css`. No
crear un segundo archivo global: la skill de shadcn lo prohíbe explícitamente.

### 4. `playwright.config.ts` viene con tres cosas a corregir

```ts
webServer: { command: 'npm run build && npm run preview', port: 4173 },
testMatch: '**/*.e2e.{ts,js}'
```

- Usa `npm` en un proyecto `pnpm`.
- El puerto 4173 es el de `vite preview`, no el 5173 del SPA.
- El script `test:e2e` corre `playwright install` en cada ejecución (baja navegadores).
- Y el e2e contra el backend real necesita MySQL, que llega con S2.

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
- **Tipografías: a definir.** Pomody usa Inter Variable + JetBrains Mono Variable vía
  `@fontsource-variable/*`. Es el precedente a copiar salvo que se prefiera otra cosa.
- Falta `mode-watcher` (`~1.1.0`) para el toggle claro/oscuro que recomienda shadcn.

## Tasks (replanteadas sobre el estado real)

| ID | Tarea | Estado |
| :--- | :--- | :--- |
| T1 | Tailwind con `typography` + `forms` | **Hecho, sin commitear** |
| T2 | Sistema de tokens Kanagawa en `src/routes/layout.css` + `mode-watcher` + reescribir `+page.svelte` con tokens. Valores ya calculados arriba | Pendiente |
| T3 | eslint y prettier en el frontend: efectivo, borrar el prettier de la raíz, que lint-staged delegue, y `ignores` de `ui/` en eslint + `.prettierignore` | Deps instaladas; la mudanza sin hacer |
| T4 | shadcn-svelte con `--css src/routes/layout.css` y alias a `src/lib/componentes/ui`; base color `stone` | Pendiente (resolver Hallazgos 1 y 2 antes) |
| T5 | Shim de íconos Solar (`@solar-icons/svelte@2.3.2`) | Pendiente |
| T6 | vitest y playwright: arreglar `playwright.config.ts` (pnpm, puerto, no reinstalar navegadores a cada corrida) | Deps instaladas; limpieza pendiente |
| T7 | Integración: scripts de raíz, `lint-staged`, `AGENTS.md`, README | Pendiente |
| T8 | **Limpieza:** borrar `src/routes/demo/`, `src/lib/vitest-examples/`, el `DemoLinks` de `+layout.svelte` y el `pi-session-*.html` de la raíz | Pendiente |
| T9 | Commitear el estado actual del `sv add` (hoy está sin commitear) | Pendiente |

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
