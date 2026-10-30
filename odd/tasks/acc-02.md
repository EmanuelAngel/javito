# ACC-02 · Acceso: login con JWT, guard del SPA y shell

Etapa 1, día 2 del plan de cierre. Rama `feat/acceso`. Cierra `RT-07` y el primer tercio de `RT-02`.
Hereda las decisiones de `odd/tasks/fundacion.md` S6–S10.

## Specs

- **S1 — Pruebas.** "Integración + 1 de vitest (recomendada)": cinco pruebas de integración sobre las 11
  de la fundación — login con credenciales buenas → 200 y token; credenciales malas → 401 con **el mismo**
  mensaje para email inexistente y para contraseña incorrecta; `desactivado@javito.local` → 401;
  `GET /api/categorias` sin token → 401; con token → 200 — y una de vitest para `pedir` (manda el
  `Bearer`, convierte el `ProblemDetails` en `ErrorDeApi` y en 401 desloguea). Descartado el camino
  "Sólo e2e con Playwright": "un 401 puede pasar por cualquier motivo" y no aísla el contrato.
- **S2 — Superficie HTTP del acceso.** "Sí: /yo devuelve la fila actual (recomendada)".
  `POST /api/sesion` (login, devuelve `{ token, usuario }`), `GET /api/sesion/yo` (el usuario del token,
  leído de **la fila actual** por `sub`, no de los claims congelados en el token) y `[Authorize]` sobre el
  `GET /api/categorias` que ya existe. La ruta va en español porque "la convención del repo es español
  para el dominio e inglés sólo para identificadores del framework, y la API ya está en español"; `auth`
  sería la única ruta en inglés de la API. Hoy `[Authorize]` va **sin roles**: el rol `Administrador`
  entra con `CAT-01`, mañana, y con él la evidencia de `RT-02`. Pregunta del usuario, respondida en L5:
  "¿esto sigue siendo stateless? ¿como debería ser una api rest con grado 2 de maduración?" — `/yo` no
  guarda estado, deriva la identidad del `Authorization` de esa misma request, y es un `GET` safe e
  idempotente sobre el recurso "principal autenticado", el mismo patrón que `/user` de la API de GitHub.
- **S3 — Paquetes y clave.** "user-secrets + fail-fast (recomendada)". `BCrypt.Net-Next` (verifica los
  `$2b$11$` del seed tal cual; reescribir el seed a PBKDF2 es peor) y
  `Microsoft.AspNetCore.Authentication.JwtBearer`. `Jwt:Clave` vive en `dotnet user-secrets` del proyecto
  `Javito.Api`, con fail-fast al arrancar y mensaje accionable, igual que `ConnectionStrings:Javito`:
  "evita una clave en un repositorio público".
- **S4 — Token vivo.** "El token vive hasta vencer (recomendada)". `activo` se valida **sólo en el
  login**; la ventana de 8 h se documenta. Cero consultas extra por request: hoy nadie puede desactivar a
  otro desde el sistema (`USU-05` es del día 6), así que el único camino es un `UPDATE` a mano.
- **S5 — Cliente HTTP y sesión.** "sesion.svelte.ts + 401 global (recomendada)".
  `src/lib/sesion.svelte.ts` con `$state` (token + usuario, persistido en `localStorage`) y
  `src/lib/api/pedir.ts` que suma el `Bearer`, convierte el `ProblemDetails` en `ErrorDeApi` con `codigo`
  y, en 401, hace logout y manda a `/login` **una sola vez**.
- **S6 — Shell.** "Mínimo: marca + avatar + salir (recomendada)". La barra inferior de cinco destinos
  entra con `REG-04`, cuando las rutas existan: "un destino que no lleva a ninguna parte en la Puerta 1 es
  peor evidencia que uno ausente".
- **S7 — Rutas del SPA en español.** "Todo en español (recomendada)": `/hoy`, `/habitos`, `/rutinas`,
  `/articulos`, `/perfil`, `/historial`, `/registro`. El `ux-brief` mezclaba `/today`, `/habits`,
  `/routines`, `/articles` con `/registro`, `/historial`, `/perfil`; su tabla de rutas se corrige en el
  mismo commit que el código.
