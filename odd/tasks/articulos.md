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
