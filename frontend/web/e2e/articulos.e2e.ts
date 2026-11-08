import { expect, test, type Locator, type Page } from '@playwright/test';
import { API_URL } from '../src/lib/config';

// ART-01 y ART-04 de punta a punta: el catálogo que lee cualquier usuario con sesión y el alta que
// sólo puede hacer Administración, recorridos como los haría una persona.
//
// Cubre S1 (alta con portada opcional; acá la portada viene y se ve en el catálogo), S5 (catálogo
// sólo con activos, del más nuevo al más viejo, con filtro por categoría) y la cara visible de la
// autorización: la nav esconde «Publicar» a un Usuario y el servidor igual contesta 403.
//
// Corre contra la base de desarrollo y la WebAPI real. La API no tiene DELETE, así que cada corrida
// deja un artículo publicado con el prefijo `Pruebas e2e` y su portada en
// `backend/Javito.Api/wwwroot/uploads/articulos/`: las dos cosas las borra `pnpm db:reset` (el
// archivo queda huérfano hasta que se limpie `wwwroot/uploads` a mano).
//
// El artículo nuevo se publica en una categoría que el seed no usa (Creatividad) y las tarjetas del
// seed se miran filtradas por Mente: así la prueba no depende de cuántos artículos dejaron las
// corridas anteriores, que se acumulan porque no hay baja.

const PREFIJO = 'Pruebas e2e';

/** Sufijo de esta corrida: distingue el artículo de los de corridas anteriores. */
const CORRIDA = Date.now();

const TITULO = `${PREFIJO} · artículo ${CORRIDA}`;
const RESUMEN = 'Publicado por el e2e para probar la vista previa y la portada.';
const CONTENIDO =
	'# Cómo se ve el Markdown\n\nEste párrafo tiene **una negrita** y sigue.\n\n- primero\n- segundo';

/** El título del intento que un Usuario no puede publicar. No se tiene que guardar. */
const TITULO_RECHAZADO = `${PREFIJO} · rechazado ${CORRIDA}`;

/** Activa en el seed y sin artículos: el filtro por esta categoría queda limpio. */
const CATEGORIA = 'Creatividad';

/** Categoría del seed con artículos: sirve para comprobar que el filtro deja afuera lo nuevo. */
const OTRA_CATEGORIA = 'Mente';

/** Un artículo activo del seed: sin portada y con cuerpo en Markdown que la tarjeta no muestra. */
const DEL_SEED = {
	titulo: 'Por qué no hay racha en Javito',
	resumen: 'La racha castiga el día que fallaste en lugar de premiar las semanas que sostuviste.',
	// Fragmento del `contenido_markdown`, no del resumen: si la tarjeta lo mostrara, la lista estaría
	// trayendo el cuerpo de cada artículo, que es justo lo que el paginado vino a evitar.
	cuerpo: 'Una racha **se corta**'
};

const ADMINISTRADOR = {
	email: 'admin@javito.local',
	password: 'Javito2026'
};

const USUARIO = {
	email: 'usuario@javito.local',
	password: 'Javito2026'
};

// PNG 1x1 real, el mismo del seed de pruebas de la API: el servidor valida la firma del archivo.
const PNG = Buffer.from(
	'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGPQqw4CAAHWAPz6Qz2AAAAAAElFTkSuQmCC',
	'base64'
);

