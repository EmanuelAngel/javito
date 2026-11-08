# Artículos (`ART-01`, `ART-04`)

Módulo 7 del [orden de implementación](../../docs/proyecto/orden-de-implementacion.md): paga la infra
de archivos estrenada por el avatar (`PER-02`) y estrena el paginado de servidor que después reusan
`HIS-01` y `USU-01`. `ART-01` primero, `ART-04` sobre el mismo repositorio.

## Specs

**S1 · Alta de artículo (`HU-ART-01`).** `POST /api/articulos`, sólo `Administrador` (un `Usuario`
recibe 403 del servidor, no un botón escondido). Campos obligatorios con los límites de
`database.sql`: `titulo` ≤ 150, `resumen` ≤ 300, `contenido_markdown` no vacío. `categoria_id` es
`NOT NULL` y se elige de las **categorías activas**: una categoría inexistente o inactiva se rechaza
con 400 `categoria_invalida`. La portada es **opcional** — "Un artículo sin portada se puede publicar
igual" — y cuando llega se valida como imagen por firma y por tamaño con la infra de `PER-02`
(códigos `archivo_*`, tope 5 MB). El artículo "nace activo y con fecha de publicación": `activo = TRUE`
y `fecha_publicacion` la pone la base.

**S2 · Sin imágenes huérfanas.** La orquestación es la del avatar: guardar la portada, insertar la
fila, y si la fila no entra, borrar la imagen recién guardada. Un alta fallida no deja archivos en
disco.

**S3 · Lectura de un artículo.** `GET /api/articulos/{id}` con cualquier usuario autenticado (no hay
catálogo público). Devuelve 404 `no_encontrado` cuando el id no existe **o el artículo está
inactivo**: así lo retirado no se abre por URL (`HU-ART-03`).

**S4 · Contrato de paginado, en español.** "que el paginado también esté en español": la metadata
viaja como `pagina`, `tamanioDePagina`, `totalDeRegistros`, `totalDePaginas`, y la página como
`elementos`. La URL repite el vocabulario: `?pagina=2&tamanioDePagina=6&categoriaId=…`.
`tamanioDePagina` por defecto **6**, acepta 1..50 y fuera de rango es 400 con `errors`. Una
`pagina` mayor que `totalDePaginas` devuelve `elementos` vacío y la metadata tal como se pidió: el
servidor no corrige en silencio lo que el cliente pidió.
> Mapeo con la consigna: `consigna-y-definiciones.md:39` pide la metadata "(`pageNumber`, `pageSize`,
> `totalRecords`, `totalPages`)". El requisito es *tener* esa metadata; los nombres siguen la
> convención del repo (contrato JSON en español, como `estado` y `servicio`). Equivalencia:
> `pagina` ↔ `pageNumber`, `tamanioDePagina` ↔ `pageSize`, `totalDeRegistros` ↔ `totalRecords`,
> `totalDePaginas` ↔ `totalPages`.

**S5 · Catálogo (`HU-ART-04`).** `GET /api/articulos` con cualquier usuario autenticado. Sólo
`activo = TRUE`, orden `fecha_publicacion DESC, id DESC` ("del más nuevo al más viejo"), filtro
opcional por categoría. Cada elemento trae portada, título, resumen, categoría y fecha. El índice
`ix_articulo_catalogo (activo, fecha_publicacion)` ya existe para esta consulta.

**S6 · Markdown seguro.** Renderizado con `marked` y sanitizado con `DOMPurify` antes de llegar al
DOM: es la única defensa contra un XSS almacenado que se dispara en el navegador de cada lector. La
vista previa del alta usa **el mismo** renderizador que va a usar el detalle.

**S7 · Alta en dos columnas.** `/articulos/nuevo` (sólo Administrador en el servidor; la nav esconde
el enlace) escribe a la izquierda y renderiza la vista previa a la derecha en vivo, apiladas en
pantalla angosta. La categoría se elige en desplegable con las activas — "Las categorías se eligen
siempre en desplegable y nunca se buscan ni se paganinan" (`ux-brief.md`).

