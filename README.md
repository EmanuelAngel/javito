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
├── .editorconfig                     # LF e indentación (fuente de verdad de dotnet format)
├── .husky/                           # hooks: lint-staged y commitlint
├── .pi/mcp.json                      # MCP de Svelte del proyecto
├── AGENTS.md                         # convenciones para agentes
├── lint-staged.config.js             # reglas del hook pre-commit
├── package.json                      # tooling: commitlint, lint-staged, husky
├── backend/
│   ├── Javito.slnx                   # solución
│   ├── Javito.Api/                   # ASP.NET Core WebAPI (.NET 10)
│   │   ├── Controllers/              # Repositories/ Models/ Dtos/ Auth/ todavía no existen
│   │   └── appsettings.Development.json
│   ├── Javito.Api.Tests/             # pruebas del backend
│   └── scripts/                      # check / format / lint del backend
├── frontend/
│   └── web/                          # SPA SvelteKit (adapter-static)
├── docs/                             # contexto, consignas, propuesta y DER
├── odd/tasks/                        # documentos de feature del trabajo trackeado
├── database.sql                      # estructura + datos de prueba
├── format-sql.sh                     # formateo de archivos .sql
└── README.md
```

> **Estado:** el scaffold del monorepo y la base de datos están hechos: `backend/` con la solución y el
> proyecto WebAPI, `frontend/web/` con la SPA en `adapter-static`, y `database.sql` con las 7 tablas y los
> datos de prueba. Los directorios de dominio del backend (`Repositories/`, `Models/`, `Dtos/`, `Auth/`)
> todavía no existen: la API solo responde `GET /api/health` y no se conecta a la base. Las convenciones de
> trabajo están en [AGENTS.md](AGENTS.md).

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
el frontend: la raíz ya no tiene prettier. El hook `pre-commit` corre, sobre los archivos en stage,
`prettier --write` y `eslint --fix` del SPA y `dotnet format` de los `.cs`: el alcance de `pnpm lint`,
acotado a lo que se commitea.

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

Pruebas y formato (desde la raíz del repositorio):

```sh
pnpm check       # dotnet build + dotnet test + svelte-check
pnpm lint        # verifica el formato sin escribir
pnpm format      # aplica el formato
```

El resto de los atajos está en la tabla de `AGENTS.md`; `pnpm run` los lista.

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

### 4. Base de datos

`database.sql` crea la base `javito` **desde cero**: borra lo que hubiera y vuelve a cargar la estructura
con las 7 tablas y los datos de prueba. Con MySQL corriendo:

```sh
mysql --table < database.sql
```

`--table` imprime con formato las consultas que corras después, útil para revisar lo que quedó cargado.
Si no querés escribir la contraseña cada vez, guardala una vez con `mysql_config_editor` y usá
`mysql --login-path=<nombre> --table < database.sql`.

El script arranca con `SET NAMES utf8mb4;` porque la terminal de Windows habla `cp850`: sin esa línea los
acentos se guardan corruptos. No hay que configurar nada más por eso.

Las fechas de los registros son relativas al día en que se ejecuta el script, así que la semana en curso
siempre tiene datos, sin importar cuándo lo cargues.

El backend se conecta con `ConnectionStrings:Javito`, que está en
`backend/Javito.Api/appsettings.Development.json` **sin contraseña**: el secreto vive fuera del
repositorio, en `dotnet user-secrets`.

```sh
dotnet user-secrets set "ConnectionStrings:Javito" "Server=localhost;Port=3306;Database=javito;User ID=root;Password=TU_CLAVE" --project backend/Javito.Api
```

`pnpm db:reset` recrea la base de desarrollo desde `database.sql`. Las pruebas usan un schema
aparte, `javito_test`, que el fixture recrea en cada corrida: por eso **`pnpm check:api` necesita MySQL
prendido**, y falla con un mensaje que dice qué correr cuando no lo está.

Con la API levantada, la documentación interactiva (Scalar) queda en
`http://localhost:5256/scalar/v1`.

### Nota sobre el build estático

