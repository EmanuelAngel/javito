import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [
		sveltekit({
			compilerOptions: {
				// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
				runes: ({ filename }) =>
					filename.split(/[/\\]/).includes('node_modules') ? undefined : true
			},

			// SPA desacoplada: sin renderizado en servidor. adapter-static emite un único
			// index.html como fallback y el ruteo ocurre en el cliente, que consume la
			// WebAPI por HTTP. Ver docs/proyecto/propuesta.md §3.5.
			adapter: adapter({ fallback: 'index.html' })
		})
	]
});
