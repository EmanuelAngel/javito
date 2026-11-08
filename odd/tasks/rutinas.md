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

**#78 · HU-RUT-05 — Editar y desactivar una rutina.** «Como Usuario quiero renombrar o dar de baja una
rutina para que la pantalla del día no se me llene de bloques viejos.» Se suma después del cierre de
la Puerta 3: este doc es el del módulo.

- **S9** — «Puedo editar nombre y descripción»
- **S10** — «Desactivarla la quita de los filtros sin borrarla ni borrar sus hábitos»
- **S11** — «Puedo reactivarla con sus hábitos y su orden intactos»
- **S12** — «Archivar un hábito lo quita de las rutinas sin romperlas»

**#77 · HU-RUT-04 — Filtrar mi día por rutina.** «Como Usuario quiero filtrar la pantalla principal
por rutina para ver solo lo que me toca ahora y no los 14 hábitos juntos.»

- **S13** — «Puedo elegir una rutina y la pantalla muestra solo sus hábitos, en su orden»
- **S14** — «Puedo volver a ver todos»
- **S15** — «El filtro no cambia lo que registro: marcar dentro del filtro marca el hábito real»

Fuera de alcance, por `docs/proyecto/plan-de-cierre.md:131`: `RUT-03` (ordenar), `RUT-04` (filtrar el
día) y `RUT-05` (editar y desactivar). Tampoco se agrega unicidad de nombre: `S3` pide lo contrario.

## Tasks

- **T1** · S1-S3 · inline · `d3ac1d9` · `feat(api): agregar alta y consulta de rutinas`
- **T2** · S4-S5 · inline · `97d83fa` · `feat(api): buscar hábitos por nombre para armar rutinas`
- **T3** · S2, S6-S7 · inline · `73035d6` · `feat(api): agregar hábitos a una rutina`
- **T4** · S8 · inline · `8fa9b3d` · `feat(api): quitar un hábito de una rutina sin archivarlo`
- **T5** · S4 · inline · `6d04e07` · `feat(web): pantalla de rutinas con buscador asíncrono de hábitos`
- **T6** · todas · inline · este commit · `docs(odd): cerrar la Puerta 3`

