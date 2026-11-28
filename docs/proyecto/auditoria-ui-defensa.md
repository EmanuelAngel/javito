# Auditoría de UI/UX para la defensa · 27-11-2026

**Qué es:** la lista priorizada que cierra el bloque 23–27/11 del [plan de la defensa](./plan-defensa.md) y
**congela el scope** de las mejoras que se ejecutan del 28/11 al 01/12. **Qué no es:** un rediseño ni un
backlog. Lo que no está acá no entra, y lo que está en §5 sólo entra si los slices de §4 cierran antes de
tiempo.

**Cómo se produjo:** recorrido manual de la app corriendo en local (MySQL + WebAPI en `:5256` + SPA en
`:5173`) con `usuario@javito.local`, el 27/11 entre las 01:10 y las 01:43. Los hallazgos son del usuario;
la lectura del código y la agrupación en unidades de trabajo son del agente.

**Regla heredada del plan (§1):** ninguna mejora nueva entra después del **27-11**. Esta lista es ese
corte.

---

## 1. Los hallazgos

Prioridad tal como la asignó el usuario en el recorrido. La columna _causa_ es la lectura del código, y
sirve para estimar: no cambia la prioridad.

| #   | Pantalla | Hallazgo                                                                                                                             | Prioridad                     | Causa en el código                                                                                                                                                        |
| :-- | :------- | :----------------------------------------------------------------------------------------------------------------------------------- | :---------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | Global   | Mobile no tiene navegación dedicada: el header no colapsa. Pide hamburguesa con drawer.                                              | **Alta**                      | `routes/(app)/+layout.svelte:46-133`: una sola fila con 8 enlaces (4 de usuario + 4 de admin) más nombre, tema, avatar y Salir. No hay breakpoint que los esconda.        |
| 2   | /hoy     | El selector de rutina no debería ser genérico: pide tabs redondeadas grandes, de distintos colores.                                  | Baja                          | `lib/components/filtro-de-rutina.svelte`: un `NativeSelect` de 35 líneas.                                                                                                 |
| 3   | /hoy     | El texto de los hábitos se trunca en mobile.                                                                                         | **Alta**                      | `lib/components/fila-del-dia.svelte:46`: `truncate` en el título, dentro de un `flex` con el bloque de acción en `shrink-0` (`:70-112`).                                  |
| 4   | /hoy     | Duda: no sabe si el responsive está roto o se le bugueó el navegador.                                                                | Baja                          | Sin verificar a propósito: primero se arregla #1 (pedido explícito del usuario).                                                                                          |
| 5   | /hoy     | Las categorías deberían ser badges de color, distintivos entre sí.                                                                   | Media                         | `fila-del-dia.svelte:48`: la categoría es texto plano. Hay 6 tokens de acento ya definidos (`routes/layout.css:107-112`).                                                 |
| 6   | Global   | Se usa mucho el color rose.                                                                                                          | Baja                          | `layout.css:42`: `--primary: #d7827e` (Rose Pine Dawn). El primario _es_ rose: es la paleta, no un exceso de uso.                                                         |
| 7   | /hoy     | En pantallas chicas los días de la semana se aplastan: la acción (`− X/X páginas +`, `Hoy me mantuve`) les come el espacio.          | Media                         | Mismo `flex` de `fila-del-dia.svelte`: la semana (`SemanaDelHabito`, `:67`) compite con el bloque de acción.                                                              |
| 8   | /habitos | Mismo pedido de badges de categoría que #5.                                                                                          | Media                         | `lib/components/habitos-lista.svelte:75`: texto plano.                                                                                                                    |
| 9   | /habitos | El texto de los hábitos se trunca y la card no ubica bien los botones de acción.                                                     | **Alta**                      | `habitos-lista.svelte:73`: `truncate`; acciones en `shrink-0` (`:83-97`) contra un `min-w-0` que no alcanza.                                                              |
| 10  | /habitos | Paginación o buscador. El usuario mismo duda: "¿cuántos hábitos tendría una persona?". Pide buscador **de UI**, sin consultas extra. | Media                         | Ninguna: la pantalla trae la lista completa y no filtra. El seed tiene 14 hábitos.                                                                                        |
| 11  | /habitos | Muchas advertencias de Svelte en consola. No es de UI/UX, se anota para después.                                                     | Media                         | Ver §6.                                                                                                                                                                   |
| 12  | /habitos | Archivar debería pedir confirmación y explicar qué hace, en lenguaje llano.                                                          | Media                         | `habitos-lista.svelte:87`: comentario explícito de que **no** pide confirmación porque es reversible. Hay una decisión de diseño tomada en contra; ver §7.                |
| 13  | /rutinas | Los inputs de crear rutina son nativos, no los de shadcn-svelte.                                                                     | Baja                          | `lib/components/rutina-form.svelte:19-25` (`<input>`), `:33-38` (`<textarea>`), `:44-50` (`<button>`). Contraste: `habito-form.svelte` ya usa `Field`/`Input`.            |
| 14  | /rutinas | La card seleccionada se ve mal y la «×» no se entiende. Pide que se parezca al selector de /hoy.                                     | Media                         | `lib/components/rutinas-lista.svelte:33-35`: **no es una ×**, es un `PlusIcon` con `rotate-45` usado como marca de selección. No cierra nada.                             |
| 15  | /rutinas | Botón para filtrar rutinas inactivas, sólo UI.                                                                                       | Baja                          | `rutinas-lista.svelte:51-59`: las inactivas ya van en una sección aparte, debajo.                                                                                         |
| 16  | /rutinas | El textarea de descripción está pegado al botón de crear: se puede crear o editar la rutina sin querer al intentar expandirlo.       | _sin asignar_ — se toma Media | `rutina-form.svelte:33-50`: el `form` usa `gap-2` para todo; el textarea tiene `rows="2"` y es redimensionable, así que su handle de resize vive justo encima del submit. |
| 17  | /hoy     | La card de hábito necesita espacio: hay que mejorar su presentación.                                                                 | **Alta**                      | `fila-del-dia.svelte:42-113`: `justify-between` con `px-5 py-4` fijos, sin breakpoint.                                                                                    |

