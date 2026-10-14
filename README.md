# Javito

Registro diario de hábitos personales, gráficos de consistencia y artículos educativos sobre ciencia del comportamiento.

> *Javito* suena a *hábito*. Nombre provisional, con chiste rioplatense.

Proyecto Integrador Final de **Laboratorio 2** (Desarrollo de Software Full Stack).
**Entrega: `2026-11-05`** · Modalidad individual · Angel Emanuel — emanuelangelsbr@gmail.com

---

## El problema que resuelve

El seguimiento de hábitos falla por problemas de las herramientas existentes:

1. **Fricción al registrar:** demasiados pasos y esperas para anotar una acción de dos segundos.
2. **Dependencia del celular:** casi todas son solo apps móviles, sin versión web para usar desde la computadora.
3. **Cobros excesivos:** suscripciones de USD 40–60 al año que bloquean funciones básicas (historial completo, más de 3 hábitos).
4. **Interfaces saturadas:** gamificación (vidas, oro, niveles) que distrae del objetivo.
5. **Falta de información:** casillas de verificación sin explicar cómo se arma y se sostiene un hábito.

### La propuesta

1. **Acceso web multiplataforma y responsivo:** funciona en cualquier navegador, sin instalar nada.
2. **Registro rápido y sin fricción:** botones de sí/no y contadores en la pantalla principal, guardado al instante sin recargar.
3. **Visualización clara de consistencia:** cuadrículas estilo contribuciones de GitHub, sin mecánicas de juego.
4. **Gratis y sin límites artificiales:** ni en cantidad de hábitos ni en historial.
5. **Módulo de educación:** artículos en Markdown sobre ciencia del comportamiento, con imagen de portada y gestión exclusiva del rol Administrador (cubre los requisitos de cátedra de manejo de archivos y funciones por rol).

---

## Stack

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core WebAPI (.NET 10) |
| Frontend | SPA en SvelteKit (`adapter-static`) |
| Base de datos | MySQL 8.0 |
| Acceso a datos | ADO.NET puro (`MySqlConnector`, **sin ORM**) |
| Autenticación | JWT con roles (`Administrador`, `Usuario`) |
| Documentación de API | OpenAPI / Swagger |

---

## Estructura del repositorio

Monorepo: backend y frontend viven juntos porque forman una sola entrega y comparten el script de base de datos.

```
javito/
├── .husky/                           # hooks: lint-staged y commitlint
├── .pi/mcp.json                      # MCP de Svelte del proyecto
├── AGENTS.md                         # convenciones para agentes
├── package.json                      # tooling: commitlint, lint-staged, husky
├── backend/
│   ├── Javito.slnx                   # solución
│   ├── Javito.Api/                   # ASP.NET Core WebAPI (.NET 10)
│   │   ├── Controllers/              # Repositories/ Models/ Dtos/ Auth/ llegan con S3
│   │   └── appsettings.Development.json
│   ├── Javito.Api.Tests/             # pruebas del backend
│   └── scripts/                      # check / format / lint del backend
├── frontend/
│   └── web/                          # SPA SvelteKit (adapter-static)
├── docs/                             # contexto, consignas, cronograma y DER
├── odd/tasks/                        # documentos de feature del trabajo trackeado
├── database.sql                      # estructura + datos de prueba
├── format-sql.sh                     # formateo de archivos .sql
└── README.md
```

> **Estado:** el scaffold del monorepo (`S1` del cronograma) está hecho: `backend/` con la solución y el
> proyecto WebAPI, `frontend/web/` con la SPA en `adapter-static`, y el repositorio Git inicializado con un
> solo commit raíz. Los directorios de dominio del backend (`Repositories/`, `Models/`, `Dtos/`, `Auth/`) y
> `database.sql` llegan con `S2` y `S3`. Las convenciones de trabajo están en [AGENTS.md](AGENTS.md).

---

## Puesta en marcha

### Requisitos

| Herramienta | Versión verificada |
|---|---|
| .NET SDK | 10.0.302 |
| Node | 24.20.0 |
| pnpm | 10.26.1 |
| MySQL | 8.0.46 |

### 1. Herramientas de la raíz

```sh
pnpm install
```

Instala `commitlint`, `lint-staged` y `husky`, y activa los hooks de Git mediante el script
`prepare`. Si los hooks no se disparan, corré `pnpm run prepare`. El formato y el lint del SPA viven en
el frontend: la raíz ya no tiene prettier, y `lint-staged` delega en el prettier del frontend.

### 2. Backend

```sh
dotnet run --project backend/Javito.Api --launch-profile http
```

Queda escuchando en `http://localhost:5256`. Verificación:

```sh
curl http://localhost:5256/api/health
# {"estado":"ok","servicio":"Javito.Api"}
```

Con el perfil `http` el documento OpenAPI queda en `http://localhost:5256/openapi/v1.json`.

Pruebas y formato:

```sh
./backend/scripts/check.sh     # dotnet build + dotnet test
./backend/scripts/lint.sh      # verifica el formato sin escribir
./backend/scripts/format.sh    # aplica el formato
```

### 3. Frontend

```sh
pnpm --dir frontend/web install
pnpm --dir frontend/web dev
```

Queda en `http://localhost:5173`. La pantalla inicial consulta `GET /api/health` y muestra
*"Conectado a Javito.Api"* cuando el vínculo entre capas funciona.

El backend habilita exactamente ese origen desde `Cors:AllowedOrigins`
(`backend/Javito.Api/appsettings.Development.json`). La política es **fail-closed**: sin orígenes
configurados no habilita ninguno. La URL de la API vive en un único lugar:
`frontend/web/src/lib/config.ts`.

Otros comandos:

```sh
pnpm --dir frontend/web check   # svelte-check
pnpm --dir frontend/web lint    # prettier --check + eslint
pnpm --dir frontend/web test:unit
pnpm --dir frontend/web build
```

> Con SvelteKit 3, `pnpm build` puede pasar con `pnpm check` en rojo. Corré los dos.

### 4. Base de datos y credenciales de prueba

**Pendiente** (`S2` y `S11` del cronograma): `database.sql` con las 7 tablas y los datos semilla, la
cadena de conexión en `backend/appsettings.Development.json` y las credenciales por rol.

### Nota sobre el build estático

`pnpm --dir frontend/web build` genera la salida desplegable en `frontend/web/build/`, con `index.html`
como fallback de la SPA. `pnpm preview` **no** sirve esa carpeta: previsualiza `.svelte-kit/output/` y
resuelve el HTML por SSR, así que una ruta que todavía no existe devuelve 404. Para probar `build/` hace
falta un servidor estático que aplique la regla de fallback; en desarrollo, `pnpm dev` ya maneja el ruteo
del cliente.

---

## Usuarios de prueba

**Pendiente** — se definen junto con `database.sql`. Requisito de promoción: un usuario por rol
(`Administrador` y `Usuario`) con credenciales documentadas acá.

---

## Matriz de Evidencias de Requerimientos Mínimos

Documento obligatorio de la consigna. Se completa a medida que cada hito del cronograma cierra.
`Estado` admite: **Pendiente**, **En curso**, **Cumplido**.

| # | Requerimiento mínimo | Estado | Dónde se implementa (previsto) | Evidencia |
|---|---|---|---|---|
| 1 | Modelo de datos: 6+ tablas relacionadas, con 1:N y N:M | Pendiente | `database.sql` · `docs/der.md` | DER adjunto |
| 2 | Seguridad: login/logout, roles, funcionalidad por rol y avatar | Pendiente | `backend/Javito.Api/Auth` · CRUD de Categorías y Artículos (solo Admin) | Captura de perfil con avatar |
| 3 | Manejo de archivos adicional al avatar | Pendiente | Imagen de portada de Artículos (`backend/Javito.Api/Controllers`) | GIF de subida |
| 4 | ABM/CRUD completo vía AJAX, sin recargar la página | Pendiente | SPA SvelteKit consumiendo la WebAPI | GIF del ABM dinámico |
| 5 | Paginado por servidor en los listados principales | Pendiente | Historial de registros y catálogo de artículos | Captura de red con `page=2` |
| 6 | Búsqueda asíncrona de entidades relacionadas | Pendiente | Selector de categorías en hábitos y artículos | GIF de la búsqueda en el desplegable |
| 7 | Web API REST con JWT (+ colección para pruebas) | Pendiente | Endpoints `[Authorize]` · colección exportada | Captura con Bearer Token |

### Requisitos adicionales para promocionar

- [x] `.gitignore` específico y sin artefactos de compilación
- [ ] Diagrama Entidad-Relación adjunto
- [ ] `README.md` con descripción y guía paso a paso
- [ ] `database.sql` con estructura y datos de prueba precargados
- [ ] Credenciales de prueba por rol
- [ ] Colección de Postman/Bruno exportada
- [ ] Todo el proyecto corriendo en `localhost` sin errores

---

## Modelo de datos (7 tablas)

`Usuario` · `Categoria` · `Habito` · `RegistroHabito` · `Rutina` · `RutinaHabito` · `Articulo`

El rol del usuario **no se guarda en una tabla**: se representa con un ENUM o constantes estáticas, a definir.

- Relación **1:N**: `Usuario` → `Habito`, `Usuario` → `Articulo`, `Habito` → `RegistroHabito`.
- Relación **N:M**: `Rutina` ↔ `Habito` mediante `RutinaHabito` (con orden de ejecución).
- `Articulo` y `Habito` dependen de dos tablas (`Usuario` + `Categoria`).
- Restricción `UNIQUE(habito_id, fecha)` en `RegistroHabito`: un registro por hábito por día. La meta se
  congela en el registro para no reescribir el historial si el usuario cambia su meta después.

El DER completo está en [docs/proyecto/propuesta.md](docs/proyecto/propuesta.md#21-diagrama-entidad-relación-der).

---

## Alcance y límite

**Alcance:** autenticación y sesiones · usuarios · categorías · hábitos · rutinas · registros diarios ·
artículos · cuadrícula de consistencia · paginado y búsqueda asíncrona.

**Límite:** no es offline ni app móvil nativa · sin integración con calendarios externos · sin
notificaciones push ni temporizadores en tiempo real · requiere conexión a internet.

---

## Estado del proyecto

| Hito | Estado |
|---|---|
| Propuesta aprobada por la cátedra | Cumplido |
| Repositorio y contexto documental | Cumplido |
| Scaffold del monorepo | Cumplido |
| Resto de los hitos | Ver [cronograma](docs/proyecto/cronograma.md) |

## Documentación

- [Índice de documentación](docs/README.md)
- [Propuesta completa (aprobada)](docs/proyecto/propuesta.md)
- [Cronograma](docs/proyecto/cronograma.md)
- [Consigna final de la cátedra](docs/consignas/consigna-final.md)
- [Especificación consolidada y criterios acordados](docs/consignas/consigna-y-definiciones.md)
- [Brief de interfaz y UX](docs/proyecto/ux-brief.md)

---

## Herramientas del repositorio

| Script | Uso |
|---|---|
| `./format-sql.sh [archivo]` | Formatea archivos `.sql` con `sql-formatter` |

Convención de commits: Conventional Commits en español, verbo en infinitivo, minúsculas y sin punto final
(`feat(repo): agregar repositorio de hábitos`).
