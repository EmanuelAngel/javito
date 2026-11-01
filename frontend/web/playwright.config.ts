import { defineConfig } from '@playwright/test';

// El SPA se sirve en el mismo origen que la WebAPI autoriza, sin importar si el servidor es
// `vite dev` o `vite preview`. El puerto por defecto de `vite preview` es 4173, y desde ese
// origen la WebAPI responde sin cabeceras CORS porque la política es fail-closed: sólo permite
// `http://localhost:5173` (ver `Cors:AllowedOrigins` en
// `backend/Javito.Api/appsettings.Development.json`). Con 4173, toda prueba de extremo a extremo
// contra la API real fallaría por CORS y no por la aplicación.
const puertoSpa = 5173;

const puertoApi = 5256;

export default defineConfig({
	testMatch: '**/*.e2e.{ts,js}',
	use: {
		baseURL: `http://localhost:${puertoSpa}`,
		// Edge del sistema en vez del Chromium que descarga Playwright: es el mismo navegador que usa el
		// project `client` de vitest, y así `pnpm test:e2e` no necesita `test:e2e:install`.
		channel: 'msedge'
	},
	webServer: [
		{
			// La API se levanta desde la raíz del monorepo con `pnpm dev:api`, que usa el perfil `http`
			// de launchSettings.json y toma la cadena de conexión y la clave de `dotnet user-secrets`.
			// Corolario: no puede haber otro `Javito.Api` corriendo, porque el proceso bloquea la DLL y
			// `dotnet run` falla con MSB3021/MSB3027.
			command: 'pnpm dev:api',
			cwd: '../..',
			url: `http://localhost:${puertoApi}/api/health`,
			reuseExistingServer: !process.env.CI,
			timeout: 120_000
		},
		{
			// `pnpm` y no `npm`: el proyecto usa pnpm y su lockfile es `pnpm-lock.yaml`.
			command: `pnpm build && pnpm preview --port ${puertoSpa}`,
			port: puertoSpa,
			reuseExistingServer: !process.env.CI
		}
	]
});
