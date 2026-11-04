// SPA sin SSR: el catálogo se pide contra la WebAPI desde el navegador, con el token de la sesión.
//
// No hay `load` a propósito. El estado de la pantalla vive en la URL —`?pagina=` y `?categoriaId=`—
// y cada cambio vuelve a pedir el catálogo, así que la petición tiene que estar donde se puedan
// mostrar el `Spinner` y el `Alert` mientras viaja. Dentro de un `load`, SvelteKit retiene la página
// anterior y el estado de carga no tendría dónde verse.
export const ssr = false;
