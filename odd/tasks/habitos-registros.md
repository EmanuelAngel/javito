# Hábitos y marcar el día (día 4 del plan de cierre)

Cierra `HAB-01`, `HAB-02` y `REG-01`. Es el 60% de la Puerta 2 (que además pide `REG-04` y `PER-02`).
Tres slices verticales, cada uno con su rama y su PR: el día se ve, el día se marca, mis hábitos.

**Estado: entregado y mergeado** — issues #19, #20 y #21; PRs #22 (`3790fb7`), #23 (`b090e06`) y
#24 (`fe96774`).

## Specs

**S1 — El día se lee como recurso propio.** `GET /api/dias/{fecha}` devuelve el día del usuario
autenticado: sólo sus hábitos **activos**, y por cada uno `id`, `titulo`, `categoria` (nombre o `null`),
`tipo` (`si_no` | `contador` | `abstinencia`), `meta`, `unidad`, `frecuencia: { tipo, veces }`,
`semana: { cumplidos, objetivo }` y `registro: { valor, completado }` o `null` si no hay marca.
`objetivo` es 7 en un hábito diario y `veces` en uno semanal; `cumplidos` cuenta los días de **la semana
en curso, de lunes a domingo**, con registro completado. Los hábitos de otra cuenta no aparecen, y uno
archivado tampoco.

**S2 — Marcar es un `PUT` idempotente sobre el par (hábito, fecha).**
`PUT /api/habitos/{id}/registros/{fecha}` con `{ valor }` crea o reemplaza la fila: `meta_objetivo` sale
del hábito y nunca del cuerpo, y `completado` se deriva (`valor >= meta`). Repetir el mismo `PUT` deja
el mismo estado. Responde `200` con `{ valor, metaObjetivo, completado }`.

**S3 — Desmarcar es `DELETE` del mismo recurso.** `DELETE /api/habitos/{id}/registros/{fecha}` borra la
fila (no la deja en cero) y responde `204`; repetirlo también responde `204`. Después del `DELETE` el
día informa `registro: null` y el conteo semanal bajó.

**S4 — El contador baja de la meta.** Un `contador` guarda el valor que muestra la interfaz, cero
incluido (`0` es «medí 0», distinto de «no registré»). Bajar de la meta devuelve `completado` a `false`.

**S5 — Pertenencia, estado y rango de fecha.** Un hábito de otra cuenta responde `404` (no existe para
mí); uno archivado responde `400` con `codigo: "habito_archivado"`; una `fecha` fuera de
`2000-01-01` … hoy+1 (UTC) responde `400` con `codigo: "fecha_fuera_de_rango"`.

**S6 — Alta de hábito.** `POST /api/habitos` responde `201` con el hábito creado. Título obligatorio
(recortado, no sólo espacios, ≤100) y descripción opcional (≤255). Categoría obligatoria y **activa**:
sin ella `400 categoria_requerida`, inexistente `400 categoria_inexistente`, archivada
`400 categoria_inactiva`. `tipo` y `frecuencia.tipo` salen de los enums de la base. Si el tipo es
`contador`: `meta` > 0 (`400 meta_requerida`) y `unidad` no vacía, ≤30 (`400 unidad_requerida`); si no,
ambas van `null`. Si la frecuencia es `semanal`: `veces` obligatorio y de 1 a 6
(`400 frecuencia_fuera_de_rango`); si es `diaria`, `veces` va `null`. Nace `activo: true`.

**S7 — Listado de hábitos propios.** `GET /api/habitos?activo=` devuelve sólo los del usuario
autenticado, cada uno con categoría y frecuencia, y respeta el filtro: `true` los activos, `false` los
archivados, sin filtro los dos. Los activos salen por título y los archivados por título.

**S8 — `/hoy` marca en dos segundos y no miente.** La pantalla lista todos los hábitos activos con su
progreso semanal `N×M` y nunca bloquea un toque por haber pasado la cuota. `si_no` y `abstinencia` se
marcan y desmarcan con un toque (en abstinencia la acción dice «Hoy me mantuve»); el `contador` sube y
baja con `+` y `−`, sin escribir números, y en cero el `−` no hace nada. El cambio se pinta al instante
y viaja en segundo plano: si el pedido falla, la fila vuelve al estado del servidor y aparece el aviso
«No se pudo guardar. Probá de nuevo.».