- **S8 — `/login`.** "Funcional ya; impeccable si sobra (recomendada)". Tarjeta con dos campos y un botón,
  con los tokens de `layout.css`; la ronda de diseño queda para cuando sobre tiempo.
- **S9 — Raíz y página de humo.** "/ → /hoy; humo a /diagnostico (recomendada)". `/` redirige a `/hoy` y
  la página de vínculo SPA-API se muda a `/diagnostico`, **dentro** de `(app)` y detrás del guard.
- **S10 — Destino.** "Sí, ?destino= en el guard (recomendada)": el guard guarda a dónde ibas y el login
  devuelve ahí después de entrar.
- **S11 — Login y `pedir`.** "fetch crudo; el 401 es del formulario (recomendada)". El 401 del login no
  desloguea: se muestra como "email o contraseña incorrectos" en el formulario, con
  `codigo: "credenciales_invalidas"`. Si el login usara `pedir`, el handler global de 401 redirigiría a
  donde ya estás y se comería el mensaje.
- **S12 — Evidencia.** "Yo creo que con Scalar ya cubrimos el requerimiento de postman, ignoralo." La
  evidencia de `RT-07` son capturas de **Scalar**: 200 con `Bearer` y 401 sin token. Consecuencia: para
  que Scalar pueda mandar el token, el documento OpenAPI tiene que declarar el esquema `bearer`; eso entra
  en T2.
- **S13 — Nada de PR sin consultar.** Heredada de `fundacion.md` S12: "mandale, ahí prendí mysql, antes de
  hacer alguna pull request consultame". Commits y rama sí; push y PR sólo con autorización.
- **S14 — Heredadas de la fundación (S6–S10).** JWT en `localStorage`, 8 h sin refresh; claims `sub`,
  `rol`, `nombre`, `email`; BCrypt porque el seed es `$2b$11$`; logout local; el login devuelve
  `{ token, usuario }`; guard en `src/routes/(app)/+layout.ts` con el shell en `(app)/+layout.svelte` y
  `/login` afuera; 401 desloguea, 403 avisa y conserva la sesión.

## Tasks

| ID | Qué | Specs | Estado |
| :--- | :--- | :--- | :--- |
| T1 | Documento de feature, rama `feat/acceso` y `todo` | — | hecho |
| T2 | Paquetes, configuración JWT con fail-fast y esquema `bearer` en OpenAPI | S2, S3, S12 | hecho · `2f07deb` |
| T3 | `UsuarioRepositorio.BuscarPorEmailAsync` + `POST /api/sesion` | S2, S4, S11 | hecho · `2f07deb` |
| T4 | `GET /api/sesion/yo` + `[Authorize]` en categorías | S2 | hecho · `2f07deb` |
| T5 | Las 5 pruebas de integración | S1 | hecho · `2f07deb` |
| T6 | `sesion.svelte.ts` + `pedir.ts` + el vitest | S5, S11 | hecho · `50faf75` |
| T7 | `/login`, guard con `?destino=` y redirect de `/` | S7, S8, S10 | hecho · `c1dca66` |
| T8 | Shell `(app)`, `/diagnostico` y `/hoy` de arranque | S6, S9 | hecho · `c1dca66` |
| T9 | Correcciones de docs: `ux-brief`, `README`, `AGENTS.md` | S3, S7 | hecho · ver commit de docs |
| T10 | Evidencia `RT-07` en Scalar + `pnpm check`/`lint`/`build` | S12 | pendiente |

## Log

- **L1.** "entonces planifiquemos cómo encarar rt-07"
- **L2.** "Mejor usa tu sistema de question, es más cómodo"
- **L3.** "Antes de preguntar otra vez, explicame cuál es la ventaja de tener un endpoint /yo, porque nunca
  hice uno así. En mvc nunca tuve un /me, ¿cuáles son sus trade-offs?"
