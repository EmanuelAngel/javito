import { expect, test } from '@playwright/test';

// USU-01: el listado de usuarios del administrador. Corre contra la WebAPI real y la base de
// desarrollo, sobre las cuentas del seed: no crea ni borra usuarios, así que es idempotente.
//
// Las cuentas que cree el e2e del alta (#85) quedan en la base de desarrollo —la baja es lógica y es
// de la parte 2—, por eso acá no se afirma sobre totales: se afirma sobre filas concretas y sobre lo
// que el filtro deja afuera.

const ADMINISTRADOR = {
	email: 'admin@javito.local',
	password: 'Javito2026'
};

const USUARIO = {
	email: 'usuario@javito.local',
	password: 'Javito2026'
};

test('el listado de usuarios: búsqueda, estado y guard de rol', async ({ page }) => {
	// 1. Sin sesión, la ruta privada manda al login y recuerda a dónde ibas.
	await page.goto('/admin/usuarios');
	await expect(page).toHaveURL(/\/login\?destino=/);

	// 2. Con la cuenta de Administración vuelve a la ruta pedida.
	await page.getByLabel('Email').fill(ADMINISTRADOR.email);
	await page.getByLabel('Contraseña').fill(ADMINISTRADOR.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).toHaveURL(/\/admin\/usuarios$/);
	await expect(page.getByRole('heading', { name: 'Usuarios' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Usuarios' })).toBeVisible();

	// 3. La primera página trae las cuentas del seed con nombre, email, rol y estado.
	const administracion = page.getByRole('row', { name: /admin@javito\.local/ });
	const desactivada = page.getByRole('row', { name: /desactivado@javito\.local/ });

	await expect(administracion).toBeVisible();
	await expect(administracion.getByText('Administrador')).toBeVisible();
	await expect(administracion.getByText('Activo')).toBeVisible();
	await expect(desactivada.getByText('Inactivo')).toBeVisible();

	// 4. Búsqueda por email: la fila queda sola.
	await page.getByLabel('Buscar').fill('Otra cuenta');
	await page.getByRole('button', { name: 'Buscar' }).click();

	await expect(page).toHaveURL(/q=Otra(\+|%20)cuenta/);
	await expect(page.getByRole('row', { name: /otra@javito\.local/ })).toBeVisible();
	await expect(administracion).toBeHidden();

	// 5. Filtro de estado: desactivados, sólo la cuenta inactiva; activos, sin ella.
	await page.getByPlaceholder('Nombre o email').fill('');
	await page.getByRole('button', { name: 'Buscar' }).click();
	await page.getByRole('button', { name: 'Desactivados' }).click();

	await expect(page).toHaveURL(/activo=false/);
	await expect(page.getByRole('button', { name: 'Desactivados' })).toHaveAttribute(
		'aria-pressed',
		'true'
	);
	await expect(desactivada).toBeVisible();
	await expect(administracion).toBeHidden();

	await page.getByRole('button', { name: 'Activos' }).click();

	await expect(page).toHaveURL(/activo=true/);
	await expect(administracion).toBeVisible();
	await expect(desactivada).toBeHidden();

	// 6. Una página que no existe se dice, en vez de mentir con «no hay cuentas».
	await page.goto('/admin/usuarios?pagina=99');

	await expect(page.getByText('Esta página no existe')).toBeVisible();

	// 7. Un Usuario no ve el enlace ni entra por URL: el guard lo devuelve a /hoy.
	await page.getByRole('button', { name: 'Salir' }).click();
	await expect(page).toHaveURL(/\/login$/);

	await page.getByLabel('Email').fill(USUARIO.email);
	await page.getByLabel('Contraseña').fill(USUARIO.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).toHaveURL(/\/hoy$/);
	await expect(page.getByRole('link', { name: 'Usuarios' })).toBeHidden();

	await page.goto('/admin/usuarios');

	await expect(page).toHaveURL(/\/hoy$/);
});
