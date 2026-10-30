# Fundación: conexión MySQL, contrato de errores y categorías

Etapa 0 del proyecto, previa a `ACC-02`. Rama `feat/fundacion`, issue #11.
Cierra cuando la WebAPI devuelve datos reales de MySQL con el contrato de errores fijado.

## Specs

- **S1 — Unidad propia.** La fundación es su propio issue y documento, y cierra con un endpoint
  leyendo datos reales. `ACC-02` (JWT, guard, shell) abre después, aparte. Respuesta del usuario:
  "A · Unidad propia, cierra con endpoint real (recomendada)".
- **S2 — Sin capa de servicios.** "Lo que recomendás pero sin servicios", alineado con la inmobiliaria.
  `IDbConnectionFactory` + un repositorio por agregado **con interfaz e implementación**
  (`ICategoriaRepositorio` / `CategoriaRepositorio`), SQL a mano, que devuelve **clases modelo** de
  `Modelos/` **sin DataAnnotations**: en una WebAPI el modelo es persistencia y las reglas de entrada
  se validan en el DTO que llega por HTTP, donde producen un 400 legible. El controlador proyecta a
  DTO antes de responder, así el modelo nunca se serializa. Helpers de archivos y JWT no son capas. La
  atomicidad, cuando exista, es del repositorio; la fundación no escribe transacciones. Sin
  `IRepositorio<T>` genérico ni `RepositorioBase`: cada agregado declara solo las operaciones que usa.
- **S3 — Contrato de errores.** "A · ProblemDetails + codigo estable (recomendada)":
  `ProblemDetails` (RFC 9457) en todo, validación con DataAnnotations y una extensión `codigo`
  estable para errores de negocio.
- **S4 — Pruebas contra MySQL.** "A · javito_test recreado, dentro de check:api (recomendada)":
  fixture que recrea el schema `javito_test` desde `database.sql` una vez por corrida;
  `WebApplicationFactory` para endpoints; entra en `pnpm check:api`, que entonces exige MySQL.
- **S5 — Alcance del día 1: solo lectura.** "Solo lectura; codigo se estrena en CAT-02
  (recomendada)": `GET /api/categorias` con filtro `activo`, `/api/health` con estado de base, 400
  de binding, 404 de ruta inexistente y el mapeador de error de dominio con prueba unitaria. Sin
  escrituras de producto.
- **S6 — JWT (para `ACC-02`, no se implementa acá).** "localStorage + Bearer, 8 h, sin refresh
  (recomendada)". Claims `sub`, `rol`, `nombre`, `email`; logout local; BCrypt porque el seed es
  `$2b$11$`.
- **S7 — Login devuelve `{ token, usuario }`.** "El login devuelve { token, usuario }
  (recomendada)".
- **S8 — Guard de la SPA en `(app)`.** El usuario preguntó: "Es la primera vez que veo que
  paréntesis envuelven, ¿consultaste eso con alguna skill/mcp de svelte/kit?". Verificado en el kit
  instalado `3.0.1`: `src/utils/routing.js:139-146` (`affects_path` devuelve `false` para
  `(grupo)`), línea 7 (`root_group_pattern`), y `redirect` exportado en `src/exports/index.js:129`.
  Guard en `src/routes/(app)/+layout.ts`, shell en `(app)/+layout.svelte`, `/login` afuera.
- **S9 — Cliente HTTP.** El usuario: "Estaba pensando en agregar openapi con scalar […] el profesor
  quiere ver que sepamos usar fetch no tan tuneado […] Otra opción que se me ocurre es generar los
  tipos de typescript a partir del esquema que genere openapi". Decisión: Scalar sí; codegen no;
  `pedir<T>()` de ~20 líneas con `fetch` plano adentro, más un módulo por recurso.
- **S10 — 401 vs 403.** "401 desloguea, 403 avisa (recomendada)". Un 403 conserva la sesión porque
  es la evidencia de `RT-02`.