**Línea A del backlog (después de la entrega).** Este doc sigue siendo el del módulo: #63 → #81 →
#79 → #80, un issue por PR. Cada issue es la fuente; acá van sus criterios.

**#63 · Modularizar las pantallas de artículos.** Criterios del issue, literales:

- **S8** «El estado del catálogo —lectura y escritura de `?pagina=` y `?categoriaId=`, el pedido a la
  API y la guardia que descarta respuestas viejas— vive fuera de la pantalla, en un módulo propio y
  **sin nada de artículos adentro**, para que `HIS-01` y `USU-01` lo usen con sus propios filtros»
- **S9** «Ese módulo tiene prueba unitaria sin montar el componente»
- **S10** «El formulario del alta es un componente con sus campos, su archivo y sus errores por campo,
  con los valores iniciales y el `onsubmit` por prop, de modo que `ART-02` lo precargue en vez de
  copiarlo»
- **S11** «Los tres estados vacíos del catálogo (no hay artículos / no hay en la categoría / la página
  no existe) no son ramas `{#if}` de treinta líneas dentro del markup de la pantalla»
- **S12** «Ninguna de las dos pantallas supera las **150** líneas contando `<script>` y markup»
- **S13** «Agregar un campo al formulario del alta toca **un solo archivo**»
- **S14** «El comportamiento observable no cambia: las pruebas existentes … siguen pasando **sin
  reescribir sus aserciones**»

**#81 · HU-ART-05 — Leer un artículo completo.** «Como Usuario quiero leer el artículo completo con su
formato para aprovechar el contenido y no un muro de texto plano.»

- **S15** «El Markdown se renderiza con títulos, listas y negritas»
- **S16** «La portada se ve en la cabecera del artículo»
- **S17** «Se ve la categoría y la fecha»
- **S18** «Volver al catálogo conserva la página en la que estaba»

**#79 · HU-ART-02 — Editar un artículo.** «Como Administrador quiero corregir un artículo publicado
para arreglar un error sin despublicarlo.»

- **S19** «Puedo editar título, resumen, contenido, categoría y portada». `PUT /api/articulos/{id}`
  (multipart, los mismos campos y límites que el alta) y la pantalla `/articulos/[id]/editar`
  (`ux-brief.md`), que reusa `articulo-form.svelte` precargado con `inicial`.
- **S20** «Reemplazar la portada no acumula imágenes huérfanas». La portada se conserva (sin archivo
  nuevo) o se reemplaza; no se puede quitar. Orquestación del avatar: guardar la nueva, actualizar la
  fila, borrar la anterior; si la fila no se actualiza, se borra la nueva y queda la anterior.
- **S21** «La fecha de publicación original no cambia al editar».
- **S22** La categoría actual se conserva aunque esté inactiva; cambiar a otra exige una activa
  (400 `categoria_invalida`), mismo criterio que `habitos-edicion.md` L4.
- **S23** Sólo Administrador edita, igual que el alta: 401 sin sesión, 403 `sin_permiso` para un
  Usuario, sin tocar la fila. Un artículo inexistente o inactivo es 404 `no_encontrado`.

**Fuera de alcance de esta feature.** Edición del artículo y reemplazo de portada (`ART-02`), baja
lógica y reactivación (`ART-03`), pantalla de detalle y "volver conservando la página" (`ART-05`),
búsqueda de artículos.

## Tasks

