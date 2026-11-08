# Hábitos — edición, archivo y categoría

Cierra #69 (`HU-HAB-03`), #70 (`HU-HAB-04`) y #71 (`HU-HAB-05`). Sin cambios al esquema: `habito` ya
tiene todas las columnas y `registro_habito.meta_objetivo` guarda la meta con la que se cerró cada día.
El issue es la fuente; esto lo descompone.

## Specs

Transcripción literal de los issues (`docs/proyecto/historias-de-usuario.md:185-212`).

**#69 · HU-HAB-03 — Editar un hábito.** «Como Usuario quiero corregir la meta, la descripción o la
categoría de un hábito para ajustarlo sin perder lo que ya registré.»

- **S1** «Puedo cambiar título, descripción, categoría, tipo, meta, unidad y frecuencia»
- **S2** «Los cambios rigen desde ese día; **los días ya registrados conservan la meta con la que se
  cerraron**»
- **S3** «Cambiar el tipo (de `sí/no` a `contador`) no borra los registros anteriores»
- **S4** «Editar un hábito ajeno no es posible, ni siquiera escribiendo la URL a mano»

**#70 · HU-HAB-04 — Archivar y reactivar un hábito.** (pendiente; se detalla al encararlo)

**#71 · HU-HAB-05 — Elegir la categoría de un hábito.** «Como Usuario quiero elegir la categoría de
un hábito de una lista para clasificarlo sin escribir nada.»

- **S5** «Puedo elegir la categoría al crear el hábito y al editarlo»
- **S6** «El desplegable trae todas las categorías activas: son pocas, así que no se busca ni se
  pagina»
- **S7** «Una categoría desactivada no aparece entre las opciones»
- **S8** «Un hábito puede quedar sin categoría»
- **S9** «El hábito conserva la categoría que eligió, aunque después se desactive»

## Tasks

| ID | Specs | Ruta | Commit |
| :--- | :--- | :--- | :--- |
| T1 | S1-S4 | worker: `PUT /api/habitos/{id}` + día con la meta del registro + edición en `/habitos` | `feat/habitos-edicion` |
| T2 | #70 | — | — |
| T3 | S5-S9 | inline: categoría opcional en alta y edición (API + `habito-form`) | `feat/habitos-categoria-opcional` |

## Log

- **L1** (pedido): «Ok» a: «arranco con #34 y #69 mientras vos hacés otra cosa». Orden acordado:
  #69 → #71, #70 → #78.
- **L2** (evidencia): `DiaRepositorio.cs:26` lee `h.meta_valor`, no `r.meta_objetivo`. Editar la meta
  reescribiría los días pasados en pantalla (`5 / 3`), aunque `completado` siga guardado. S2 lo exige
  arreglar en T1.
- **L3** (decisión): la meta nueva rige desde el próximo registro. Un día ya registrado, incluido hoy,
  conserva la suya hasta que se lo vuelve a marcar: `PUT .../registros/{fecha}` ya copia la meta
  vigente del hábito.
- **L4** (decisión): la validación de la edición es la del alta, con una excepción que adelanta
  HU-HAB-05 («El hábito conserva la categoría que eligió, aunque después se desactive»): si
  `categoriaId` es la que el hábito ya tiene, incluida `null` (el seed tiene el 10 sin categoría), se
  acepta aunque esté inactiva. Cambiar a otra categoría exige que exista y esté activa.
- **L5** (decisión): un hábito archivado también se puede editar. El archivo es de #70.
- **L6** (evidencia T1): RED 6/41 en `HabitosEndpointTests` + `DiasEndpointTests` (el PUT no existía).
  GREEN: `pnpm check:api` 151/151 · `pnpm lint` limpio · `pnpm check:web` 0 · `pnpm test:web` 59/59 ·
  `svelte_autofixer` sin hallazgos en `habito-form`, `habitos-lista` y `(app)/habitos/+page.svelte`.
- **L7** (evidencia T1): `pnpm test:e2e` 3/4. Pasa `habitos.e2e.ts`, con el paso nuevo de edición. Falla
  `articulos.e2e.ts:200` por una causa ajena a T1: desde #67 un Usuario que abre `/articulos/nuevo`
  vuelve a `/hoy`, y la prueba esperaba el formulario para mostrar el 403. Va en su propio issue.
- **L8** (decisión, #71): «Sí, categoría opcional en el alta». S8 vale al crear y al editar: `null`
  deja de ser `categoria_requerida` (el código se elimina de la API y del mapeo del SPA) y el
  desplegable ofrece siempre «Sin categoría». S5-S7 y S9 ya estaban cubiertos por #21 y #69 (L4):
  la lista trae solo activas y suma la inactiva que el hábito ya tenía.