- **S11 — Credenciales fuera de git.** Corrección a la decisión inicial de commitear el connection
  string: `mysql -u root` sin contraseña devuelve `ERROR 1045 (28000)`, así que la contraseña existe
  y no viaja en un repositorio público. `ConnectionStrings:Javito` se commitea **sin** contraseña; la
  contraseña vive en `dotnet user-secrets` del proyecto `Javito.Api` (fuera del repo). Las pruebas
  resuelven la misma cadena con `AddUserSecrets<Program>()` y cambian el nombre de base a
  `javito_test`; la variable `ConnectionStrings__Javito` tiene prioridad si está definida.
- **S12 — Nada de PR sin consultar.** "mandale, ahí prendí mysql, antes de hacer alguna pull request
  consultame". Commits y rama: sí. Push y PR: sólo con autorización.

## Tasks

| ID | Qué | Specs | Estado |
| :--- | :--- | :--- | :--- |
| T1 | Fixture `javito_test` desde `database.sql` + preflight con mensaje accionable | S4, S11 | hecho · `6f684d8` |
| T2 | `IDbConnectionFactory` + `CategoriaRepositorio` + `GET /api/categorias` con filtro | S2, S5 | hecho · `6f684d8` |
| T3 | `GET /api/health` con estado de base, conservando `estado` y `servicio` | S5 | hecho · `6f684d8` |
| T4 | Contrato de errores: `AddProblemDetails`, 400 de binding, 404, mapeador `ErrorDeNegocio` | S3, S5 | hecho · `6f684d8` |
| T5 | Documentación del entorno local (MySQL, `database.sql`, user-secrets) | S4, S11 | hecho · ver commits de docs |
| T6 | Scalar sobre el OpenAPI existente | S9 | hecho · `6f684d8` |

## Log

- **L1.** "empecemos con una sesión liviana de grilling para la fundación del proyecto"
- **L2.** "Usá tu sistema de question para esto"
- **L3.** "Lo que recomendás pero sin servicios" (respuesta a la forma de la capa de datos).
- **L4.** "Discutamos mejor esto con ejemplos. En el proyecto de inmobiliaria no tuve que hacer
  alguna transacción, tal vez por eso me cuesta visualizar esto. Veamos trade-offs con ejemplos.
  Paremos el grilling para esto."
- **L5.** "Supongamos que sí me cierra así […]. Sigamos" — con el pedido de respuestas más directas.
- **L6.** "No sé, es la primera vez que hago un frontend totalmente desacoplado. Explicame."
- **L7.** "Estaba pensando en agregar openapi con scalar para facilitar las cosas. Pero no sé qué
  tanto nos va a facilitar. Tal vez los primeros fetch sean sueltos. Igual el profesor quiere ver
  que sepamos usar fetch no tan tuneado, por eso mismo no me convence usar el fetch personalizado
  de openapi. ¿Qué sugieres teniendo en mente esto? Otra opción que se me ocurre es generar los
  tipos de typescript a partir del esquema que genere openapi, pero no sé qué tanto nos complique
  eso más que ayudar con los tiempos."
- **L8.** "Es la primera vez que veo que paréntesis envuelven, ¿consultaste eso con alguna
  skill/mcp de svelte/kit? te pregunto por miedo ya que hace poco salió sveltekit 3."
- **L9.** "Seguramente vayan a haber correcciones, no tengo manera de imaginar cómo quedará todo
  ahora mismo. ¿Qué opinas de eso que pienso?"
- **L10.** "mandale, ahí prendí mysql, antes de hacer alguna pull request consultame"
- **L11.** Corrección de S11: `mysql -u root` sin contraseña falla con `ERROR 1045 (28000)`. La
  contraseña existe, así que el connection string commiteado va sin ella.
- **L12.** "¿no están los dotnet secret para eso?" — Sí: el secreto va en `dotnet user-secrets` del
  proyecto `Javito.Api` (`UserSecretsId 7b13dd07-5b6b-4b71-9e7a-5c1693124168`), fuera del repositorio.