- **L4.** "perdón, no me termina de cerrar al ser al primera vez que lo veo. ¿esto sigue siendo stateless?
  ¿como debería ser una api rest con grado 2 de maduración?"
- **L5.** Respuestas del grilling, en orden: pruebas → "Integración + 1 de vitest (recomendada)"; clave y
  paquetes → "user-secrets + fail-fast (recomendada)"; token vivo → "El token vive hasta vencer
  (recomendada)"; cliente → "sesion.svelte.ts + 401 global (recomendada)"; shell → "Mínimo: marca +
  avatar + salir (recomendada)"; rutas → "Todo en español (recomendada)"; `/login` → "Funcional ya;
  impeccable si sobra (recomendada)"; raíz → "/ → /hoy; humo a /diagnostico (recomendada)"; destino →
  "Sí, ?destino= en el guard (recomendada)"; login → "fetch crudo; el 401 es del formulario
  (recomendada)"; `/yo` → "Sí: /yo devuelve la fila actual (recomendada)".
- **L6.** "Ok! Yo creo que con Scalar ya cubrimos el requerimiento de postman, ignoralo." — La evidencia de
  `RT-07` pasa de Postman a capturas de Scalar (S12). Consecuencia asumida: hay que declarar el esquema
  `bearer` en el documento OpenAPI, porque hoy Scalar no tiene dónde pegar el token.
- **L7.** Cierre de T2–T5 (`2f07deb`). Evidencia: `dotnet test` en verde, con la aritmética corregida
  por la verificación de L9: `main` tenía 14 pruebas y HEAD tiene 23, o sea 9 nuevas (5 de sesión y
  acceso + 4 de `pedir`), no 7; se observaron en rojo antes de implementar. Además `pnpm lint:api`
  sin cambios, y el documento OpenAPI sirviendo
  `securitySchemes.bearer` + `security: [{ bearer: [] }]` con la API levantada. Hallazgos:
  - **Las DataAnnotations en un record posicional van sobre el parámetro, no con `[property: …]`.** Con el
    prefijo, MVC lanza al validar: "Record type 'CredencialesDto' has validation metadata defined on
    property 'Password' that will be ignored".
  - En `Microsoft.OpenApi` 2.12 el enum es `SecuritySchemeType`, no `OpenApiSecuritySchemeType`.
  - `JwtBearer` corre fuera de MVC: sin `OnChallenge`/`OnForbidden` el 401 y el 403 salen con el cuerpo
    vacío y el SPA no puede decidir por `codigo`.
  - `UseAuthentication()` faltaba en `Program.cs`. Sin él, `[Authorize]` rechazaría toda request con token
    válido.
  - El 401 del login y el del challenge comparten el `codigo` `credenciales_invalidas` vs `no_autenticado`:
    son cosas distintas y el SPA las trata distinto.
  - Prueba de que `/yo` lee la fila y no los claims: se firma un token cuyos claims mienten y la respuesta
    devuelve los datos del seed.
  - `/yo` **no** revalida `activo`, por S4: rechazarlo ahí desloguearía al SPA mientras el mismo token
    seguiría sirviendo en el resto de la API.
  - La `Jwt:Clave` quedó cargada en `dotnet user-secrets` del proyecto `Javito.Api` (65 caracteres).
- **L8.** Cierre de T6–T9 (`50faf75`, `c1dca66` y el commit de docs). Evidencia: `pnpm check:web` 0 errores y
  0 advertencias, `pnpm lint:web` limpio, `pnpm test:web` 6/6 (los 4 de `pedir` se observaron en rojo
  antes de implementar) y `pnpm build:web` en 0 con las dos advertencias conocidas del kit. Smoke
  funcional contra la API levantada: login bueno → token de 348 caracteres; `/api/sesion/yo` → 200 con
  `{id:2, nombre, email, rol}`; `/api/categorias` con token → 200 y sin token → 401 con
  `codigo: no_autenticado`; login con contraseña mala → 401 con `codigo: credenciales_invalidas`;
  `desactivado@javito.local` → 401; token basura → 401. Hallazgos:
  - En SvelteKit 3.0.1 `goto` sale de `$app/navigation` y `page` de `$app/state` (runes, no stores).
  - `eslint` exige `resolve()` en `goto`/`href` (`svelte/no-navigation-without-resolve`); el `?destino=`
    del guard lleva un `eslint-disable` justificado porque la URL la construye el guard, no una ruta
    estática.
  - `pedir` arma las cabeceras con `new Headers(...)` y no con un spread: un spread descarta un `Headers`
    o un array de pares sin avisar. Además pone `Content-Type: application/json` cuando hay cuerpo.
  - La raíz se quedó sin `+page.svelte`: `+page.ts` redirige a `/hoy` antes de cualquier render.
