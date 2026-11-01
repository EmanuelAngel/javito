# CAT · Categorías: el ABM por AJAX

Etapa 1, día 3 del plan de cierre (sáb 31/10). Rama `feat/categorias`. Cierra `CAT-01`…`CAT-04` y
con ellos la **Puerta 1** y `RT-04`. Hereda las decisiones de `odd/tasks/acc-02.md`.

## Specs

- **S1 — Alcance.** Las cuatro historias del módulo, de punta a punta y sin recarga de página:
  `HU-CAT-01` (listado completo, distingue activas de desactivadas, busca por nombre),
  `HU-CAT-02` (alta con nombre obligatorio y único, descripción opcional, nace activa),
  `HU-CAT-03` (edición de nombre y descripción; el nombre sigue siendo único y no afecta a quien la usa),
  `HU-CAT-04` (baja lógica y reactivación; nunca se borra la fila).
- **S2 — Superficie HTTP.** Decisión del usuario: "Lectura abierta, escritura Administrador".

  | Verbo | Ruta | Quién | Devuelve |
  | :--- | :--- | :--- | :--- |
  | `GET` | `/api/categorias` | autenticado | ya existía (ACC-02) |
  | `POST` | `/api/categorias` | Administrador | `201` + `Location` + la categoría |
  | `PUT` | `/api/categorias/{id}` | Administrador | `200` con la categoría |
  | `PUT` | `/api/categorias/{id}/activo` | Administrador | `200` con la categoría |

  `GET` queda abierto porque el selector de hábitos lo va a necesitar con rol `Usuario` (`HAB-01`).
- **S3 — Baja lógica.** Decisión del orquestador bajo delegación ("hace lo que quieras, ya me canse"),
  después de discutir la propuesta del usuario de dos rutas (`/{id}/activar` y `/{id}/desactivar`):
  `PUT /api/categorias/{id}/activo` con `{ "activo": true|false }`. Una operación en vez de dos rutas
  con verbo en la URL, el SPA manda el estado destino y no ramifica, y `activo` es un campo de estado
  de `categoria`, `usuario`, `habito` y `articulo`: una sola forma para los cuatro agregados. Regla que
  se suma: repetir el estado que ya tiene es **idempotente**, devuelve `200`, nunca `409`. El `409`
  queda reservado para el nombre repetido.
- **S4 — Unicidad del nombre.** La impone la base: `nombre VARCHAR(60) UNIQUE` sobre
  `utf8mb4_0900_ai_ci`. No hay consulta previa: el `1062` de MySQL se traduce a `409` con
  `codigo: "nombre_repetido"`. Es lo único que cubre la carrera entre dos altas concurrentes.
  **Consecuencia declarada del esquema:** esa collation ignora mayúsculas *y acentos*, así que
  `Alimentacion` choca con `Alimentación`.
- **S5 — Errores.** `400` de DataAnnotations en el DTO de entrada (claves por campo en `errors`),
  `404` con `codigo: "no_encontrado"`, `409` con `codigo: "nombre_repetido"`, `403` con
  `codigo: "sin_permiso"` para rol `Usuario`, `401` con `codigo: "no_autenticado"` sin token. Todo
  `ProblemDetails`; el SPA decide por `codigo`.
- **S6 — Pruebas de integración.** Una por regla, sin recortes. Decisión del usuario: "solo
  integración y primer e2e de critical user journey" — **no hay vitest nuevo** en este módulo.
  El listado y la escritura van en la misma colección de xUnit: la base se recrea una sola vez por
  corrida, así que en paralelo las cuentas sobre el seed dejan de ser deterministas.
- **S7 — Primer e2e.** Playwright, el camino crítico de la Puerta 1 entero: guard → login → alta →
  edición → baja → reactivación → salida. El `webServer` de Playwright hoy solo levanta el SPA; se le
  suma la WebAPI. Base: **la de desarrollo** (`javito`), con nombre fijo `Pruebas e2e` y alta
  tolerante (si ya existe, se reusa) para que la prueba sea idempotente sin resetear nada: la baja es
  lógica y no hay `DELETE`. Trade-off aceptado: queda una fila inactiva por corrida. Se descartó una
  base aislada porque la contraseña solo vive en `dotnet user-secrets` y la API no puede apuntar a
  otro schema sin leerla desde Node.