| ID | Qué | Specs | Ruta | Commit |
| :--- | :--- | :--- | :--- | :--- |
| T1 | Backend del alta: modelo, repositorio, contratos, `POST /api/articulos` y `GET /api/articulos/{id}` con portada opcional | S1, S2, S3 | inline + worker (verificación) | `0de7feb` |
| T2 | Frontend del alta: `api/articulos.ts`, `markdown.svelte` (marked + DOMPurify), `/articulos/nuevo` con vista previa en dos columnas | S6, S7 | inline + worker | `45d165a` (render) + `50e64c7` (alta) |
| T3 | Backend del catálogo: helper de paginado reusable + `GET /api/articulos` paginado y filtrable | S4, S5 | worker | `0de7feb` |
| T4 | Frontend del catálogo: `/articulos` con tarjetas, filtro por categoría y paginado | S5 | worker | `50e64c7` |
| T5 | Recorrido e2e del alta y del catálogo contra la WebAPI real | S1, S5 | worker | `50e64c7` |
| T6 | #63: `paginado.svelte.ts` (+ prueba), `catalogo-de-articulos.svelte.ts`, `articulo-form`, `catalogo-vacio`, `grilla-de-articulos` | S8-S14 | inline | `chore/articulos-modularizar` |
| T7 | #81: `/articulos/[id]`, la tarjeta pasa a ser enlace, «Volver» con la query del catálogo | S15-S18 | inline | `feat/articulos-detalle` |
| T8 | #79: `PUT /api/articulos/{id}` con reemplazo de portada, `/articulos/[id]/editar` con `articulo-form`, «Editar» en el detalle, e2e | S19-S23 | inline | `feat/articulos-editar` |

## Log

**L1 — pedido original del usuario (verbatim):**

> Veamos cómo encarar ART-01 Y ART-04

**L2 — decisiones del usuario (verbatim):**

> Algo extra que noté: el backend en su mayoría lo tenemos en español, que el paginado también esté
> en español, pageSize - tamanioDePagina. Por el resto que ya preguntaste todo bien.

Respuestas a las cuatro decisiones dejadas abiertas: **marked + DOMPurify** para el Markdown,
**6** artículos por página por defecto en el helper de paginado, vista previa en **dos columnas en
vivo**, y orden **ART-01 y después ART-04**.

**L3 — decisiones de implementación tomadas por el orquestador** (no pedidas por el usuario, se
documentan para que sean revisables): página fuera de rango devuelve vacío en vez de corregir;
`tamanioDePagina` acotado a 1..50; `contenido_markdown` con tope de 50 000 caracteres en el DTO
(`MEDIUMTEXT` acepta más, pero un cuerpo de 16 MB no es un artículo); categoría inexistente o
inactiva en el alta es 400 `categoria_invalida`; el `GET /{id}` ya nace devolviendo 404 para
inactivos porque el detalle y la edición lo van a reusar tal cual.

**L4 — evidencia y bloqueo.** T1 escrito (`Modelos/Articulo.cs`, `Repositorios/ArticuloRepositorio.cs`,
`Contratos/ArticuloDto.cs`, `Contratos/MapeoDeArticulos.cs`, `Controllers/ArticulosController.cs`, DI en
`Program.cs`) con su prueba en `backend/Javito.Api.Tests/ArticulosEndpointTests.cs` (18 casos).
`dotnet build` sin errores nuevos y `dotnet format --verify-no-changes` limpio. T2 escrito
(`api/articulos.ts`, `components/markdown.svelte` y su test, `routes/(app)/articulos/nuevo/`), con
`marked@18` y `dompurify@3` agregados al SPA; `vitest` del render en verde (4/4) y `svelte-check` en
0 errores. **El servicio `MySQL80` está parado** y `Start-Service` falla por falta de elevación, así
que `dotnet test` muere en la conexión: el RED/GREEN de T1 y el e2e del alta quedan pendientes de que
la base esté arriba. El primer intento de RED falló además por un `using System.Net.Http.Json`
faltante en la prueba, ya corregido.

**L5 — verificación y cierre.** El ciclo quedó completo y con evidencia en cada paso:

| Qué | Resultado |
| :--- | :--- |
| RED de T1 | 18/18 en rojo contra el commit base, todas con `NotFound`: la regla la sostiene el código nuevo, no el azar |
| Primer GREEN de T1 | 16/18. Dos causas reales: `RegularExpressionAttribute` exige match de la cadena entera y `.` no cruza saltos de línea (todo Markdown multilínea se rechazaba como vacío, corregido con `(?s)`); y tres pruebas afirmaban «carpeta vacía» sobre un temporal compartido |
| RED de T3 | 12/12 en rojo con `405 Method Not Allowed` |
| Suite del backend | **114/114**, `dotnet format --verify-no-changes` limpio |
| Frontend | `vitest` **48/48**, `svelte-check` 0 errores, prettier y eslint limpios |
| e2e | **4/4** en `pnpm test:e2e`, con el spec nuevo de artículos (login, catálogo, alta con portada, vista previa antes de enviar, filtro, `?pagina=99` y el 403 de un Usuario sin dejar fila) |

