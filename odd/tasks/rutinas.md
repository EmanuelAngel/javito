# Rutinas — Puerta 3

Cierra `RUT-01` y `RUT-02`, los dos requisitos de cátedra que paga el mismo módulo: `RT-01` (N:M) y
`RT-06` (búsqueda asíncrona). Las tablas `rutina` y `rutina_habito` ya existen en `database.sql:90` y
`database.sql:101`; falta el código que las use.

## Specs

Transcripción literal de `docs/proyecto/historias-de-usuario.md:298-315`. Cada `S#` es un ítem de
aceptación, sin reformular.

**HU-RUT-01 — Crear una rutina.** «Como Usuario quiero agrupar hábitos bajo un nombre como `Mañana` o
`Corte de escritorio` para hacerlos en bloque y en orden.»

- **S1** — «Pido nombre obligatorio y descripción opcional»
- **S2** — «La rutina nace activa, vacía y sin hábitos»
- **S3** — «Puedo tener varias rutinas en el mismo momento del día»

**HU-RUT-02 — Agregar hábitos a una rutina.** «Como Usuario quiero agregar hábitos a una rutina
buscándolos para no recorrer la lista completa cada vez.»

- **S4** — «Busco el hábito por nombre mientras escribo y lo agrego desde ahí»
- **S5** — «La búsqueda consulta el servidor y solo devuelve **mis** hábitos activos»
- **S6** — «El mismo hábito no se puede agregar dos veces a la misma rutina»
- **S7** — «Un hábito puede estar en varias rutinas a la vez»
- **S8** — «Puedo quitar un hábito de la rutina sin archivarlo»

Fuera de alcance, por `docs/proyecto/plan-de-cierre.md:131`: `RUT-03` (ordenar), `RUT-04` (filtrar el
día) y `RUT-05` (editar y desactivar). Tampoco se agrega unicidad de nombre: `S3` pide lo contrario.

## Tasks

- **T1** · S1-S3 · inline · `d3ac1d9` · `feat(api): agregar alta y consulta de rutinas`
- **T2** · S4-S5 · inline · `97d83fa` · `feat(api): buscar hábitos por nombre para armar rutinas`
- **T3** · S2, S6-S7 · inline · `73035d6` · `feat(api): agregar hábitos a una rutina`
- **T4** · S8 · inline · `8fa9b3d` · `feat(api): quitar un hábito de una rutina sin archivarlo`
- **T5** · S4 · inline · `feat(web): pantalla de rutinas con buscador asíncrono de hábitos`
- **T6** · todas · inline · `docs(odd): cerrar la Puerta 3`

## Log

- **L1** — Pedido original, verbatim: «Analicemos cómo encarar lo que falta de la puerta 3 (RUT-01 y
  RUT-02)»
- **L2** — Corrección de routing, verbatim: «No, invoca explorer». El primer explorador
  (`mw2yfe5r-1-p1pc`) murió sin primer evento de modelo; el segundo (`mw2yln0a-2-v1s9`) devolvió la
  evidencia con la que se armó este documento.
- **L3** — Autorización, verbatim: «Arrancá».
- **D1** — La pantalla vive en `(app)/rutinas`, con enlace en el shell.
- **D2** — Sin e2e nuevo para `RT-06`: la lógica del buscador se extrae a un módulo `.ts` y se prueba
  con vitest y fake timers; el GIF del buscador filtrando lo graba el autor. Hueco aceptado: el binding
  con el DOM no queda cubierto end-to-end.
- **D3** — Backend completo y verificado antes de la UI: si el presupuesto se corta en T5, `RT-01`
  igual queda cerrado.
- **D4** — `rutina_habito.orden` es `NOT NULL` con `CHECK (orden >= 1)` y `RUT-03` está afuera: el alta
  asigna `COALESCE(MAX(orden), 0) + 1` en la misma sentencia, y quitar deja huecos en la numeración a
  propósito (todo lee `ORDER BY orden`).
- **D5** — Los escritores van en serie, no en paralelo: `BaseDeDatosDePrueba` recrea el schema
  `javito_test` desde `database.sql` al arrancar cada fixture, así que dos `dotnet test` simultáneos se
  pisan.
- **D6** — La búsqueda **no** lleva tope de resultados. Se propuso un `LIMIT` por higiene, pero con los
  hábitos de una cuenta doméstica un tope sólo esconde coincidencias que la persona espera ver; el `q`
  ya acota. Sí se conserva el escape de `%`, `_` y `\` en el patrón, que era el bug real de `LIKE`.
- **D7** — La búsqueda exige `activo = TRUE` **dentro del SQL de la búsqueda**
  (`AND (@patron IS NULL OR (activo = TRUE AND titulo LIKE …))`): la regla de S5 es del servidor y no
  del SPA, y pedir `q` junto a `activo=false` devuelve vacío a propósito. Lo encontró la prueba de S5,
  que nació roja.
- **D8 — Incidente, reparado.** El primer commit de T3 (`ca9603a`) quedó con
  `RutinasController.cs` **vacío** y arrastró el código y las pruebas de T4: se editó el árbol mientras
  el `git commit` estaba en vuelo, y el stash/restore de `lint-staged` escribió el archivo después de
  escenificarlo. La historia se reconstruyó con `git reset --mixed` a `97d83fa`, achicando el árbol a
  T3 y reaplicando T4 desde un snapshot: quedan `73035d6` (T3) y `8fa9b3d` (T4), ambos íntegros. Regla
  para lo que sigue: **no se toca ningún archivo mientras hay un commit corriendo**.