- **S8 — UI.** Decisión del usuario: "Enlace en el header si es Administrador". La ruta es
  `/admin/categorias`, la que ya estaba en el mapa de superficies de `docs/ux-brief.md` (§4), no una
  inventada acá. Sin vendorizar shadcn: `src/lib/componentes/ui/` está vacío y meter componentes
  generados suma un diff que no lo cubren prettier ni eslint. Se usan los tokens de `layout.css`.
- **S9 — Búsqueda.** En el cliente sobre el listado completo: `HU-CAT-01` dice "son pocas y no se
  pagina", y `RT-06` (búsqueda asíncrona contra el servidor) es de rutinas, no de acá.
- **S10 — Documentación.** `AGENTS.md` (comando `pnpm test:e2e` y el estado real de los e2e) y la
  Puerta 1 de `docs/proyecto/plan-de-cierre.md`.
- **S11 — Fuera de alcance.** Barra de cinco destinos (`REG-04`), paginado, `DELETE` real, orden de la
  tabla, y `pnpm db:reset` sin prompt interactivo (queda como deuda aparte: hoy pide la contraseña
  porque usa la CLI de `mysql`, que no conoce `dotnet user-secrets`).
- **S12 — Riesgo.** `Risk: none`. No hay migración ni reescritura de datos, no cambia ningún contrato
  existente (solo se agregan verbos y se restringe la escritura por rol) y las pruebas cubren cada
  regla nueva.

## Tasks

| ID | Qué | Specs | Ruta | Commit |
| :--- | :--- | :--- | :--- | :--- |
| T1 | Pruebas de integración de las cuatro historias, en rojo | S1, S4–S6 | inline | `test(api): cubrir el ABM de categorías` |
| T2 | Contratos de entrada, repositorio de escritura y endpoints por rol | S2–S5 | inline | `feat(api): agregar alta, edición y baja lógica de categorías` |
| T3 | Primer e2e del camino crítico, en rojo | S7 | inline | `test(e2e): agregar el recorrido de la puerta 1` |
| T4 | Módulo de API del SPA, pantalla `/admin/categorias` y enlace en el header | S8, S9 | inline | `feat(web): agregar la pantalla de categorías` |
| T5 | Comando `test:e2e` en la raíz y documentación del cierre | S10 | inline | `docs(proyecto): cerrar la puerta 1` |

## Log

- **L1.** Pedido original del usuario, textual: "Dale, veamos cómo encaramos cat-01...cat-04". Decisiones
  textuales de la ronda de preguntas: "Lectura abierta, escritura Administrador" y "Enlace en el header
  si es Administrador" y "Sin querer dije integración + vitest, quise decir solo integración y primer
  e2e de critical user journey". Cierre de la ronda, textual: "hace lo que quieras, ya me canse".
- **L2.** Sobre la baja lógica el usuario propuso `PATCH /{id}/activar` y `PATCH /{id}/desactivar` y
  pidió discutirlo: el argumento fue que "en algún módulo futuro las desactivaciones y activaciones
  puedan incluir acciones extra". Se concedió que los *action endpoints* existen (Okta
  `/lifecycle/deactivate`, PayPal `/capture`) pero no aplican hoy: en `categoria` las dos reglas son el
  mismo `UPDATE`, y `HU-CAT-04` dice explícitamente que desactivar no toca hábitos ni artículos. El día
  que las reglas difieran (`USU-05`) la forma correcta es un recurso de comando con body
  (`POST /api/usuarios/{id}/baja { motivo }`), no un par de verbos sin body. Decisión final en S3.
- **L3.** Sobre la base del e2e el usuario objetó que `pnpm db:reset` pida la contraseña. Dato: la
  contraseña sí está guardada, en `dotnet user-secrets` del proyecto `Javito.Api`
  (`ConnectionStrings:Javito` y `Jwt:Clave`); quien la usa sin prompt es el fixture de pruebas, que
  habla MySQL por `MySqlConnector`. Ningún camino de shell la conoce, y por eso `db:reset` la pide.
  Ese arreglo (script que lea user-secrets y arme un `--defaults-extra-file` temporal) queda fuera de
  este documento, en S11.