Dos correcciones que valen por sí mismas: el catálogo pasó a devolver `ArticuloDelCatalogoDto` **sin
`contenidoMarkdown`** —mandar seis cuerpos completos en una lista de tarjetas es exactamente lo que
`HU-ART-04` pide evitar— y la tarjeta dejó de ser un enlace a `/articulos/{id}`: esa ruta es `ART-05`
y un enlace a un 404 es peor que una tarjeta quieta. `ART-05` la vuelve a convertir en enlace con
`resolve()`.

**Pendiente al cerrar:** la verificación independiente por la dependencia nueva (riesgo ítem 5) y las
historias `ART-02` (edición y reemplazo de portada), `ART-03` (baja lógica) y `ART-05` (detalle), que
son las que terminan de contar el módulo. El e2e deja en la base de desarrollo un artículo `Pruebas
e2e · artículo <timestamp>` por corrida y su PNG en `backend/Javito.Api/wwwroot/uploads/articulos/`:
`pnpm db:reset` borra las filas y el PNG se limpia con la carpeta.

**L6 — la verificación independiente encontró dos cosas y se corrigieron.**

1. El perfil por defecto de DOMPurify **deja pasar `<style>`**: un artículo publicado podía tapar la
   interfaz entera con `body{display:none}`. No es ejecución de código, es defacing, y alcanza a
   todas las cuentas. El renderizador pasó a sanitizar con `USE_PROFILES: { html: true }` y
   `FORBID_TAGS: ['style']`, y la prueba cubre ahora SVG con `onload`, `iframe` y `style` además de
   los vectores que ya cubría. La prueba se escribió primero y falló: `expected <style> to be null`.
2. El catálogo **traía `contenido_markdown` desde MySQL** aunque el DTO no lo mande: la afirmación de
   L5 valía para el payload y no para la consulta. El listado usa ahora su propia lista de columnas
   (`ColumnasDelCatalogo`, sin el `MEDIUMTEXT`) y su propio lector, y el detalle sigue leyendo el
   cuerpo con `BuscarAsync`.

Se sumaron además los bordes que faltaban del contrato: `tamanioDePagina` 1 y 50 se aceptan, y una
`pagina` que no es un entero (`abc`, `2147483648`) da 400 nombrando el campo. Backend **118/118**,
frontend **49/49**, `svelte-check` y `dotnet format` limpios.

**L7 — entrega.** Issues **#58** (`ART-01`, publicar) y **#57** (`ART-04`, catálogo). PRs encadenados
**#59** (`feat/api-articulos` → `main`) y **#60** (`feat/web-articulos` → `feat/api-articulos`, que
GitHub reapunta solo al mergear el primero). Las ramas se rearmaron por capa desde los mismos commits
—`540146a` se aplicó sobre `0de7feb` para que la rama de la API no arrastre código del SPA— y el
contenido de `feat/web-articulos` quedó idéntico al árbol original (`git diff` vacío antes de borrar la
rama de trabajo). Peso ponderado: 738 en la API, 1260 en el SPA; los dos por encima del techo de 600
del presupuesto, con autorización explícita del usuario para revisar PRs de ese tamaño.

**L-A1** — Pedido de la línea A, verbatim: «listá qué cosas puedo ir haciendo, implementaciones digo.
… Cosas que se puedan ir realizando en paralelo»; elegida «A · Artículos». Ante una ambigüedad:
«Preguntame, no estoy en casa pero si puedo responderte preguntas».

