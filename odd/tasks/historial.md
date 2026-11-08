# Historial — registros paginados y detalle de un hábito

Cierra #82 (`HU-HIS-01`) y #83 (`HU-HIS-02`). Sin cambios al esquema: `registro_habito` ya guarda
`valor_alcanzado`, `meta_objetivo`, `completado` y `notas`. El issue es la fuente; esto lo descompone.

## Specs

Transcripción literal de los issues (`docs/proyecto/historias-de-usuario.md`).

**#82 · HU-HIS-01 — Historial de registros.** «Como Usuario quiero consultar mis registros paginados y
filtrables para revisar un período concreto sin traer años de datos.»

- **S1** «El historial es paginado en el servidor»
- **S2** «Puedo filtrar por hábito y por rango de fechas»
- **S3** «Solo veo mis registros»
- **S4** «Cada fila muestra fecha, hábito, valor alcanzado, meta de ese día y si se completó»
- **S5** «Las notas aparecen cuando el día tiene una» — **abierto**: lo cierra #74, que es quien las
  escribe (L2).

**#83 · HU-HIS-02 — Detalle histórico de un hábito.** «Como Usuario quiero abrir un hábito y ver su
histórico completo para juzgar mi consistencia real, no la de esta semana.»

- **S6** «El detalle muestra una cuadrícula de 30, 60 o 365 días, a elección»
- **S7** «Muestra el total de veces cumplido y el porcentaje del período»
- **S8** «La cuadrícula se lee igual que la semanal: cumplido, vacío y neutro son distinguibles sin
  depender del color»
- **S9** «Desde el detalle puedo editar el hábito»

## Tasks

| ID | Specs | Ruta | Commit |
| :--- | :--- | :--- | :--- |
| T1 | S1-S4 | API: `GET /api/historial` paginado y filtrable (`HistorialController`, `HistorialRepositorio`, pruebas en `HistorialEndpointTests`) | `8bcea4a` |
| T2 | S1-S4 | SPA: `/historial` con tabla, filtros y paginador (`historial.svelte.ts`, `api/historial.ts`, `historial.test.ts`, `historial.e2e.ts`) | `67dee63` |
| T3 | S6-S8 | API: `GET /api/habitos/{id}/historial?dias=30\|60\|365` con totales y la cuadrícula | `feat/historial-detalle` |
| T4 | S6-S9 | SPA: `/historial/[id]` con la cuadrícula, los totales y el atajo a editar | `feat/historial-detalle` |

## Log

**L1** (pedido) — «Javito, línea B (historial): implementá #82 y después #83, un PR por issue. Tu spec
completo está en `C:\Users\Usuario\AppData\Local\Temp\spec-historial.md`: leelo entero antes de
empezar y seguilo. Ahí están los criterios literales, las restricciones del repo, RED antes del fix, los
checks de a uno y la evidencia que tenés que reportar. Contexto: `mem_context` y `mem_search "javito
backlog regularizar"`. Una decisión ya tomada por el usuario, anotala como L# en `odd/tasks/historial.md`:
- #82 NO toca las notas. El criterio «las notas aparecen cuando el día tiene una» queda abierto hasta
  #74, que es quien las escribe. No agregues el campo al contrato ni lo pruebes con un INSERT directo.
Reglas: trabajá en este directorio, que ahora está en la rama `feat/articulos-desactivar` de otro agente:
antes de ramificar, `git checkout main` y `git pull`. No copies el repo ni `node_modules`: Windows
Defender se come la CPU. Checks pesados de a uno, sin ningún `Javito.Api` corriendo. Si aparece otra
ambigüedad de producto, preguntame antes de decidir.»

**L2** (decisión, S5) — #82 **no toca las notas**: no viaja el campo en el contrato, no se prueba, y la
regla no se resuelve con un `INSERT` directo en `registro_habito`. El criterio queda abierto hasta #74
(anotar una nota del día), que es el endpoint que las escribe. Razón: un medio contrato inventado
obligaría a adivinar cuándo aparece la columna y a probarla por una puerta que ningún usuario usa.

**L3** (evidencia #82) — **RED** antes del fix: `HistorialEndpointTests` 9/9 en rojo (404 de una ruta que no
existía y el 400 ausente). **GREEN**: 9/9, y `pnpm check:api` 170/170. Los nueve casos, uno por regla:
paginado con metadata y sin solapamiento entre páginas (S1); filtro por hábito y por rango inclusivo, con
los dos extremos sueltos (S2); sólo lo propio, con el contraejemplo de las otras cuentas del seed (S3);
las cinco columnas de la fila, con la meta del día que sobrevive a editar el hábito (S4); el hábito
archivado que sigue apareciendo (L9 de `habitos-edicion.md`); y los tres 400: rango invertido
(`rango_de_fechas_invalido`), fecha mal escrita (`fecha_invalida`) y página fuera de rango (el 400 de
validación de `ParametrosDePagina`, para probar que el paginado es el compartido y no uno nuevo).