test('artículos: catálogo, alta con portada y vista previa, y el 403 de un Usuario', async ({
	page
}) => {
	// 1. Sin sesión, el catálogo es una ruta privada: manda al login recordando a dónde ibas.
	await page.goto('/articulos');
	await expect(page).toHaveURL(/\/login\?destino=/);

	await entrar(page, ADMINISTRADOR);

	await expect(page).toHaveURL(/\/articulos$/);
	await expect(page.getByRole('heading', { name: 'Artículos', level: 1 })).toBeVisible();

	// Administración sí tiene la entrada al alta en la nav.
	await expect(page.getByRole('link', { name: 'Publicar' })).toBeVisible();

	// 2. Las tarjetas del seed, miradas por categoría: título, resumen, nombre de la categoría, la
	//    fecha como dd/mm/aaaa y ninguna portada (el seed las deja en NULL).
	await page.getByLabel('Categoría').selectOption({ label: OTRA_CATEGORIA });
	await expect(page).toHaveURL(/categoriaId=\d+/);

	const delSeed = tarjeta(page, DEL_SEED.titulo);

	await expect(delSeed).toBeVisible();
	await expect(delSeed).toContainText(DEL_SEED.resumen);
	await expect(delSeed).toContainText(OTRA_CATEGORIA);

	// La tarjeta no trae el cuerpo: ni el Markdown crudo ni su texto.
	await expect(delSeed).not.toContainText(DEL_SEED.cuerpo);
	await expect(delSeed).not.toContainText('**');

	await expect(delSeed.getByRole('img', { name: 'Sin portada' })).toBeVisible();

	// La fecha se muestra dd/mm/aaaa y es la misma que viaja en el atributo `datetime` (aaaa-mm-dd):
	// el corte lo hace la tarjeta, no la zona horaria del navegador.
	const fechaDelSeed = delSeed.locator('time');
	const enLaApi = (await fechaDelSeed.getAttribute('datetime')) ?? '';

	expect(enLaApi).toMatch(/^\d{4}-\d{2}-\d{2}$/);
	await expect(fechaDelSeed).toHaveText(/^\d{2}\/\d{2}\/\d{4}$/);
	await expect(fechaDelSeed).toHaveText(enFormatoDeTarjeta(enLaApi));

	// 3. El alta, por donde va una persona: el enlace de la nav.
	await page.getByRole('link', { name: 'Publicar' }).click();
	await expect(page).toHaveURL(/\/articulos\/nuevo$/);
	await expect(page.getByRole('heading', { name: 'Publicar artículo', level: 1 })).toBeVisible();

	await page.getByLabel('Título', { exact: true }).fill(TITULO);
	await page.getByLabel('Resumen', { exact: true }).fill(RESUMEN);
	await page.getByLabel('Contenido (Markdown)').fill(CONTENIDO);
	await page.getByLabel('Categoría').selectOption({ label: CATEGORIA });

	// La portada viaja como archivo y el servidor la acepta por su firma, no por el nombre.
	await page.getByLabel('Portada (opcional)').setInputFiles({
		name: 'portada.png',
		mimeType: 'image/png',
		buffer: PNG
	});

	// 4. La vista previa renderiza el Markdown antes de publicar: el encabezado es un h1 y la
	//    negrita es un <strong>, no el Markdown crudo.
	const vistaPrevia = page
		.locator('section')
		.filter({ has: page.getByRole('heading', { name: 'Vista previa' }) });

	await expect(
		vistaPrevia.getByRole('heading', { level: 1, name: 'Cómo se ve el Markdown' })
	).toBeVisible();
	await expect(vistaPrevia.locator('strong')).toHaveText('una negrita');
	await expect(vistaPrevia).not.toContainText('# Cómo');

	// Todavía no se publicó: la previa es de la pantalla, no del artículo guardado.
	await expect(page).toHaveURL(/\/articulos\/nuevo$/);

	// 5. Publicar: el aviso confirma el alta y el artículo nace activo y con fecha de la base.
	await page.getByRole('button', { name: 'Publicar artículo' }).click();
	await expect(page.getByRole('status')).toContainText(`Publicado «${TITULO}»`);

	await page.getByRole('link', { name: 'Ver en el catálogo' }).click();

	// 6. En el catálogo el artículo nuevo es el primero: el orden es del más nuevo al más viejo. Si
	//    aparece acá es porque nació activo: el catálogo no lista los inactivos.
	await expect(page).toHaveURL(/\/articulos$/);

	const primero = page.locator('main ul > li').first();

	await expect(primero).toContainText(TITULO);
	await expect(primero).toContainText(CATEGORIA);

	// La fecha es la del día en que se publicó, en el formato de la pantalla.
	const hoyEnLaApi = enFormatoDeLaApi(new Date());

	await expect(primero.locator('time')).toHaveAttribute('datetime', hoyEnLaApi);
	await expect(primero.locator('time')).toHaveText(enFormatoDeTarjeta(hoyEnLaApi));

	// Y la portada que se subió se sirve desde el almacén de la WebAPI.
	const portada = primero.getByRole('img', { name: `Portada de ${TITULO}` });

	await expect(portada).toBeVisible();
	await expect(portada).toHaveAttribute(
		'src',
		/^http:\/\/localhost:5256\/uploads\/articulos\/.+\.png$/
	);

	// `naturalWidth` distingue la imagen que cargó de la que quedó rota: la ruta relativa de la fila
	// resuelve contra la API y el archivo está en disco.
	expect(await portada.evaluate((imagen) => (imagen as HTMLImageElement).naturalWidth)).toBe(1);

	// 7. El filtro por categoría es del servidor: en otra categoría el artículo no está…
	await page.getByLabel('Categoría').selectOption({ label: OTRA_CATEGORIA });
	await expect(tarjeta(page, TITULO)).toHaveCount(0);

	// …y en la suya vuelve a estar primero, que es el orden de la historia.
	await page.getByLabel('Categoría').selectOption({ label: CATEGORIA });
	await expect(tarjeta(page, TITULO)).toBeVisible();
	await expect(page.locator('main ul > li').first()).toContainText(TITULO);

	// 8. `?pagina=` en la URL del build estático: una página pasada del final no se corrige en
	//    silencio, la pantalla lo dice y ofrece volver a la última que existe.
	await page.goto('/articulos?pagina=99');
	await expect(page.getByText('Esta página no existe')).toBeVisible();

	await page.getByRole('button', { name: 'Ir a la última página' }).click();
	await expect(page).not.toHaveURL(/pagina=99/);
	await expect(page.getByText('Esta página no existe')).toHaveCount(0);

	// 9. Un Usuario común no recibe la entrada al alta: la nav sólo le ofrece el catálogo.
	await page.getByRole('button', { name: 'Salir' }).click();
	await expect(page).toHaveURL(/\/login$/);

	await entrar(page, USUARIO);

	await expect(page.getByRole('link', { name: 'Artículos' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Publicar' })).toHaveCount(0);

	// 10. Escribir la URL a mano tampoco lo deja en el alta: el guard de rol (#67) lo manda a /hoy.
	await page.goto('/articulos/nuevo');
	await expect(page).toHaveURL(/\/hoy$/);
	await expect(page.getByRole('heading', { name: 'Publicar artículo', level: 1 })).toHaveCount(0);

	// …pero el guard es comodidad, no la defensa: el mismo alta mandada directo a la API, con el
	// token del Usuario, recibe 403. El pedido sale del e2e porque el formulario ya no se abre.
	const token = await page.evaluate(
		() => JSON.parse(localStorage.getItem('javito-sesion') ?? '{}').token as string
	);
	const respuesta = await page.request.post(`${API_URL}/api/articulos`, {
		headers: { Authorization: `Bearer ${token}` },
		multipart: { titulo: TITULO_RECHAZADO, resumen: RESUMEN, contenido: CONTENIDO }
	});
	expect(respuesta.status()).toBe(403);
	expect((await respuesta.json()).codigo).toBe('sin_permiso');

	// El rechazo no dejó fila: el título rechazado no aparece en el catálogo, ni siquiera en su
	// categoría. (Si el servidor hubiera guardado antes de autorizar, acá estaría.)
	await page.goto('/articulos');
	await page.getByLabel('Categoría').selectOption({ label: CATEGORIA });
	await expect(tarjeta(page, TITULO_RECHAZADO)).toHaveCount(0);
});

/** Entra por el formulario de login y espera a salir de /login. */
async function entrar(page: Page, cuenta: { email: string; password: string }) {
	await page.getByLabel('Email').fill(cuenta.email);
	await page.getByLabel('Contraseña').fill(cuenta.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).not.toHaveURL(/\/login/);
}

/** La tarjeta de un artículo, buscada por su título dentro de la grilla del catálogo. */
function tarjeta(page: Page, titulo: string): Locator {
	return page.locator('main ul > li').filter({ hasText: titulo });
}

/** dd/mm/aaaa, el formato de la tarjeta, a partir del aaaa-mm-dd del contrato. */
function enFormatoDeTarjeta(fecha: string): string {
	return fecha.split('-').reverse().join('/');
}

/** aaaa-mm-dd del día indicado, que es el formato en el que la API manda la fecha. */
function enFormatoDeLaApi(fecha: Date): string {
	const mes = String(fecha.getMonth() + 1).padStart(2, '0');
	const dia = String(fecha.getDate()).padStart(2, '0');

	return `${fecha.getFullYear()}-${mes}-${dia}`;
}
