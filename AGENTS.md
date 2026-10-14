# Javito — instrucciones para agentes

Monorepo de una sola entrega: WebAPI **ASP.NET Core (.NET 10)** en `backend/` + SPA **SvelteKit** en
`frontend/web/`, con **MySQL 8.0** y **JWT**. Un solo repositorio Git, en la raíz. Todo corre en
`localhost`; no hay despliegue.

Alcance completo, cronograma y consignas: `docs/`. Los documentos de trabajo en curso: `odd/tasks/`.

## Mapa

| Ruta | Qué es |
| :--- | :--- |
| `backend/Javito.slnx` | solución .NET |
| `backend/Javito.Api/` | WebAPI: `Program.cs`, `Controllers/` |
| `backend/Javito.Api.Tests/` | xUnit + `Microsoft.AspNetCore.Mvc.Testing` |
| `backend/scripts/` | `check.sh`, `format.sh`, `lint.sh` |
| `frontend/web/` | SPA SvelteKit con `adapter-static` |
| `database.sql` | estructura + datos semilla (S2, pendiente) |
| `docs/` | consignas de cátedra, propuesta, cronograma, brief de UX |
| `odd/tasks/` | documentos de feature del trabajo trackeado |

## Comandos

```sh
# Backend
./backend/scripts/check.sh                                  # dotnet build + dotnet test
./backend/scripts/format.sh                                 # aplica formato
./backend/scripts/lint.sh                                   # verifica formato sin escribir
dotnet run --project backend/Javito.Api --launch-profile http

# Frontend
pnpm --dir frontend/web dev                                 # http://localhost:5173
pnpm --dir frontend/web check                               # svelte-check
pnpm --dir frontend/web build
```

Las pruebas del backend usan `dotnet test`; el frontend no tiene runner todavía.

## Puertos y vínculo entre capas

- WebAPI: `http://localhost:5256` (perfil `http` de `launchSettings.json`).
- SPA: `http://localhost:5173`.
- El backend permite exactamente ese origen en `Cors:AllowedOrigins`
  (`backend/Javito.Api/appsettings.Development.json`). La política es fail-closed: sin orígenes
  configurados no habilita ninguno.
- La URL de la API vive en un único lugar: `frontend/web/src/lib/config.ts`.
- Endpoint de humo: `GET /api/health`.

## Convenciones

- **Commits**: Conventional Commits **en español**, verbo en infinitivo, minúsculas, sin punto final
  (`feat(api): agregar repositorio de hábitos`). Lo verifica `commitlint` en el hook `commit-msg`.
- **Idioma**: español para nombres de prueba, comentarios y el contrato JSON de la API (`estado`,
  `servicio`), coherente con el dominio en español del DER; inglés para identificadores que siguen
  convenciones del framework (`HealthController`, `Program`).
- **Pruebas**: cada regla nueva llega con su prueba, y se observa RED antes del fix. Una prueba por
  regla, verificando todos sus efectos observables.
- **Unidades de trabajo**: cada commit es una unidad revisable y lleva el código junto a sus pruebas
  y su documentación.
- **Sin ORM**: acceso a datos con ADO.NET puro (`MySqlConnector`), SQL a mano.

## Trampas conocidas de SvelteKit 3

Verificado a golpes en este repo; no está en la doc evidente.

- **`$lib` no existe.** El error es `module_removed_lib`. El alias es `#lib`, declarado como subpath
  imports en `frontend/web/package.json`.
- **El patrón `#lib/*` no resuelve en TypeScript** si el especificador no lleva extensión.
  `#lib/assets/favicon.svg` funciona porque la tiene; para un módulo `.ts`, usá import relativo.
- **`$env/*` fue reemplazado** por `src/env.ts` + `defineEnvVars`, expuesto desde `$app/env/public`.
  `$env/dynamic/public` sobrevive como shim sin tipos y con advertencia de deprecación. Hoy la
  configuración vive en `src/lib/config.ts`.
- **La config del adapter está en `vite.config.ts`**, no en `svelte.config.js`.
- **`pnpm build` verde no implica `pnpm check` verde.** Corré los dos.

## Alcance y límite

No es offline ni app móvil nativa · sin integración con calendarios externos · sin notificaciones
push ni temporizadores. El módulo de artículos es un agregado para cubrir los requisitos de cátedra
de manejo de archivos y funciones por rol.