- **T7** · S6-S7 · inline · este commit · `fix(api): serializar el alta de hábitos en una rutina`
- **T8** · S9-S12 · inline · `feat/rutinas-editar-desactivar` · editar, desactivar y reactivar (#78)
- **T9** · S10, S13-S15 · inline · este commit · filtrar `/hoy` por rutina (#77)

## Cierre

| Regla | Dónde se prueba |
| :--- | :--- |
| S1-S3 | `backend/Javito.Api.Tests/RutinasEndpointTests.cs` |
| S4-S5 | `backend/Javito.Api.Tests/BusquedaDeHabitosEndpointTests.cs` |
| S6-S8 | `backend/Javito.Api.Tests/RutinasHabitosEndpointTests.cs` |
| S6-S7 bajo concurrencia | `backend/Javito.Api.Tests/RutinasConcurrenciaEndpointTests.cs` |
| Debounce, abort y respuesta vieja | `frontend/web/src/lib/busqueda.test.ts` |

- `RT-01` — las pruebas de agregar y quitar escriben y borran filas reales de `rutina_habito` en
  `javito_test`: la N:M queda con filas, no con la tabla vacía.
- `RT-06` — el buscador consulta al servidor con debounce de 250 ms y aborta el pedido anterior. El
  GIF del plan de cierre se graba con `pnpm dev:api` + `pnpm dev:web`: crear una rutina y escribir en
  el buscador. Falta sólo eso, que es evidencia manual.

### Verificación observada

| Comando | Resultado |
| :--- | :--- |
| `dotnet test` (suite completa) | 142/142 |
| `dotnet test --filter ~Rutinas` | 18/18 |
| `dotnet test --filter ~BusquedaDeHabitos` | 6/6 |
| `pnpm check` | exit 0 |
| `pnpm lint` | exit 0 |
| `pnpm test:web` | 53/53 en 12 archivos |
| `pnpm test:e2e` | 4/4, sin modificar ningún e2e |
| `svelte-check` | 0 errores, 0 advertencias (sólo las tres esperadas de SvelteKit) |
| `svelte_autofixer` | sin issues en los cinco `.svelte` |
| build del commit de T3 (`73035d6`) | 0 errores, en un worktree aparte |

Ninguna advertencia nueva. Las dos `CS8602` del build son preexistentes
(`EsquemaDeSeguridadEnOpenApi.cs:43` y `HabitosController.cs:172`).


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
- **D9 — Deadlock en altas simultáneas, encontrado por la verificación independiente.** El
  `INSERT … SELECT COALESCE(MAX(orden), 0) + 1 FROM rutina_habito` tomaba locks de rango sobre la
  tabla hija: dos altas a la misma rutina se mataban con `1213 Deadlock found` y una se perdía con 500
  (medido: 4/20 con dos POST a 0 ms; 8/24 en la corrida del verificador; a 40 ms de desfase, 0/20).
  Nunca corrompía datos: el `orden` no se repetía ni el UNIQUE se violaba. Ahora el repositorio abre
  una transacción, bloquea la fila de `rutina` con `FOR UPDATE` —la raíz del agregado— y recién
  entonces numera e inserta. Se descartó reintentar ante el 1213: tapar el síntoma deja el lock
  tomado igual y esconde el problema en cada alta. La misma reescritura eliminó un hallazgo menor: el
  orden se devuelve calculado, así que ya no hay un `Convert.ToInt32(null)` si otra transacción borra
  la fila entre el INSERT y la relectura.
- **D10 — Hallazgos que se dejan como están, a propósito.** `[StringLength(60)]` valida antes del
  `Trim()`, así que un nombre de 60 caracteres con un espacio final da 400 aunque recortado entre en
  la columna: es el mismo comportamiento que `CategoriaEntradaDto` y desde el SPA es inalcanzable
  (`maxlength=60` más recorte en `rutinas.svelte.ts`). `ExisteHabitoAsync` y `QuitarHabitoAsync` no
  filtran por `usuario_id`: su único llamador ya validó el dueño antes, y agregarles el filtro es
  defensa en profundidad para un llamador futuro, no un defecto hoy.
- **L4** — Pedido (#78), verbatim: «Ya mergeé 94, continúa». Orden acordado: #70 → #78.
- **L5** — Decisiones de T8, con el mismo criterio que `habitos-edicion.md` L5 y L9:
  `PUT /api/rutinas/{id}` (el cuerpo del alta) y `PUT /api/rutinas/{id}/activo` (el de categorías y
  hábitos, idempotente). «Los filtros» de S10 son los de `RUT-04` (filtrar el día), que todavía no
  existe: el listado sigue trayendo todas con su `activo` y la pantalla agrupa las inactivas aparte;
  `RUT-04` debe ofrecer solo las activas. Una rutina inactiva también se edita. S12: el detalle
  oculta los hábitos archivados sin borrar la fila de `rutina_habito`, así que al reactivar el hábito
  vuelve a su lugar en el orden.
- **L6** — Pedido (#77), verbatim: «Con rutinas activas te referís a poder filtrar 'hoy' por las
  rutinas activas, ¿no?» → «Sí». Decisiones del usuario: el filtro se resuelve **en el SPA** (el
  endpoint del día no cambia; el selector sale de `GET /api/rutinas` filtrado por `activo` y el orden
  de `GET /api/rutinas/{id}`), con el estado en `?rutinaId=` como el historial; un `rutinaId`
  inactivo, ajeno o inexistente **muestra todos**, sin aviso. S15 se cumple porque el filtro es sólo
  una vista sobre el mismo estado del día: marcar sigue yendo por el id real del hábito.
