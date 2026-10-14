import { defineConfig } from '@playwright/test';

// El SPA se sirve en el mismo origen que la WebAPI autoriza, sin importar si el servidor es
// `vite dev` o `vite preview`. El puerto por defecto de `vite preview` es 4173, y desde ese
// origen la WebAPI responde sin cabeceras CORS porque la política es fail-closed: sólo permite
// `http://localhost:5173` (ver `Cors:AllowedOrigins` en
// `backend/Javito.Api/appsettings.Development.json`). Con 4173, toda prueba de extremo a extremo
// contra la API real fallaría por CORS y no por la aplicación.
const puertoSpa = 5173;

export default defineConfig({
	testMatch: '**/*.e2e.{ts,js}',
	use: {
		baseURL: `http://localhost:${puertoSpa}`
	},
	webServer: {
		// `pnpm` y no `npm`: el proyecto usa pnpm y su lockfile es `pnpm-lock.yaml`.
		command: `pnpm build && pnpm preview --port ${puertoSpa}`,
		port: puertoSpa,
		reuseExistingServer: !process.env.CI
	}
});
