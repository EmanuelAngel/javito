import { expect, test } from '@playwright/test';

// PER-02: el avatar de punta a punta. Cubre la subida desde /perfil y la regla que más se rompe:
// el avatar nuevo aparece en la barra **sin recargar la página**.
//
// Corre contra la base de desarrollo y usa la cuenta menos usada del seed. Cada corrida reemplaza el
// avatar de esa cuenta (la regla de la historia), así que la prueba puede repetirse sin resetear
// nada. `pnpm db:reset` devuelve la columna a NULL y el archivo queda huérfano en wwwroot/uploads.

const CUENTA = {
	email: 'otra@javito.local',
	password: 'Javito2026',
	nombre: 'Otra cuenta'
};

// PNG 1x1 real, el mismo del seed de pruebas de la API: el servidor valida la firma del archivo.
const PNG = Buffer.from(
	'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGPQqw4CAAHWAPz6Qz2AAAAAAElFTkSuQmCC',
	'base64'
);

test('el avatar sube desde el perfil y aparece en la barra sin recargar', async ({ page }) => {
	// 1. Sin sesión, el perfil es una ruta privada.
	await page.goto('/perfil');
	await expect(page).toHaveURL(/\/login\?destino=/);

	await page.getByLabel('Email').fill(CUENTA.email);
	await page.getByLabel('Contraseña').fill(CUENTA.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).toHaveURL(/\/perfil$/);
	await expect(page.getByRole('heading', { name: 'Perfil' })).toBeVisible();

	// 2. La barra superior ofrece el avatar como acceso al perfil.
	await expect(page.getByRole('link', { name: 'Mi perfil' })).toBeVisible();

	// Marca en la ventana: si la subida recargara la página, desaparecería.
	await page.evaluate(() => {
		(window as Window & { sinRecarga?: boolean }).sinRecarga = true;
	});

	// 3. La imagen viaja como multipart y la API la acepta.
	await page.getByLabel('Cambiar la foto').setInputFiles({
		name: 'avatar.png',
		mimeType: 'image/png',
		buffer: PNG
	});

	await expect(page.getByRole('status')).toContainText('Tu avatar ya está actualizado.');

	// 4. El avatar aparece arriba de todo, y la página no se recargó.
	const enLaBarra = page.getByRole('link', { name: 'Mi perfil' }).getByRole('img');
	await expect(enLaBarra).toHaveAttribute('alt', `Avatar de ${CUENTA.nombre}`);
	await expect(enLaBarra).toHaveAttribute(
		'src',
		/^http:\/\/localhost:5256\/uploads\/avatares\/.+\.png$/
	);
	await expect(enLaBarra).toBeVisible();

	expect(await page.evaluate(() => (window as Window & { sinRecarga?: boolean }).sinRecarga)).toBe(
		true
	);

	// 5. Recargando, el avatar sigue ahí: quedó en la fila de la cuenta y no sólo en el estado del SPA.
	await page.reload();
	await expect(page.getByRole('link', { name: 'Mi perfil' }).getByRole('img')).toHaveAttribute(
		'alt',
		`Avatar de ${CUENTA.nombre}`
	);
});
