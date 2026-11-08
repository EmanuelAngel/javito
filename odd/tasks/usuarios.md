# Usuarios (parte 1): listado paginado, alta y edición

Módulo nuevo de administración de usuarios: #84 (HU-USU-01), #85 (HU-USU-02) y #86 (HU-USU-03), un PR
por issue, en ese orden. Solo Administrador, sin cambios de esquema, SQL a mano con MySqlConnector.
Este documento es la fuente que lee el chat de la parte 2: el contrato, los `codigo` y las decisiones
(`L#`) están acá y no en la conversación.

## Specs

Criterios literales de cada issue, más las consignas transversales del pedido.

### #84 — ver el listado de usuarios paginado (HU-USU-01)

- **S1** El listado es paginado en el servidor
- **S2** Muestra nombre, email, rol y estado
- **S3** Puedo buscar por nombre o email
- **S4** Distingo activos de desactivados

### #85 — crear un usuario desde administración (HU-USU-02)

- **S5** El camino normal sigue siendo el registro público; esto es la excepción
- **S6** Asigno la contraseña inicial y se la comunico yo: no hay envío de emails en el sistema
- **S7** Puedo elegir el rol desde el alta
- **S8** Valen las mismas reglas de email y contraseña que en el registro

### #86 — editar los datos de un usuario (HU-USU-03)

- **S9** Puedo editar nombre y email
- **S10** El email sigue sin poder repetirse
- **S11** La contraseña de otro usuario no se muestra ni se puede leer

### Transversales (consigna del chat)

- **S12** Todo es solo para Administrador (`[Authorize(Roles = Roles.Administrador)]`; en el SPA,
  `exigirAdministrador`). Un Usuario recibe 403 sin que se toque nada.
- **S13** El email ya es UNIQUE en la base y no distingue mayúsculas: chequealo antes con el
  repositorio y devolvé un 409 con codigo (como `nombre_repetido` de categorías). El UNIQUE queda como
  última defensa.
- **S14** `password_hash` nunca viaja en ningún DTO de salida.
- **S15** #86 edita datos, no el rol ni el estado: esos son de la parte 2.
- **S16** Sin cambios de esquema. SQL a mano con MySqlConnector. Errores con `RespuestasDeError` y
  `ErrorDeNegocio` con `codigo`, nunca `Problem()` a mano.

## Contrato decidido

Rutas y contratos que las tres tareas implementan. Un solo módulo de API, `api/usuarios`, y un solo
módulo de SPA, `lib/api/usuarios.ts`.

| Verbo | Ruta | Cuerpo / query | Respuesta | Issue |
| :--- | :--- | :--- | :--- | :--- |
| GET | `api/usuarios` | `ParametrosDePagina` + `q`? + `activo`? | `PaginaDto<UsuarioAdministradoDto>` | #84 |
| POST | `api/usuarios` | `AltaDeUsuarioDto` | `201` + `UsuarioAdministradoDto` | #85 |
| PUT | `api/usuarios/{id}` | `EdicionDeUsuarioDto` | `200` + `UsuarioAdministradoDto` | #86 |

- `UsuarioAdministradoDto(int Id, string Nombre, string Email, string Rol, bool Activo)`: solo lo que
  muestra el listado. **No** reemplaza a `UsuarioDto` (`Contratos/UsuarioDto.cs`), que es el contrato de
  `SesionDto` (login y `GET api/sesion/yo`) y queda intacto. `password_hash` no aparece en ninguno (S14).
- `AltaDeUsuarioDto(string Nombre, string Email, string Password, string Rol)`.
- `EdicionDeUsuarioDto(string Nombre, string Email)`: sin `Rol` ni `Activo` (S15), sin `Password` (S11).
- Códigos de error nuevos: `email_repetido` (409, mismo patrón que `nombre_repetido`),
  `rol_invalido` (400, solo `Administrador` o `Usuario`), `usuario_inexistente` no se agrega: el 404 usa
  `no_encontrado`, que es el único código de 404 del repo.
- Reglas de alta y edición, en un helper compartido (`ReglasDeUsuarios`) que **#68 va a reusar**: nombre
  1..100 no en blanco; email normalizado a minúsculas y sin espacios, formato válido, hasta 150;
  contraseña de 8..72 caracteres. Los largos de nombre y email coinciden con `database.sql`
  (VARCHAR(100) y
  VARCHAR(150)).