`pnpm --dir frontend/web build` genera la salida desplegable en `frontend/web/build/`, con `index.html`
como fallback de la SPA. `pnpm preview` **no** sirve esa carpeta: previsualiza `.svelte-kit/output/` y
resuelve el HTML por SSR, así que una ruta que todavía no existe devuelve 404. Para probar `build/` hace
falta un servidor estático que aplique la regla de fallback; en desarrollo, `pnpm dev` ya maneja el ruteo
del cliente.

---

## Usuarios de prueba

Vienen cargados en `database.sql`, con la contraseña **`Javito2026`** para los cuatro.

| Email | Rol | Estado |
|---|---|---|
| `admin@javito.local` | `Administrador` | Activo |
| `usuario@javito.local` | `Usuario` | Activo, con los 14 hábitos reales y sus registros |
| `otra@javito.local` | `Usuario` | Activo, para comprobar que nadie ve datos ajenos |
| `desactivado@javito.local` | `Usuario` | Inactivo: no puede iniciar sesión |

Las contraseñas están guardadas como hash bcrypt. El login todavía no existe: se verifica contra estos
usuarios cuando llegue la autenticación con JWT.

---

## Matriz de Evidencias de Requerimientos Mínimos

Documento obligatorio de la consigna. Se completa a medida que el proyecto avanza.
`Estado` admite: **Pendiente**, **En curso**, **Cumplido**.

| # | Requerimiento mínimo | Estado | Dónde se implementa (previsto) | Evidencia |
|---|---|---|---|---|
| 1 | Modelo de datos: 6+ tablas relacionadas, con 1:N y N:M | En curso | `database.sql` · [DER](docs/proyecto/propuesta.md#21-diagrama-entidad-relación-der) | DER adjunto |
| 2 | Seguridad: login/logout, roles, funcionalidad por rol y avatar | Pendiente | `backend/Javito.Api/Auth` · CRUD de Categorías y Artículos (solo Admin) | Captura de perfil con avatar |
| 3 | Manejo de archivos adicional al avatar | Pendiente | Imagen de portada de Artículos (`backend/Javito.Api/Controllers`) | GIF de subida |
| 4 | ABM/CRUD completo vía AJAX, sin recargar la página | Pendiente | SPA SvelteKit consumiendo la WebAPI | GIF del ABM dinámico |
| 5 | Paginado por servidor en los listados principales | Pendiente | Historial de registros y catálogo de artículos | Captura de red con `page=2` |
| 6 | Búsqueda asíncrona de entidades relacionadas | Pendiente | Selector de hábitos al armar una rutina | GIF de la búsqueda en el desplegable |
| 7 | Web API REST con JWT (+ colección para pruebas) | Pendiente | Endpoints `[Authorize]` · colección exportada | Captura con Bearer Token |

### Requisitos adicionales para promocionar

- [x] `.gitignore` específico y sin artefactos de compilación
- [x] Diagrama Entidad-Relación adjunto ([DER en Mermaid](docs/proyecto/propuesta.md#21-diagrama-entidad-relación-der))
- [x] `README.md` con descripción y guía paso a paso
- [x] `database.sql` con estructura y datos de prueba precargados
- [x] Credenciales de prueba por rol
- [ ] Colección de Postman/Bruno exportada
- [ ] Todo el proyecto corriendo en `localhost` sin errores

---

## Modelo de datos (7 tablas)

`Usuario` · `Categoria` · `Habito` · `RegistroHabito` · `Rutina` · `RutinaHabito` · `Articulo`

El rol del usuario **no vive en una tabla**: `usuario.rol` es un `ENUM('Administrador', 'Usuario')`.

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
| Base de datos con datos de prueba (`database.sql`) | Cumplido |

## Documentación

- [Índice de documentación](docs/README.md)
- [Propuesta completa (aprobada)](docs/proyecto/propuesta.md)
- [Historias de usuario](docs/proyecto/historias-de-usuario.md)
- [Consigna final de la cátedra](docs/consignas/consigna-final.md)
- [Especificación consolidada y criterios acordados](docs/consignas/consigna-y-definiciones.md)

---

## Herramientas del repositorio

| Script | Uso |
|---|---|
| `./format-sql.sh [archivo]` | Formatea archivos `.sql` con `sql-formatter` |

Convención de commits: Conventional Commits en español, verbo en infinitivo, minúsculas y sin punto final
(`feat(repo): agregar repositorio de hábitos`).
