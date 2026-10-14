import path from 'node:path';

/**
 * `dotnet format --include` acepta únicamente rutas relativas al directorio de trabajo.
 * Con una absoluta no formatea nada y sale con código 0, sin avisar. lint-staged pasa
 * rutas absolutas (`relative: false` es el default), así que hay que traducirlas.
 */
const rutasRelativas = (archivos) =>
	archivos
		.map((archivo) => `"${path.relative(process.cwd(), archivo).split(path.sep).join('/')}"`)
		.join(' ');

/** @type {import('lint-staged').Configuration} */
export default {
	// Espeja `pnpm lint:web`, que formatea con prettier todo el SPA (incluye css, html y md).
	'frontend/web/**/*.{ts,js,svelte,json,yml,yaml,css,html,md}':
		'pnpm --dir frontend/web exec prettier --write',
	// `--no-warn-ignored` silencia el aviso por `src/lib/componentes/ui/`: es código vendorizado
	// de shadcn-svelte y `eslint.config.js` lo ignora a propósito.
	'frontend/web/**/*.{ts,js,svelte}':
		'pnpm --dir frontend/web exec eslint --fix --no-warn-ignored',
	// Espeja `pnpm format:api`, acotado a los .cs que se están por commitear.
	'backend/**/*.cs': (archivos) =>
		`dotnet format backend/Javito.slnx --include ${rutasRelativas(archivos)}`
};
