# Toolchain de UI y sistema de tokens Kanagawa

Feature sobre `feat/scaffold-monorepo`. Aplica las decisiones del usuario del 2026-10-14 sobre la
cadena de UI del frontend y el tema visual.

## Specs

- **S1 — Add-ons de SvelteKit.** Agregar eslint, prettier, vitest, playwright y tailwindcss desde
  `sv add`. Tailwind con los plugins `typography` y `forms`.
- **S2 — shadcn-svelte.** Bajo `src/lib/componentes/ui`. `ui/` es carpeta vendorizada: se excluye de
  prettier y de eslint, **no** de `svelte-check` (decisión del usuario).
- **S3 — Íconos: opción B del plan.** Un módulo propio que re-exporta los íconos de
  `@solar-icons/svelte` bajo los nombres que shadcn-svelte espera, mapeado por alias. `ui/` queda
  intacto y `shadcn update` no lo revierte. Un solo pack de íconos en toda la UI.
- **S4 — Tokens Kanagawa.** Un tema con claro/oscuro, siguiendo la estructura de Pomody
  (`~/dev/pomody/src/app.css`): capa de marca, capa semántica de shadcn, `@theme inline` y
  `@layer base`. Sin la indirección `data-theme` de Pomody (un solo tema: `:root` + `.dark`).
  Kanagawa Dragon para oscuro, Kanagawa Lotus para claro, del repo oficial `rebelot/kanagawa.nvim`.
  Los roles semánticos se mapean al miembro de la paleta que **pasa AA**, no al que usa el tema de
  neovim: `dragonAsh` (4.17:1) y `lotusGray3` (2.93:1) fallan para texto de interfaz.
- **S5 — Interfaz en español.** Todo texto de UI, incluidos aria-labels y textos de los componentes.
- **S6 — Prettier se muda al frontend.** La raíz conserva husky, commitlint y lint-staged.
- **S7 — Base color de shadcn: `stone`** (inmutable después del `init`).
- **S8 — Vitest** sólo `unit` al principio. **Playwright** sin demo; el e2e contra el backend real
  necesita MySQL, que llega en S2.

## Tasks

| ID | Tarea | Commit |
| :--- | :--- | :--- |
| T1 | Restaurar `$lib` y agregar Tailwind (typography + forms) | `feat(web): agregar tailwind con typography y forms` |
| T2 | Sistema de tokens Kanagawa claro/oscuro + mode-watcher + adaptar la página | `feat(web): agregar sistema de tokens Kanagawa` |
| T3 | eslint y prettier en el frontend, con `ui/` excluido de formato y lint | `chore(web): agregar eslint y mover prettier al frontend` |
| T4 | shadcn-svelte con alias a `src/lib/componentes/ui` | `feat(web): inicializar shadcn-svelte` |
| T5 | shim de íconos Solar | `feat(web): unificar los íconos con Solar vía alias` |
| T6 | vitest y playwright | `chore(web): agregar vitest y playwright` |
| T7 | integración: scripts, lint-staged, AGENTS.md y README | `docs: documentar la cadena de UI` |

## Log

### L1 — Pedido original (verbatim)

> Me olvidé de decirte que SvelteKit no lo quería simple, lo quería con algunas cosas que ofrece el mismo `sv create` que entiendo se pueden agregar con `sv add`: - eslint. - prettier. - vitest. - playwright. - tailwindcss. Fuera de lo que ofrece SvelteKit quiero tres cosas: - shadcn-svelte (tenés la skill disponible) es la herramienta para construir las interfaces bajo src/lib/componentes/ui (ui/ pasaría a ser una carpeta vendor, así que excluíla de los checks). - Solar Icons Svelte ... - Crear un sistema de tokens css como el de Pomody (~/dev/pomody/src/app.css) pero en lugar de tener tres temas basados en Rosé Pine, para Javito quiero uno con tema claro/oscuro y con la paleta de colores Kanagawa Dragon.

### L2 — Decisiones del usuario

1. Íconos: **opción B (shim de alias)**.
2. Interfaz: **en español**.
3. Prettier: **mover al frontend**.
4. `ui/`: **dejar el check de tipos adentro**.
5. Contraste: **"lo que recomiendes y mejor sea para contraste"**.

### L3 — Restricciones de esta corrida

"no tardes tanto esta vez, no es algo tan serio y estas cosas no son tan complicadas. Y sin
gentle-verify esta vez". Sin verificación independiente; se corre `check`/`build`/`lint` y se mide el
contraste, que es determinista y no necesita ojo externo.