- **L9.** Ronda de verificación independiente (rama `feat/acceso`, pre-commit): cinco correcciones y
  sus evidencias, sin commit (el orquestador decide cuándo):
  - **Cobertura de emisor, audiencia y vencimiento.** Nueva clase `TokensDeAccesoEndpointTests` con 4
    pruebas: token bien firmado con emisor `otro-emisor` y audiencia `otro-cliente` → 401
    `no_autenticado` como ProblemDetails, token vencido (firma propia a mano, `expires` 5 minutos
    atrás, porque `ServicioDeTokens` sólo emite tokens vigentes) → 401 igual, y login con contraseña
    incorrecta → 401 `credenciales_invalidas` (el login sigue siendo público). Las 4 pasaron de una
    porque la producción ya valida emisor/audiencia/vida. Prueba de que sirven: con
    `ValidateIssuer = false` y `ValidateAudience = false` en `Program.cs`, los tokens ajenos pasan y
    devuelven 200 (las 2 pruebas se observaron en rojo); con sólo el emisor en `false` seguían
    rechazando, porque `ValidateAudience` por defecto está encendida y con `ValidAudience` nulo
    invalida igual. Revertido y en verde otra vez.
  - **Dependencia explícita.** `System.IdentityModel.Tokens.Jwt` se usa directo en
    `ServicioDeTokens.cs` y `SesionController.cs` pero sólo llegaba transitivo vía `JwtBearer`. La
    `PackageReference` en `Javito.Api.csproj` con la versión que el árbol ya resolvía: `8.19.2`,
    tomada de `obj/project.assets.json` (no inventada). `dotnet build` en 0 y sin NU1xxx.
  - **OpenAPI del login.** El requisito `security` a nivel documento mentía sobre `POST /api/sesion`.
    En el mismo transformer, `EsquemaDeSeguridadEnOpenApi` ahora le vacía la `Security` a la
    operación POST de `/api/sesion`; `security: []` significa "sin autenticación" en OpenAPI. Smoke
    contra la API levantada: `metodos en /api/sesion: [post]`, `POST security: []`, y el resto
    (`/api/categorias`) sigue heredando el requisito del documento (`security: [{bearer: []}]`).
  - **Open redirect en `?destino=`.** `goto(destino)` aceptaba el query param crudo: un
    `?destino=//otro.host` sacaba al usuario del sitio. Nueva función `destinoSeguro` en
    `frontend/web/src/lib/navegacion.ts` (sólo `/` sin `//` ni `/\`; cualquier otra cosa → `/hoy`),
    usada en `login/+page.ts`, `login/+page.svelte` y el guard `(app)/+layout.ts`; el formato de la
    URL de login (`?destino=` con `encodeURIComponent`) vive ahora en `rutaDeLogin` del mismo
    módulo, que el guard usa en sus dos redirect.
  - **Aritmética del L7 corregida arriba**: el paso de 14 en `main` a 23 en HEAD es 9 nuevas
    (5 de sesión y acceso + 4 de `pedir`), no "eran 11 con 7 nuevas".
  - La verificación independiente no encontró bloqueantes. Detalle honesto: MySQL80 estaba detenido
    cuando corrió, por eso esa ronda no observó el verde de las pruebas de integración; en esta
    ronda se lo prendió (`net start MySQL80`) y el verde se observó acá, con los comandos del
    cierre.
