import { expect, test } from '@playwright/test';

// USU-01 y USU-02: el listado de usuarios del administrador y el alta de una cuenta. Corre contra
// la WebAPI real y la base de desarrollo.
//
// El listado se prueba sobre las cuentas del seed y no crea ni borra nada. El alta sí crea una fila
// fija: queda en la base de desarrollo —la baja es lógica y es de la parte 2— hasta el próximo
// `pnpm db:reset` (L11). Por eso ninguna de las dos pruebas afirma sobre totales: se afirma sobre
// filas concretas y sobre lo que el filtro deja afuera.

const ADMINISTRADOR = {
	email: 'admin@javito.local',
	password: 'Javito2026'
};

const USUARIO = {
	email: 'usuario@javito.local',
	password: 'Javito2026'
};

// La cuenta del alta es fija para que la prueba sea idempotente: la baja es lógica y no hay DELETE,
// así que si ya existe de una corrida anterior el alta devuelve 409 `email_repetido` y se acepta esa
// rama. La fila queda en la base de DESARROLLO hasta el próximo `pnpm db:reset` (L11).
const ALTA = {
	nombre: 'Pruebas e2e usuario',
	email: 'pruebas-e2e-usuario@javito.test',
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

test('el alta de un usuario desde administración', async ({ page }) => {
	// 1. Con la cuenta de Administración, el listado ofrece el alta y la ruta existe.
	await page.goto('/admin/usuarios');
	await expect(page).toHaveURL(/\/login\?destino=/);

	await page.getByLabel('Email').fill(ADMINISTRADOR.email);
	await page.getByLabel('Contraseña').fill(ADMINISTRADOR.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).toHaveURL(/\/admin\/usuarios$/);

	await page.getByRole('link', { name: 'Nuevo usuario' }).click();
	await expect(page).toHaveURL(/\/admin\/usuarios\/nuevo$/);
	await expect(page.getByRole('heading', { name: 'Nuevo usuario' })).toBeVisible();

	// 2. El formulario pide nombre, email, contraseña y rol: la contraseña la elige el administrador y
	//    la cuenta nace con el rol elegido (S6 y S7).
	await page.getByLabel('Nombre', { exact: true }).fill(ALTA.nombre);
	await page.getByLabel('Email', { exact: true }).fill(ALTA.email);
	await page.getByLabel('Contraseña', { exact: true }).fill(ALTA.password);
	await page.getByLabel('Rol', { exact: true }).selectOption('Usuario');

	await page.getByRole('button', { name: 'Crear usuario' }).click();

	// Dos finales posibles y los dos son un éxito de la prueba: el alta nueva vuelve al listado, y la
	// repetida deja el 409 en el campo del email, que es lo que se quiere ver.
	const repetido = page.getByText('Ya existe una cuenta con el email');
	const listado = page.getByRole('heading', { name: 'Usuarios' });

	await expect(repetido.or(listado)).toBeVisible();

	if (await repetido.isVisible()) {
		await expect(page.getByLabel('Email', { exact: true })).toHaveAttribute('aria-invalid', 'true');

		await page.getByRole('link', { name: 'Volver al listado' }).click();
		await expect(page).toHaveURL(/\/admin\/usuarios$/);
	}

	// 3. La cuenta está en el listado, con el rol con el que se creó y activa.
	await page.getByLabel('Buscar').fill(ALTA.email);
	await page.getByRole('button', { name: 'Buscar' }).click();

	const fila = page.getByRole('row', { name: new RegExp(ALTA.email) });

	await expect(fila).toBeVisible();
	await expect(fila.getByText(ALTA.nombre)).toBeVisible();
	await expect(fila.getByText('Activo', { exact: true })).toBeVisible();
});