- Orden del listado: `ORDER BY id DESC` (L6). Filtro de estado `activo` con la misma forma que
  `GET api/habitos` (`[FromQuery] bool? activo`). Búsqueda `q` con la misma semántica que hábitos:
  `LIKE %q%` sobre `nombre` **o** `email`, escapando `\`, `%` y `_`.
- El alta responde **201 con el cuerpo y sin `Location`** (L13): no existe `GET api/usuarios/{id}`
  todavía, y un `Location` que devuelve 404 sería peor que no mandarlo. La cuenta nace activa, con
  `avatar_url` en `NULL` y la contraseña ya hasheada (L14).

## Tasks

| ID | Issue | Qué | Specs | Ruta | Commit |
| :--- | :--- | :--- | :--- | :--- | :--- |
| T1 | #84 | Backend del listado: repositorio (listar + contar + buscador), `UsuariosController.ListarAsync`, extracto de `APatron` a helper compartido con hábitos | S1-S4, S12-S14, S16 | inline (padre) | `588abba` |
| T2 | #84 | SPA del listado: `lib/api/usuarios.ts`, `lib/administracion-de-usuarios.svelte.ts`, `/admin/usuarios`, enlace en el shell, e2e | S1-S4, S12 | inline (padre) | `1f535f0` |
| T3 | #85 | Backend del alta: `ReglasDeUsuarios` + hash BCrypt + `POST api/usuarios` con 409 `email_repetido` y 400 `rol_invalido` | S5-S8, S12-S14, S16 | delegado (worker) | `f7d69f6` |
| T4 | #85 | SPA del alta: `/admin/usuarios/nuevo` con formulario (nombre, email, contraseña, rol) | S5-S8, S12 | delegado (worker) | `1086020` |
| T5 | #86 | Backend de la edición: `PUT api/usuarios/{id}`, 404 y 409 de email, sin rol/estado/contraseña | S9-S11, S13, S15, S16 | inline (padre) | pendiente |
| T6 | #86 | SPA de la edición: `/admin/usuarios/[id]/editar` reusando el formulario del alta | S9-S11, S15 | inline (padre) | pendiente |

Un PR por issue: PR 1 = T1+T2 (#84), PR 2 = T3+T4 (#85), PR 3 = T5+T6 (#86). T3 depende de T1 (mismo
controlador y repositorio) y T5 de T3, así que van apilados en ese orden: si el anterior no está
mergeado, el PR lo dice.

## Log

- **L1** — Pedido original, verbatim:
  > Javito, usuarios (parte 1): implementá #84 (listado paginado), #85 (crear) y #86 (editar),
  > un PR por issue, en ese orden.
  >
  > Contexto: mem_context y mem_search "javito backlog regularizar". Leé AGENTS.md entero (sobre todo
  > «Arquitectura», «Seguridad» y «Trampas conocidas de SvelteKit 3») y
  > .agents/skills/svelte-proyecto/SKILL.md. Es un módulo nuevo: creá odd/tasks/usuarios.md con los
  > criterios literales de cada issue como S#, las tareas como T# y las decisiones como L#. Ese doc
  > lo va a leer el chat de la parte 2: dejá anotado todo lo que decidas.
  >
  > Antes de escribir, revisá qué existe: UsuarioRepositorio (lo usan el login y el perfil), el
  > hash de contraseña que usa SesionController, el patrón de /admin/categorias y /admin/articulos,
  > backend/Javito.Api/Paginacion/ y, en el SPA, lib/paginado.svelte.ts + paginador.svelte. Reusá;
  > no escribas otro paginado, otro hash ni otro guard.
  >
  > Ya sabido:
  > - Todo es solo para Administrador ([Authorize(Roles = Roles.Administrador)]; en el SPA,
  >   exigirAdministrador). Un Usuario recibe 403 sin que se toque nada.
  > - El email ya es UNIQUE en la base y no distingue mayúsculas: chequealo antes con el
  >   repositorio y devolvé un 409 con codigo (como nombre_repetido de categorías). El UNIQUE queda
  >   como última defensa.
  > - password_hash nunca viaja en ningún DTO de salida.
  > - #86 edita datos, no el rol ni el estado: esos son de la parte 2.
  >
  > Antes de decidir, preguntame: quién define la contraseña inicial al crear un usuario, si el
  > listado busca por nombre o email y cómo, y cualquier otra cosa que el issue no defina.
  >
  > Reglas:
  > - Un issue por PR, desde origin/main actualizado. Si uno depende de otro sin mergear, apilalo y
  >   avisalo en el PR.
  > - Sin cambios de esquema. SQL a mano con MySqlConnector. Errores con RespuestasDeError y
  >   ErrorDeNegocio con codigo, nunca Problem() a mano.
  > - RED antes del fix: una prueba por regla, que verifique todos sus efectos observables, incluidos
  >   los 401 y 403 y que un rechazo no cambie la fila.
  > - svelte_autofixer en cada .svelte. Ningún +page.svelte pasa de 150 líneas.
  > - Checks pesados (check:api, test:e2e) de a uno y sin ningún Javito.Api corriendo. Si otro
  >   agente trabaja en paralelo, comparten la base javito_test: no corran check:api a la vez.
  >   Antes de cada PR, verde: pnpm lint, check:api, check:web, test:web y test:e2e.
  > - Toca seguridad: antes de abrir cada PR, lanzá una verificación independiente (gentle-ai-verify)
  >   y poné su veredicto en el PR.
  > - Nunca copies el repo ni node_modules a otra carpeta: Windows Defender se come la CPU.
  > - Commits en Conventional Commits en español. Al cerrar cada PR, guardá las decisiones con
  >   mem_save (topic_key javito/usuarios).

- **L2** — Hallazgo que precede a las decisiones: **el registro público no existe todavía**. No hay
  `RegistroController`, y `api/registro`, `Registrar` y `registrarse` no aparecen en
  `backend/Javito.Api`. El registro es el issue #68, abierto. Consecuencia: S5 describe una intención
  del producto y S8 no tiene de dónde copiarse. El usuario eligió (pregunta del chat) **fijar las
  reglas ahora en un helper compartido que #68 va a reusar**, en vez de frenar #85 o inventar reglas
  provisionales. Por eso existe `ReglasDeUsuarios` en T3 y por eso #68 queda como el primer consumidor
  de esas reglas, no como su origen.

- **L3** — Contraseña inicial (S6): **la escribe el administrador** en el formulario del alta, campo
  obligatorio. La respuesta del alta nunca la devuelve y no hay envío de emails. Descartadas: generarla
  en el sistema y mostrarla una vez; campo opcional con generación.

- **L4** — Búsqueda del listado (S3): **un solo campo `q`** que busca en nombre **o** email, con la
  misma semántica que `GET api/habitos` (`LIKE %q%`, escapando `\`, `%`, `_`). Descartados: `q` más un
  selector de columna; listado sin buscador.

- **L5** — Estado en el listado (S4): **marca visual por fila más filtro por estado** en la query y en
  la UI (`activo=true|false`, igual que hábitos). Descartado: solo la marca visual.

- **L6** — Orden del listado: el usuario pidió «por fecha de alta, el más nuevo primero», pero
  `usuario` **no tiene columna `fecha_alta`** (`database.sql:31-40`: id, rol, nombre, email,
  password_hash, avatar_url, activo) y el pedido prohíbe cambios de esquema. Se implementa
  `ORDER BY id DESC`: el id es `AUTO_INCREMENT`, así que el orden es exactamente el de alta, el más
  nuevo primero. Si la parte 2 quiere fecha real, es un cambio de esquema aparte.

- **L7** — DTO: se agrega `UsuarioAdministradoDto(Id, Nombre, Email, Rol, Activo)`. **No** se toca
  `UsuarioDto`, que viaja dentro de `SesionDto` (login y `/api/sesion/yo`) y es contrato de un camino
  que este pedido no menciona; sumarle `Activo` sería romperlo sin necesidad (S14).

- **L8** — Códigos de error: `email_repetido` (409) para el alta y la edición, calcado de
  `nombre_repetido` de categorías; `rol_invalido` (400) cuando el rol no es `Administrador` ni
  `Usuario`. El 404 reusa `no_encontrado`, el único código de 404 del repo: no se agrega un
  `usuario_inexistente`. Los errores previstos se **devuelven** con
  `RespuestasDeError.De(new ErrorDeNegocio(...))`, nunca se lanzan.

- **L9** — Reuso de la búsqueda: `APatron` hoy es privado de `HabitoRepositorio`. Se extrae a
  `Paginacion/PatronDeBusqueda.cs` y hábitos pasa a delegar ahí, para no tener dos copias del escape
  de `%` y `_`. Como toca código compartido con un listado que este pedido no menciona, la garantía
  es `backend/Javito.Api.Tests/BusquedaDeHabitosEndpointTests.cs`, que ya existe y cubre `q` con
  comodines en `GET api/habitos?q=`: no hizo falta inventar una prueba de caracterización nueva, se
  corrió ésa junto con las del listado de usuarios (14/14 verdes).

- **L10** — SPA: rutas `/admin/usuarios`, `/admin/usuarios/nuevo` y `/admin/usuarios/[id]/editar`,
  con `+page.ts` que hace `ssr = false` + `exigirAdministrador(parent)` (patrón de
  `admin/categorias/+page.ts`). El listado reusa `crearPaginado` de `lib/paginado.svelte.ts`,
  `paginador.svelte` y el patrón de `lib/administracion-de-articulos.svelte.ts`. El estado y los
  pedidos viven en `lib/`, no en el `+page.svelte`, para no pasar de 150 líneas.

- **L11** — e2e: `frontend/web/e2e/usuarios.e2e.ts`. El listado se prueba sobre las cuentas semilla;
  lo que cree el e2e del alta usa nombre prefijado `Pruebas e2e ` y **queda en la base de
  desarrollo**, porque la baja es lógica y es de la parte 2: `pnpm db:reset` lo borra. Queda anotado
  acá para que la parte 2 lo sepa.

- **L12** — Riesgo declarado de todo el módulo: **item 2 (seguridad)**. Se asignan roles y se escriben
  contraseñas. Cada PR lleva verificación independiente (`gentle-ai-verify`) y su veredicto en el PR.

- **L13** — El alta responde `201` con el cuerpo de `UsuarioAdministradoDto` y **sin cabecera
  `Location`**. `CreatedAtAction` necesita una acción que lea una cuenta por id, y `GET
  api/usuarios/{id}` no existe en la parte 1: mandar un `Location` que responde 404 enseña al cliente
  a seguir un enlace roto. Si la parte 2 agrega el detalle, se agrega el `Location` con él.

- **L14** — La cuenta creada por administración nace **activa** (`activo = TRUE`), con `avatar_url` en
  `NULL` y `password_hash` ya hasheado con `BCrypt.Net.BCrypt.HashPassword` (el mismo algoritmo que
  verifica el login; hoy el repo sólo llamaba a `Verify`). El estado y el rol de una cuenta existente
  se cambian en la parte 2 (#88), no acá.

- **L15** — Las reglas compartidas con el registro (#68) viven en `Usuarios/ReglasDeUsuarios.cs`, como
  `Archivos/ReglasDeArchivos.cs`: constantes de largo (nombre 1..100, email hasta 150, contraseña
  8..72), `NormalizarEmail` (trim + minúsculas) y la lista de roles válidos. Los DTO de entrada
  llevan las DataAnnotations —sobre el parámetro del record, sin `[property: …]`— y el controlador
  sólo agrega lo que una anotación no puede decir: `rol_invalido` y `email_repetido`.

- **L16** — El rol se compara **ordinal**, no por la intercalación del `ENUM`: `administrador` en
  minúsculas es 400 `rol_invalido`. El `ENUM` de MySQL lo aceptaría resolviéndolo solo, y ése es
  justamente el accidente que no queremos: un alta con un rol «parecido» cambia privilegios. El SPA
  manda los valores exactos de `ROLES` en `lib/api/usuarios.ts`, así que el error sólo aparece si
  alguien arma el cuerpo a mano.

- **L17** — El formato del email es el de `[EmailAddress]` de .NET, que es laxo: acepta `a@b` (un solo
  `@`, sin exigir punto ni dominio). No se inventó una regla más estricta porque ningún issue la
  define; si #68 pide otra, se cambia en un solo lugar (el DTO del alta y el del registro comparten
  `ReglasDeUsuarios`) y las dos puertas heredan el mismo criterio.

- **L18** — La contraseña va de **8 a 72 bytes**, no de 8 a 100 caracteres: BCrypt.Net hashea como
  máximo 72 bytes y trunca lo que sobra **en silencio**, así que aceptar más dejaría dos cuentas cuyas
  contraseñas se autentican entre sí con sólo compartir los primeros 72 bytes. Lo encontró el
  verificador independiente de #85 (aviso A1) y se corrigió antes de abrir el PR. No se puede
  pre-hashear para saltar el límite: `BCrypt.Verify` es lo que valida los hashes `$2b$11$` del seed y
  cualquier cambio de esquema rompería las cuentas existentes. El tope se expresa dos veces porque 72
  caracteres no son 72 bytes en UTF-8: `[StringLength(72, MinimumLength = 8)]` y
  `Usuarios/ContrasenaDeBcryptAttribute`, que mide con `Encoding.UTF8.GetByteCount`. El `maxlength` y
  el texto de ayuda del formulario dicen lo mismo que el servidor.

- **L19** — Revisión de seguridad del alta (#85): **APPROVED** con tres avisos. A1 (tope de BCrypt) se
  corrigió y quedó en L18. A2: un cuerpo con campos de más (`activo`, `id`) se ignora en silencio y
  responde 201 — no hay mass assignment porque el modelo se arma campo por campo; el contrato son
  cuatro campos. A3: se agregaron las pruebas que faltaban (rol en minúsculas, tope de 72 caracteres,
  tope de 72 bytes y email normalizado verificado por el listado, no por la columna).
