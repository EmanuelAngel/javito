# La semana de un vistazo (REG-04)

Cierra `REG-04` y con eso el último renglón pendiente de la **Puerta 2**. Hoy la API del día manda sólo
conteos de la semana (`SemanaDto(cumplidos, objetivo)`); la historia pide los **siete días con su marca**.
Es la pantalla del día, no un reporte nuevo: la tarjeta del hábito gana sus siete días.

## Specs

**S1 — «Cada tarjeta muestra sus 7 días, de lunes a domingo»** (HU-REG-04, literal).

**S2 — «Un día cumplido se distingue de uno vacío sin depender solo del color»** (HU-REG-04, literal).
La forma y el texto también distinguen: un día cumplido lleva tilde y su `aria-label` lo dice.

**S3 — «Los días de un hábito semanal que todavía no llegó a su cuota se muestran neutros, nunca en rojo»**
(HU-REG-04, literal). Un día sin marca no usa `text-destructive` ni `bg-destructive`: un día futuro y un
día fallado se pintan igual, porque el día por sí solo no dice «fallaste».

**S4 — «Hoy se distingue de los días anteriores»** (HU-REG-04, literal). La marca de hoy no se confunde
con un día cumplido ni con un día futuro.

**S5 — El conteo sigue teniendo una sola fuente.** `SemanaDto` gana `dias[]` (`{ fecha, completado }`,
siempre 7, de lunes a domingo) y conserva `cumplidos` y `objetivo`: el badge «2 de 3 esta semana» de
`fila-del-dia.svelte` no cambia de texto. `cumplidos` se deriva de `dias`, no de una consulta aparte.

**S6 — La semana se cuenta de lunes a domingo.** Es la misma regla del seed y de `DiaRepositorio`; el
`lunes` se calcula en un solo lugar y el armado de los siete días no lo repite.

**S7 — La prueba no depende del día en que se corre.** La expectativa se calcula con `CURDATE()` y
`WEEKDAY()`, como la prueba del conteo: nada de 2 fijo, o el fixture de #34 vuelve a romper el gate al
cruzar la medianoche.

**S8 — El gate cierra igual.** `pnpm lint` 0 · `pnpm check` 0 · `pnpm test:web` verde (hoy 27/27) ·
`pnpm test:e2e` 2/2 **sin modificar los e2e**.

## Tasks

- **T1 — La semana con sus siete días** (`S5`–`S7`) · inline · rama `feat/semana-del-habito` ·
  commit `feat(api): exponer los siete días de la semana en curso del día` · **Hecho** en `766a0b0`
- **T2 — Los siete puntos en la tarjeta** (`S1`–`S4`, `S8`) · inline · commit
  `feat(web): mostrar los siete días de la semana en cada tarjeta` · **Hecho** en `448ebf7`

## Log

**L1** — «Ok, veamos de hacer reg-04 entonces. Estima costo y grado de dificultad.»

**L2** — «Encarguemonos de eso entonces. Commitea lo actual del doc de odd y continúa, aunque sí creo que
debería haber doc de feature»

**L3** — Cierre. **T1** (`766a0b0`): la subconsulta que contaba pasó a traer las fechas con
`GROUP_CONCAT` y el repositorio arma los siete días cruzando el calendario con esas fechas, así que
`cumplidos` se deriva de la lista y no de un `COUNT` paralelo. **T2** (`448ebf7`): `semana.ts` traduce los
siete días a inicial, estado y etiqueta, y `semana-del-habito.svelte` los pinta con tilde en el día
cumplido, anillo en el de hoy y neutro en el resto (nunca `destructive`).

Tres decisiones que costaron pensarlas:

- Los siete días van como `role="group"` con `role="img"` por día, **no** como lista anidada: la tarjeta ya
  es un `li` del listado y siete `listitem` más habrían entrado en el `getByRole('listitem')` del e2e.
- El punto de hoy se repinta con la misma marca que mueve el conteo. RED observado al quitar el repintado
  (2 pruebas en rojo); GREEN 32/32 al restaurarlo. Sin eso la tarjeta diría «3 de 7» con el lunes vacío.
- La prueba nueva es weekday-aware: la expectativa sale de `WEEKDAY()`, no de un 2 fijo, para no repetir
  el defecto del issue #34.

Gate: `pnpm lint` 0 · `pnpm check` 0 (74/74 de API y svelte-check 0/0) · `pnpm test:web` 32/32 ·
`pnpm test:e2e` 2/2 **sin modificar los e2e**. La Puerta 2 queda a un renglón: `PER-02`.

**L4** — Mergeado en `main` con squash (**#48**, `7f22776`). La rama remota se borró y la local también.
Con esto la Puerta 2 queda a un renglón: el avatar (`PER-02`), que trae la infra de archivos que después
usa la portada de Artículos (`RT-03`).