**L-A2** — Decisiones de T6 (#63). El paginado genérico es `lib/paginado.svelte.ts`: funciones puras
para leer y armar la URL (`leerPagina`, `leerEnteroPositivo`, `armarConsulta`) y `crearPaginado`
con la guardia por turno. La pantalla sigue siendo dueña de la URL y del `$effect`. Lo que sabe de
artículos (categorías, `navegar`) va en `catalogo-de-articulos.svelte.ts`. `Pagina<T>` pasa a
`api/pagina.ts`, reexportado desde `api/articulos.ts`. El formulario se vacía después de publicar
volviéndolo a montar con `{#key}`. Es un refactor: no hay RED; la red de seguridad son las pruebas que
ya existían, sin cambios en sus aserciones.

**L-A3** — Decisiones de T7 (#81). La tarjeta es un enlace a `/articulos/{id}` con la query del
catálogo (`?categoriaId=&pagina=`), armada con `armarConsulta`; el detalle la lee y su «Volver» la
devuelve. Sin estado en memoria: el enlace se puede copiar y recargar. El detalle pide **todas** las
categorías para nombrar una que se desactivó después de publicar. Un 404 (inexistente o dado de baja)
muestra «Este artículo no existe». Sale apilado sobre #63 (usa `paginado.svelte.ts`).

**L-A4** — Decisiones del usuario para #79 (verbatim del pedido):

> - La portada solo se conserva (sin archivo nuevo) o se reemplaza. No se puede quitar.
> - El reemplazo sigue el patrón del avatar: guardar el archivo nuevo, actualizar la fila y borrar el
>   anterior. Si la fila no se actualiza, se borra el nuevo y queda el anterior. No pueden quedar
>   imágenes huérfanas.
> - La fecha de publicación no cambia.
> - La categoría actual se conserva aunque esté inactiva; cambiar a otra exige una activa (mismo
>   criterio que habitos-edicion.md L4).
> - Solo Administrador puede editar, igual que el alta (403 para Usuario).
> - Reusá components/articulo-form.svelte: ya acepta `inicial` y `alEnviar` por prop. No copies el
>   formulario. La ruta acordada es /articulos/[id]/editar (ux-brief.md).

Decisiones del usuario para #80, tomadas por adelantado: los inactivos se ven y se reactivan en una
pantalla nueva **`/admin/articulos`**; se desactiva desde el **detalle**; desactivar pide
**confirmación con diálogo**; un artículo **inactivo no se edita** (primero se reactiva). Por eso el
`PUT` de #79 ya responde 404 sobre un inactivo.

Decisiones de implementación de T8: el `UPDATE` filtra `activo = TRUE`, así una baja que se cruza con
la edición termina en 404 y borra la portada nueva; no toca `fecha_publicacion` ni `activo`. Después
de guardar, la pantalla vuelve al detalle con la misma query del catálogo.

**L-A5** — Evidencia de T8. RED de `ArticulosEdicionEndpointTests`: 11/11 en rojo con `405 Method Not
Allowed`; GREEN 11/11. El camino «la fila no acepta la portada nueva» se prueba con un repositorio que
falla al actualizar (`ConfigureTestServices`): 500, la fila sigue con la anterior y el disco queda como
estaba. `check:api` 172/172, `check:web` 0 errores, `test:web` 67/67, e2e 4/4 (el de artículos suma la
edición conservando y reemplazando la portada, la anterior en 404 y el 403 del Usuario).
`svelte_autofixer` marcó como sobrante el `eslint-disable` del `goto` de la edición: es un falso
positivo, sin él eslint del repo falla con `svelte/no-navigation-without-resolve`.

La verificación independiente (solo lectura) dio PASS en S19, S21, S22 y S23, y PASS parcial en S20:
dos ediciones cruzadas del mismo artículo pueden dejar la fila apuntando a una portada borrada o una
imagen sin referencia, y el camino «baja cruzada → 404 y se borra la nueva» no tiene prueba propia.
Decisión del usuario (verbatim): «No te compliques con la concurrencia, no hagas eso, no pasa nada».
Queda aceptado como riesgo conocido: un solo Administrador edita, y el camino secuencial no deja
huérfanas.