- **L13.** Evidencia de cierre: `dotnet test` 11/11, `pnpm check` y `pnpm lint` verdes, `dotnet format`
  sin cambios. El mapeador se observó en rojo (`NotImplementedException`) antes de implementarlo, y el
  preflight se observó en rojo con `Access denied for user 'root'` antes de cargar el secreto.
  Smoke con la API levantada: `/scalar/v1` 200, `/openapi/v1.json` 200, `/api/no-existe` 404
  ProblemDetails.
- **L15.** Con la base de desarrollo creada (`pnpm db:reset`), el smoke completo contra `javito` da:
  `health` → `{"estado":"ok","baseDeDatos":"ok"}`; `/api/categorias` → 5 filas del seed;
  `?activo=false` → sólo `Espiritualidad`; `?activo=basura` → 400. Fundación cerrada.
- **L16. Feedback del autor, pendiente de análisis — no son cambios pedidos todavía.**
  1. "Esperaba que cada agregado tuviera su propia clase modelo, no que todo se maneje vía DTOs.
     Sé que tal vez no es necesario pero bueno, así nos enseñaron en la cátedra".
  2. "Esperaba que cada repositorio tuviera su interfaz e implementación."
  3. "No entendí muy bien qué hiciste con los errores o por qué (no digo que esté mal)."
  4. "No entendí PARA NADA lo que hice con los tests"; "nunca hice testing en ASP.NET Core";
     "considerar solo quedarme con tests e2e".
  5. "¿Por qué los DTO son records y no class con data annotations?"
     Cierre del autor: "Son cosas para analizar, no para que hagas cambios. […] mañana vemos".
- **L17.** "Toma el proyecto inmobiliaria como base y corregí modelos, interfaces y tests" ("tests la
  inmobiliaria no tiene"). Hecho: `Modelos/Categoria.cs` con DataAnnotations como
  `Models/TipoInmueble.cs` de la inmobiliaria; `ICategoriaRepositorio` + implementación; el controlador
  proyecta con `MapeoDeCategorias.ADto`; DI por interfaz. Desvío consciente de la inmobiliaria: no se
  copia `IRepositorio<T>` genérico ni `RepositorioBase`, porque obligarían a cada agregado a declarar
  un CRUD que no usa. En tests, el fixture pasó de `MySqlBatch` + un divisor por `yield` a **una sola**
  sentencia con el script completo y `AllowUserVariables`: 25 líneas menos y nada que mantener. Las 11
  pruebas siguen verdes. Nota de entorno: el servicio `MySQL80` estaba detenido y se inició con
  `net start MySQL80`.
- **L18.** "sacalos [los DataAnnotations de los modelos] y dejalos para los dto de entrada". Los
  atributos salieron de `Modelos/Categoria.cs`; el modelo quedó 1:1 con la tabla y los límites viven
  en `database.sql`. Las DataAnnotations se mudan a los DTO de entrada cuando llegue `CAT-02`, donde
  `[ApiController]` las convierte en 400 con `errors`. 11/11 y `dotnet format` verdes.
- **L19. Bug post-merge, ya corregido.** El fixture sustituía `CREATE DATABASE` y `USE`, pero no el
  `DROP DATABASE IF EXISTS javito;` de `database.sql:20`: **cada `dotnet test` borraba la base de
  desarrollo**. Se detectó desde Scalar con `Unknown database 'javito'`. Corrección en
  `ScriptDePruebas.Adaptar`: sustituye las tres sentencias y después verifica que toda sentencia de
  schema apunte a `javito_test`, así un cambio futuro en `database.sql` falla antes de conectar. Tres
  pruebas unitarias sin MySQL (renombrado, seed intacto, base ajena que debe explotar). Prueba de
  punta a punta: `javito` existía antes y después de correr `dotnet test` (14/14) y la API devolvió las
  5 categorías.
- **L14.** Corrección de implementación: `database.sql` usa variables de sesión (`SET @lunes = …`), así
  que el fixture ejecuta el script con `Allow User Variables=true`; sin eso MySqlConnector lo lee como
  parámetro y falla con `Parameter '@lunes' must be defined`.
