// SPA pura: el cliente renderiza todo y el estado viene de la WebAPI.
// Sin esto el build intentaría prerenderizar rutas que dependen de datos del backend.
export const ssr = false;
export const prerender = false;