**S9 — `/habitos` lista, separa y crea.** La pantalla muestra los activos y, aparte, los archivados sin
que ensucien la vista principal; cada fila muestra categoría y frecuencia. El formulario de alta pide
categoría en un desplegable con las activas (`GET /api/categorias?activo=true`) y muestra meta y unidad
sólo cuando el tipo es `contador`. Cada `codigo` de error se traduce a su campo o al aviso general.

**S10 — El e2e crea su propio dato y no toca el seed.** Un e2e nuevo entra con `usuario@javito.local`,
da de alta el hábito «Prueba e2e» (contador, meta 3) desde `/habitos`, lo marca y desmarca en `/hoy`,
lo sube con `+` y lo baja con `−`, recarga y verifica que el estado persistió. No escribe ningún
registro de los hábitos del seed, que cuentan la historia semanal de la cátedra (2 de 3 en «Ejercicio
pesado»).

## Tasks

- **T1 — El día se ve** (`S1`) · inline · rama `feat/dia-lectura` · commit `feat(api): agregar la lectura del día con sus registros` (`1e20b1d`)
- **T2 — El día se marca** (`S2`–`S5`, `S8`) · inline · rama `feat/dia-marcar` · commit `feat(dia): agregar el marcado del día con guardado optimista` (`5533eae`)
- **T3 — Mis hábitos** (`S6`, `S7`, `S9`, `S10`) · API inline + SPA delegado a `gentle-ai-worker` · rama `feat/habitos-lista` · commit `feat(habitos): agregar el alta y el listado de hábitos` (`7b62129`)

Cada slice sale de `main` después del merge del anterior, así el reviewer siempre lee contra una base
conocida. Los PRs se abren cuando el autor lo pida: nada se pushea solo.

## Log

**L1** — «¿en qué quedamos? ¿qué tenemos que hacer hoy?»

**L2** — «Hagamos el grilling con el sistema tuyo de question»

**L3** — «Me estoy mareando». Después: «Es que es la primera vez que veo que se 'inventa' un recurso
(dias) que no tiene tabla ni nada, por eso no me cierra. Sí me cierra las url por recursos (las 3 que
sugeriste) pero ya estoy confundido respecto a qué es lo correcto de REST, ¿crear un recurso 'ficticio'
como 'dias' es permitido en rest? Como ves estoy algo perdido». Se explicó con precedentes del propio
repo (`/api/health`, `/api/sesion/yo`, `/api/categorias?activo=`) y con la regla: proyecciones de sólo
lectura cuando el cliente repetiría una regla, escrituras por el recurso con tabla detrás. El autor
aceptó la proyección `/api/dias`.

**L4** — Decisiones de la ronda 1: listado con `?activo=` y sin acción de archivar (`HAB-04` fuera de
alcance); `/hoy` muestra todos los activos con progreso semanal `N×M`, sin ocultar ni colapsar.

**L5** — Decisiones de la ronda 3: `DELETE` = sin marcar y `0` en un contador es un dato; interfaz
optimista con rollback y aviso; entran sólo `/hoy` y `/habitos` (el detalle con cuadrícula, corregir el
pasado y archivar no entran); integración por regla más un e2e de `/hoy`.

**L6** — Decisiones de la ronda 4: categoría obligatoria en el alta; pasarse de la cuota semanal está
permitido (`4/3`), porque el seed ya lo contempla («cumple, se pasa de la cuota», `database.sql:129`);
el e2e crea su propio hábito descartable.

**L7** — «Dividí en la cantidad de PRs que creas conveniente, pero manteniendo slices verticales. Decime
qué opinas de esto y mostrame cómo queda. Digo porque hacer review de un CRUD entero a veces me abruma
y no solo porque sean más de 400 líneas netas.» Se cerró en tres slices verticales (ver Tasks).

**L8** — Decisiones que tomó el agente y que el autor no eligió una por una, anotadas para que se puedan
revisar: unidad de medida como texto libre ≤30; meta obligatoria sólo en `contador`; códigos de error
`categoria_requerida`, `categoria_inexistente`, `categoria_inactiva`, `meta_requerida`,
`unidad_requerida`, `frecuencia_fuera_de_rango`, `habito_archivado`, `fecha_fuera_de_rango`; `PUT` de
registro responde `200` y `DELETE` `204`; `/hoy` ordena por id y `/habitos` por título; la acción de
abstinencia dice «Hoy me mantuve».