En el SPA la prueba de S4 es `historial.test.ts` (la fila que se pinta: fecha larga, valor y meta con
formato de acá, «Cumplido»/«Sin cumplir» con palabra además del ícono) y `historial.e2e.ts` recorre el
recorrido completo contra el seed: seis filas por página, `?pagina=2`, filtro por «Leer» y el desplegable
sin los hábitos de las otras cuentas. Gate: `pnpm lint` limpio · `pnpm check:api` 170/170 ·
`pnpm check:web` 0 errores · `pnpm test:web` 70/70 · `pnpm test:e2e` 5/5.

Dos cosas que costaron pensarlas:

- Las fechas del filtro llegan como texto, no como `DateOnly`: con el binding automático el 400 no lleva
  `codigo` y el SPA no podría decidir por él. Se leen con `FechaIso`, y el rango invertido es
  `ErrorDeNegocio` como cualquier otra regla.
- El `JOIN` con `habito` no es sólo para el título: `registro_habito` no guarda el dueño, así que el
  `WHERE h.usuario_id` es lo que hace cumplir la propiedad. Por eso un `habitoId` ajeno devuelve lista
  vacía y no un 404: el filtro no encuentra nada, que es exactamente lo que se quiere decir.

**L4** (decisión, S7) — «Semanales: semanas con cuota cumplida». El porcentaje de un hábito **diario** es
días cumplidos / días evaluables; el de un **semanal** es semanas que alcanzaron su cuota / semanas
evaluables. `total de veces cumplido` sigue la misma unidad que el porcentaje: días en un diario, semanas
en un semanal.

**L5** (decisión, S6/S7) — Los días **anteriores al primer registro** del hábito se pintan **neutros** y
quedan **fuera del porcentaje**: el período se evalúa desde `max(desde, primer registro)`. El esquema no
guarda la fecha de alta, así que la primera marca es la única evidencia de que el hábito existía. Sin
ningún registro no hay ventana evaluable: todo neutro, `evaluables = 0`, `porcentaje = 0`.

**L6** (decisión, S8) — Un día es **cumplido** si su registro está `completado`; es **vacío** si es un día
evaluable de un hábito **diario** sin registro completado; y es **neutro** si es un día futuro, si es
anterior al primer registro, o si es un día sin marca de un hábito **semanal** (su cuota no es diaria, la
misma razón por la que la tarjeta del día no lo pinta en rojo). Hoy es evaluable: neutro no es «el día no
terminó».

**L7** (decisión, S7) — Para un semanal, **todas las semanas tocadas** por el período entran al
denominador, con la semana que empieza el lunes. Una semana cuenta como cumplida si sus días **dentro del
período evaluado** alcanzan `frecuencia_veces`. Una semana parcial del borde no se descarta: se evalúa con
los días que le tocan.

**L8** (contrato de #83) — `GET /api/habitos/{id}/historial?dias=30|60|365&hasta=aaaa-mm-dd`, con la acción
en `HistorialController` y ruta absoluta `~/api/habitos/{id:int}/historial` para no partir el módulo. La
ventana es `[hasta - (dias - 1), hasta]`, con `hasta` = hoy en UTC si no viene (la manda el SPA con el día
del navegador, como en `/api/dias`). Respuesta:

```json
{ "habitoId": 15, "habito": "Leer", "tipo": "contador",
  "frecuencia": { "tipo": "diaria", "veces": null },
  "dias": 30, "desde": "2001-06-01", "hasta": "2001-06-30",
  "cumplidos": 12, "evaluables": 30, "porcentaje": 40.0,
  "cuadricula": [ { "fecha": "2001-06-01", "estado": "cumplido" } ] }
```

`estado` es `cumplido` | `vacio` | `neutro` (L6); `porcentaje` va con un decimal. Errores: hábito ajeno o
inexistente → el 404 `no_encontrado` que ya usa `HabitosController`; `dias` fuera de la lista → 400
`dias_invalidos`; `hasta` mal escrita → 400 `fecha_invalida`. El 404 no distingue «no existe» de «no es
tuyo», igual que el resto de la API.