---

## 2. Las 17 no son 17 cosas

Al leer el código, seis hallazgos son **la misma card**: #3, #5, #7, #8, #9 y #17. Los dos títulos
truncados (`fila-del-dia.svelte:46` y `habitos-lista.svelte:73`) y la semana aplastada nacen del mismo
layout: un `flex` con el contenido en `min-w-0` y el bloque de acción en `shrink-0`, sin breakpoint y con
el título en `truncate`. Pedir "más espacio" (#17) es pedir que ese `flex` cambie de comportamiento en
angosto.

Dos más son **el mismo selector**: #2 y #14. El de /hoy es un `NativeSelect`; el de /rutinas es una lista
de cards con una `PlusIcon` rotada que se lee como una ×. Unificarlos resuelve los dos y de paso saca la
«×» que no hace nada.

Quedan tres unidades sueltas: la navegación mobile (#1), la confirmación de archivar (#12) y el formulario
de rutina (#13 + #16, que están en el mismo archivo y se arreglan juntos).

---

## 3. Unidades de trabajo

| Unidad                               | Hallazgos                            | Por qué junto                                                                                                            | Peso  |
| :----------------------------------- | :----------------------------------- | :----------------------------------------------------------------------------------------------------------------------- | :---- |
| **A. La card de hábito**             | 3, 5, 7, 8, 9, 17 (y 6, de arrastre) | Un solo componente resuelve el truncado, la semana aplastada y los badges. Es la pantalla que más se muestra en la demo. | Alto  |
| **B. Navegación mobile**             | 1 (+ verificación de 4)              | El header no tiene versión chica. Sin esto la app no se sostiene a 390px.                                                | Alto  |
| **C. Selector de rutina único**      | 2, 14                                | Mismo control en dos pantallas con dos formas distintas.                                                                 | Medio |
| **D. Formulario de rutina**          | 13, 16                               | Mismo archivo; el textarea pegado al botón es un riesgo de envío accidental.                                             | Bajo  |
| **E. Archivar con red de seguridad** | 12                                   | Cambia el texto y el gesto de una acción que hoy asusta.                                                                 | Medio |
| **F. Filtro de /habitos**            | 10, 15                               | Sólo si sobra: un buscador de UI sobre la lista ya cargada no pide backend.                                              | Bajo  |

---

## 4. Scope congelado

**Propuesta: A + B + C + E.** Cuatro unidades, en ese orden, con `pnpm check` y `pnpm lint` en verde al
cerrar cada slice.

- **A** toca `fila-del-dia.svelte` y `habitos-lista.svelte`: conviene un solo corte para no arreglar dos
  veces el mismo `flex`.
- **B** toca `(app)/+layout.svelte` y suma un componente de drawer. Es el único que agrega una pieza nueva.
- **C** toca `filtro-de-rutina.svelte` y `rutinas-lista.svelte`: un componente, dos consumidores.
- **E** es texto y un diálogo: el slice más chico de los cuatro.

> **Sin confirmar.** Esta sección congela el scope recién cuando el usuario la aprueba. Hasta entonces es
> una propuesta.

---

## 5. Fuera del freeze

Entran sólo si A–E cierran antes del 01/12:

- **D. Formulario de rutina** (#13, #16) — el quinto candidato natural; es el más barato de los que quedan.
- **F. Buscador de /habitos** (#10) — filtrado en cliente sobre la lista ya cargada.
- **#6 (menos rose)** — no se ataca como hallazgo propio: se resuelve de arrastre si A usa los tokens de
  acento para las categorías (`layout.css:107-112`: `love`, `rose`, `pine`, `foam`, `iris`, `gold`).
- **#15 (filtro de inactivas)** — las inactivas ya están separadas en su propia sección.

---

## 6. Deuda técnica detectada en el recorrido (#11)

No es UI/UX y no entra en el freeze, pero quedó registrada con su causa exacta.

1. **`window_fetch_in_load`** — los `load` piden con el `fetch` global en vez del que SvelteKit inyecta:
   `lib/api/pedir.ts:38` llama a `fetch` directo y los módulos de API (`lib/api/dias.ts`, `rutinas.ts`,
   `habitos.ts`, `categorias.ts`) no reciben el `fetch` del `load`. Por eso el warning aparece en
   `/api/sesion/yo`, `/api/dias/…`, `/api/rutinas`, `/api/habitos` y `/api/categorias`.
2. **`ownership_invalid_mutation`** — `habito-form.svelte:103` y `:116` mutan el prop `estado` sin
   `bind:estado` desde `routes/(app)/habitos/+page.svelte`. El propio mensaje de Svelte dice el arreglo.
3. **`routes/(app)/admin/categorias/+page.svelte` tiene 210 líneas** — viola la regla de 150 del proyecto
   (`AGENTS.md`). Es deuda previa, no de este recorrido.

---

## 7. Decisiones abiertas

1. **¿Dónde se muestra la defensa: a 390px o en la compu?** Decide el peso real de B. Si la demo va en
   pantalla grande, el drawer casi no se ve.
2. **Archivar: confirmación o deshacer.** El código decidió a propósito no confirmar
   (`habitos-lista.svelte:87`) porque la baja es reversible y se deshace con Reactivar. El hallazgo #12
   pide confirmación. Las dos cosas pueden convivir: un aviso que explique («se puede reactivar después»)
   sin un modal que frene cada archivado.
3. **#16 quedó sin prioridad** en el recorrido; se asume Media por ser un envío accidental.

---

## 8. Seguimiento

Marcado al **28-11-2026**. El estado dice _qué línea de código_ lo sostiene, no qué se intentó: un
hallazgo se marca cerrado cuando el defecto ya no existe en el árbol.

### Unidades del freeze (§4)

| Unidad                               | Hallazgos              | Estado       | Dónde quedó                                                                                                                                                                                                   |
| :----------------------------------- | :--------------------- | :----------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **B. Navegación mobile**             | 1, 4                   | ✅ Cerrado   | `nav-mobile.svelte` + `(app)/+layout.svelte`; «Hoy» en el drawer, commit `5fdcca5`                                                                                                                            |
| **C. Selector de rutina único**      | 2, 14                  | ✅ Cerrado   | `selector-de-rutinas.svelte`, un consumidor por pantalla; `filtro-de-rutina` y `rutinas-lista` borrados, commit `84f6f36`                                                                                     |
| **A. La card de hábito**             | 3, 5, 7, 8, 9, 17 (+6) | ⚠️ Parcial   | `fila-del-dia` y `habitos-lista` apilados en angosto, sin `truncate`, con `chip-de-categoria` (commits `f85ae6e`, `994181b`). **Falta #6**: el primario sigue siendo rose y el badge de cuota lo usa en suave |
| **E. Archivar con red de seguridad** | 12                     | ⬜ Pendiente | `habitos-lista` sigue archivando sin aviso                                                                                                                                                                    |

### Hallazgo por hallazgo

| #   | Estado | Nota                                                                                                 |
| :-- | :----- | :--------------------------------------------------------------------------------------------------- |
| 1   | ✅     | Drawer mobile con todos los enlaces del rol y la ruta activa                                         |
| 2   | ✅     | Pastillas horizontales; reemplazó al `NativeSelect`                                                  |
| 3   | ✅     | Título sin `truncate`, envuelve                                                                      |
| 4   | ✅     | Verificado a 390px: el responsive no estaba roto, faltaba #1                                         |
| 5   | ✅     | Chip con acento por categoría (iris, love, foam, gold, pine)                                         |
| 6   | ⬜     | Sigue el primario rose; se usa en el badge suave y en la pastilla «Todos»                            |
| 7   | ✅     | La acción ya no compite por el ancho                                                                 |
| 8   | ✅     | Mismo chip en `/habitos`                                                                             |
| 9   | ✅     | Sin truncado y con las acciones repartidas debajo                                                    |
| 10  | ⬜     | Sin buscador en `/habitos` — era opcional (§5)                                                       |
| 11  | ⬜     | Deuda de §6, intacta: `fetch` global, `ownership_invalid_mutation`, 237 líneas en `admin/categorias` |
| 12  | ⬜     | Pendiente (unidad E)                                                                                 |
| 13  | ⬜     | Inputs nativos en `rutina-form` (unidad D, fuera del freeze)                                         |
| 14  | ✅     | Se fue el `PlusIcon` rotado: la selección se lee por relleno                                         |
| 15  | ➖     | Descartado: las inactivas ya tienen su fila y hacen falta para reactivar                             |
| 16  | ⬜     | Textarea pegado al submit (unidad D)                                                                 |
| 17  | ✅     | Tarjeta apilada en angosto, con más aire                                                             |

### Aparte del freeze

| Cosa                              | Estado | Nota                                                                                                                 |
| :-------------------------------- | :----- | :------------------------------------------------------------------------------------------------------------------- |
| Contraste de los roles semánticos | ⬜     | `text-primary` sobre fondo da 2.74:1 en Dawn; `destructive` apunta a love. Nunca se auditó consumidor por consumidor |
| Tipografía                        | ✅     | Plus Jakarta Sans (`fa5c9e9`)                                                                                        |
| Header fijo                       | ✅     | `sticky top-0` (`99c6f01`)                                                                                           |
| Verificación visual de A y C      | ✅     | Revisado a 390px y 1280px, Dawn y OLED                                                                               |

### Regla para seguir

Cuando un hallazgo se cierre, se cambia acá **y** se nombra el commit que lo sostiene. Si no hay commit,
no está cerrado.

---

## Log

- **L1** — Recorrido manual del usuario el 27/11 (01:10–01:43) sobre `/hoy`, `/habitos` y `/rutinas`.
  17 hallazgos con prioridad propia; `/artículos`, `/historial`, `/perfil` y `/admin/*` quedaron sin
  recorrer.
- **L2** — Ejecución del freeze del 27 al 28/11: B y C cerrados, A parcial (falta #6). Se agregó esta
  sección de seguimiento para no depender de la memoria de cada sesión.