**L9 — Dónde se escribe cada cosa.** `GET /api/habitos` expone `categoriaId` y no el nombre de la
categoría: el `/habitos` ya pide el catálogo completo para el desplegable —y para mostrar el nombre
de una categoría que después se archivó—, así que el nombre se resuelve en el cliente. `/api/dias` sí
trae el nombre: es una proyección y no tiene sentido pedirle al cliente que una dos payloads.
Desmarcar un hábito archivado responde el mismo 400 `habito_archivado` que marcarlo.

**L10 — Evidencia de T1.** `1e20b1d`: 8 pruebas nuevas, 53/53 en la suite, svelte-check 0/0, lint
limpio, vitest 4 nuevas de `fechas.ts`. Dos rojos iniciales fueron defecto de la prueba, no de la API:
interpolar un `DateOnly` en la ruta usa la cultura local («1/11/2026») y no matchea ninguna acción.

**L11 — Evidencia de T2.** `5533eae`: 10 pruebas nuevas, 63/63 en la suite, vitest 18/18 (5 del estado
del día, 3 del 204 de `pedir`), svelte-check 0/0. El 204 del `DELETE` obligó a tocar `pedir.ts`: antes
`respuesta.json()` explotaba con un `SyntaxError` y un desmarcado exitoso se veía como error.

**L12 — Evidencia de T3.** 10 pruebas nuevas de API, 73/73 en la suite; el SPA entra con `/habitos`
modularizado (`lib/habitos.svelte.ts`, `HabitoForm.svelte`, `HabitosLista.svelte`, el helper
`lib/api/errores.ts` compartido y la página como composición de 31 líneas), más el e2e
`frontend/web/e2e/habitos.e2e.ts`, que da de alta «Prueba e2e» por la UI, lo marca y desmarca en
`/hoy`, sube y baja de la meta y recarga. Gate de cierre: 73/73 de API, 22/22 de vitest, 2/2 de e2e,
svelte-check 0/0 y lint limpio en las dos capas.

**L13 — El e2e no toca el seed.** «Prueba e2e» es un contador propio (meta 3, vasos) y el recorrido lo
devuelve a cero para poder repetirse; el histórico del usuario de prueba sigue contando la semana de la
cátedra. La fila queda en la base de desarrollo y `pnpm db:reset` la borra, igual que la categoría
«Pruebas e2e» del e2e anterior.

**L14 — Cierre.** Las tres tareas quedan en tres commits encadenados, mergeados como squash en `main`:
#22 `3790fb7` (`feat/dia-lectura`), #23 `b090e06` (`feat/dia-marcar`) y #24 `fe96774`
(`feat/habitos-lista`). Cada PR cerró su issue con `Closes #N`. El presupuesto se declaró en el body de
cada PR: 484 ponderadas la primera, y 721 y 947 las dos siguientes, sobre el techo de 600, a propósito
para conservar el corte vertical. Con esto `HAB-01`, `HAB-02` y `REG-01` están cerrados y la Puerta 2
del 02/11 queda a un paso: `REG-04` (la semana) y `PER-02` (el avatar).

**L15 — Deuda técnica anotada, no ejecutada.** `Modelos/HabitoDelDia.cs` es un **read model** (sale de
`habito` + `categoria` + `registro_habito` y de una agregación semanal), no un DTO ni una entidad, pero
vive en `Modelos/`, que en este repo promete tablas 1:1, y duplica 7 campos de `Modelos/Habito.cs`.
Origen: T1 construyó la proyección antes de que existiera la entidad, porque en ese slice un hábito no
se podía crear ni listar. Arreglo decidido para un commit aparte sobre `main`: mover las proyecciones a
`Consultas/` y componer `Habito` adentro de `HabitoDelDia`, así la proyección queda como «el hábito +
lo del día» y no como una segunda copia. No entró en la cadena porque compuesta depende de
`Modelos/Habito.cs`, que nace en #23, y habría hecho que #22 dejara de compilar sola.
