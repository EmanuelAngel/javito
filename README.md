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
│   │   ├── Controllers/              # endpoints REST
│   │   ├── Repositorios/             # SQL a mano con MySqlConnector
│   │   ├── Modelos/ Contratos/       # filas de la base y DTOs que viajan
│   │   ├── Seguridad/                # JWT y roles
│   │   ├── Archivos/                 # subida y validación de imágenes
│   │   └── Paginacion/               # parámetros y página del paginado por servidor
│   └── Javito.Api.Tests/             # pruebas del backend (xUnit)
├── frontend/
│   └── web/                          # SPA SvelteKit (adapter-static)
├── docs/                             # contexto, consignas, propuesta y DER
├── odd/tasks/                        # documentos de feature del trabajo trackeado
├── database.sql                      # estructura + datos de prueba
├── format-sql.sh                     # formateo de archivos .sql
└── README.md
```

> **Estado:** la fundación y el acceso están hechos: MySQL por ADO.NET, errores `ProblemDetails` con un
> `codigo` estable, JWT con guard en la SPA y shell autenticado. Sobre eso viven el ABM de categorías
> (sólo Administrador), el alta y el listado de hábitos, el día con sus marcas, el perfil con avatar,
> el catálogo de artículos (sólo Administrador para publicar) y las rutinas con su búsqueda asíncrona.
> La API expone `POST /api/sesion`, `GET /api/sesion/yo`, `GET|POST|PUT /api/categorias`,
> `GET|POST /api/habitos`, `PUT|DELETE /api/habitos/{id}/registros/{fecha}`, `GET /api/rutinas`,
> `GET|POST /api/rutinas/{id}`, `POST|DELETE /api/rutinas/{id}/habitos`, `GET /api/dias/{fecha}`,
> `POST /api/perfil/avatar`, `GET|POST /api/articulos`, `GET /api/articulos/{id}` y `GET /api/health`.
> El listado de hábitos acepta `q` (pedazo de título en un `LIKE` con los comodines escapados) y, con
> `q` dado, sólo devuelve hábitos activos: es el servidor adonde consulta el buscador de rutinas.
> Las convenciones de trabajo están en [AGENTS.md](AGENTS.md).

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

La clave con la que se firman los JWT también vive fuera del repositorio. Sin ella **la API no
arranca**: falla al construirse, con un mensaje que dice cómo cargarla. Necesita 32 caracteres o más.

```sh
dotnet user-secrets set "Jwt:Clave" "TU_CLAVE_DE_AL_MENOS_32_CARACTERES" --project backend/Javito.Api
```

La sesión dura **8 horas y no se renueva**. Los endpoints de acceso son `POST /api/sesion` (login) y
`GET /api/sesion/yo` (quién soy, para validar la sesión al recargar). El resto exige
`Authorization: Bearer <token>`; sin él responde 401 con `codigo: "no_autenticado"`.

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

Las contraseñas están guardadas como hash bcrypt. El login (`POST /api/sesion`) las verifica y devuelve
un JWT con el rol del usuario.

---

## Matriz de Evidencias de Requerimientos Mínimos

Los enlaces apuntan a la línea exacta del código. Las evidencias en `frontend/web/e2e/` son recorridos de
extremo a extremo con Playwright: reproducen cada funcionalidad en el navegador con `pnpm test:e2e`.

| Requerimiento Mínimo | Estado | Enlace al Código / Controladores | Evidencia (Captura / GIF / Video) |
|---|---|---|---|
| 1. 7 tablas, relación 1:N y N:M | Cumplido | [database.sql#L101 (`rutina_habito`, N:M)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/database.sql#L101-L112) · [database.sql#L51 (`habito`, 1:N y dependiente de `usuario` + `categoria`)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/database.sql#L51-L68) | [DER en Mermaid](docs/proyecto/propuesta.md#21-diagrama-entidad-relación-der) |
| 2. Login, logout, roles y avatar | Cumplido | [SesionController.cs#L22 (login)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/SesionController.cs#L22) · [sesion.svelte.ts#L91 (logout)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/frontend/web/src/lib/sesion.svelte.ts#L91) · [CategoriasController.cs#L31 (sólo Administrador)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/CategoriasController.cs#L30-L31) · [ArticulosController.cs#L33 (sólo Administrador)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/ArticulosController.cs#L32-L33) · [PerfilController.cs#L27 (avatar)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/PerfilController.cs#L27) | [perfil.e2e.ts](frontend/web/e2e/perfil.e2e.ts) (sube el avatar y la barra lo muestra sin recargar) · [categorias.e2e.ts](frontend/web/e2e/categorias.e2e.ts) (login y logout) |
| 3. Manejo de archivos (portada de artículo) | Cumplido | [ArticulosController.cs#L62](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/ArticulosController.cs#L62) · [AlmacenDeArchivosLocal.cs#L15](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Archivos/AlmacenDeArchivosLocal.cs#L15) · [FirmasDeImagen.cs (valida la firma, no la extensión)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Archivos/FirmasDeImagen.cs) | [articulos.e2e.ts](frontend/web/e2e/articulos.e2e.ts) (publica un artículo con portada) |
| 4. ABM en SvelteKit + AJAX (categorías) | Cumplido | [CategoriasController.cs](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/CategoriasController.cs#L19-L106) · [admin/categorias/+page.svelte](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/frontend/web/src/routes/(app)/admin/categorias/+page.svelte) · [api/categorias.ts](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/frontend/web/src/lib/api/categorias.ts) | [categorias.e2e.ts](frontend/web/e2e/categorias.e2e.ts) (alta, edición, búsqueda, baja lógica y reactivación sin recargar) |
| 5. Paginado por servidor (catálogo de artículos) | Cumplido | [ArticulosController.cs#L111](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/ArticulosController.cs#L111) · [ArticuloRepositorio.cs#L80 (`LIMIT`/`OFFSET`)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Repositorios/ArticuloRepositorio.cs#L80) · [ParametrosDePagina.cs](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Paginacion/ParametrosDePagina.cs#L15) · [paginador.svelte](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/frontend/web/src/lib/components/paginador.svelte) | [articulos.e2e.ts](frontend/web/e2e/articulos.e2e.ts) (navega con `?pagina=`) |
| 6. Selección asíncrona (hábitos de una rutina) | Cumplido | [HabitosController.cs#L32 (`?q=` en el servidor)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Controllers/HabitosController.cs#L28-L40) · [buscador-de-habitos.svelte](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/frontend/web/src/lib/components/buscador-de-habitos.svelte) · [busqueda.ts](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/frontend/web/src/lib/busqueda.ts) | [busqueda.test.ts](frontend/web/src/lib/busqueda.test.ts) |
| 7. Web API + JWT | Cumplido | [Program.cs#L52 (`AddJwtBearer`)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Program.cs#L52) · [ServicioDeTokens.cs#L14](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/backend/Javito.Api/Seguridad/ServicioDeTokens.cs#L14) · [pedir.ts (manda el `Bearer`)](https://github.com/EmanuelAngel/javito/blob/1deda39438cc8cdbcc98aa5e60412f9d0cc9b822/frontend/web/src/lib/api/pedir.ts) | La SPA consume la API de forma nativa; para probarla a mano, Scalar en `http://localhost:5256/scalar/v1` |

La metadata del paginado viaja en español (`pagina`, `tamanioDePagina`, `totalDeRegistros`,
`totalDePaginas`): es la misma que la consigna nombra `pageNumber`, `pageSize`, `totalRecords` y
`totalPages`.

### Requisitos adicionales para promocionar

- [x] `.gitignore` específico y sin artefactos de compilación
- [x] Diagrama Entidad-Relación adjunto ([DER en Mermaid](docs/proyecto/propuesta.md#21-diagrama-entidad-relación-der))
- [x] `README.md` con descripción y guía paso a paso
- [x] `database.sql` con estructura y datos de prueba precargados
- [x] Credenciales de prueba por rol
- [x] Colección de Postman/Bruno: no aplica, la SPA consume la Web API de forma nativa
- [x] Todo el proyecto corriendo en `localhost` sin errores (`pnpm check`: build, 142 pruebas del backend y `svelte-check` en verde)

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
| Los 7 requerimientos mínimos de la consigna | Cumplido |

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
