import { expect, test } from '@playwright/test';
import { API_URL } from '../src/lib/config';

// USU-01, USU-02 y USU-03: el listado de usuarios del administrador, el alta de una cuenta y la
// edición de sus datos. Corre contra la WebAPI real y la base de desarrollo.
//
// El listado se prueba sobre las cuentas del seed y no crea ni borra nada. El alta sí crea una fila
// fija: queda en la base de desarrollo —la baja es lógica y es de la parte 2— hasta el próximo
// `pnpm db:reset` (L11). Por eso ninguna de las pruebas afirma sobre totales: se afirma sobre filas
// concretas y sobre lo que el filtro deja afuera.

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

// HU-USU-03: el nombre corregido de esa misma cuenta. Contiene al del alta, así la fila se sigue
// encontrando por el email y la prueba no depende del orden en que se corran los tests del archivo.
const NOMBRE_EDITADO = 'Pruebas e2e usuario editado';

/** El nombre con el que la edición deja la cuenta, para que la próxima corrida la encuentre igual. */
const NOMBRE_RESTAURADO = ALTA.nombre;

/** El nombre que no se tiene que guardar cuando el email ya es de otra cuenta. */
const NOMBRE_RECHAZADO = 'Pruebas e2e usuario rechazado';

/** Email de una cuenta del seed: es el «email de otro» que la edición tiene que rechazar. */
const EMAIL_DE_OTRA_CUENTA = 'usuario@javito.local';

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

test('la edición de los datos de un usuario', async ({ page }) => {
	// 1. Sesión de Administración: la edición es de administración, igual que el resto del módulo.
	await page.goto('/admin/usuarios');
	await expect(page).toHaveURL(/\/login\?destino=/);

	await page.getByLabel('Email').fill(ADMINISTRADOR.email);
	await page.getByLabel('Contraseña').fill(ADMINISTRADOR.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).toHaveURL(/\/admin\/usuarios$/);

	// 2. La cuenta que se edita es la del alta. Se busca por email y no por posición, y si el alta de
	//    este archivo todavía no corrió se crea por la API con el token de esta sesión: así la prueba
	//    no depende del orden de los tests. La baja es lógica y no hay DELETE, así que una cuenta que
	//    ya existe contesta 409 `email_repetido` y esa rama también es un éxito de este paso.
	const token = await page.evaluate(
		() => JSON.parse(localStorage.getItem('javito-sesion') ?? '{}').token as string
	);
	const asegurada = await page.request.post(`${API_URL}/api/usuarios`, {
		headers: { Authorization: `Bearer ${token}` },
		data: {
			nombre: NOMBRE_RESTAURADO,
			email: ALTA.email,
			password: ALTA.password,
			rol: 'Usuario'
		}
	});
	expect([201, 409]).toContain(asegurada.status());

	await page.goto('/admin/usuarios');
	await page.getByLabel('Buscar').fill(ALTA.email);
	await page.getByRole('button', { name: 'Buscar' }).click();
	await expect(page.getByRole('row', { name: new RegExp(ALTA.email) })).toBeVisible();

	// 3. La fila ofrece la edición y el formulario llega precargado, sin contraseña ni rol: esos datos
	//    no se editan en la parte 1 (S15) y la contraseña de otro usuario no se muestra (S11).
	await page
		.getByRole('row', { name: new RegExp(ALTA.email) })
		.getByRole('link', { name: 'Editar' })
		.click();

	await expect(page).toHaveURL(/\/admin\/usuarios\/\d+\/editar$/);
	await expect(page.getByRole('heading', { name: 'Editar usuario' })).toBeVisible();
	await expect(page.getByLabel('Nombre', { exact: true })).toHaveValue(NOMBRE_RESTAURADO);
	await expect(page.getByLabel('Email', { exact: true })).toHaveValue(ALTA.email);
	await expect(page.getByLabel('Contraseña', { exact: true })).toHaveCount(0);
	await expect(page.getByLabel('Rol', { exact: true })).toHaveCount(0);

	// 4. Corrige el nombre y guarda: vuelve al listado y la fila muestra el nombre nuevo.
	await page.getByLabel('Nombre', { exact: true }).fill(NOMBRE_EDITADO);
	await page.getByRole('button', { name: 'Guardar cambios' }).click();

	await expect(page).toHaveURL(/\/admin\/usuarios$/);
	await expect(
		page.getByRole('row', { name: new RegExp(ALTA.email) }).getByText(NOMBRE_EDITADO)
	).toBeVisible();

	// 5. Un email que ya es de otra cuenta no se guarda (S10): el 409 queda en el campo del email, con
	//    el nombre que se escribió intacto en el formulario y la fila con el nombre de antes.
	await page
		.getByRole('row', { name: new RegExp(ALTA.email) })
		.getByRole('link', { name: 'Editar' })
		.click();
	await page.getByLabel('Nombre', { exact: true }).fill(NOMBRE_RECHAZADO);
	await page.getByLabel('Email', { exact: true }).fill(EMAIL_DE_OTRA_CUENTA);
	await page.getByRole('button', { name: 'Guardar cambios' }).click();

	await expect(page.getByText('Ya existe una cuenta con el email')).toBeVisible();
	await expect(page.getByLabel('Email', { exact: true })).toHaveAttribute('aria-invalid', 'true');
	await expect(page).toHaveURL(/\/editar$/);

	// 6. La fila no cambió: al recargar la pantalla, el formulario trae lo guardado en el paso 4 y no
	//    lo que se intentó escribir. El rechazo no dejó rastro del nombre ni del email rechazados.
	await page.reload();

	await expect(page.getByLabel('Nombre', { exact: true })).toHaveValue(NOMBRE_EDITADO);
	await expect(page.getByLabel('Email', { exact: true })).toHaveValue(ALTA.email);

	// 7. La deja como estaba, con el nombre del alta, para que la próxima corrida encuentre el estado
	//    que espera. Es el mismo camino del paso 4.
	await page.getByLabel('Nombre', { exact: true }).fill(NOMBRE_RESTAURADO);
	await page.getByRole('button', { name: 'Guardar cambios' }).click();
	await expect(page).toHaveURL(/\/admin\/usuarios$/);

	await page.getByLabel('Buscar').fill(ALTA.email);
	await page.getByRole('button', { name: 'Buscar' }).click();

	const fila = page.getByRole('row', { name: new RegExp(ALTA.email) });

	await expect(fila.getByText(NOMBRE_RESTAURADO)).toBeVisible();
	await expect(fila.getByText(NOMBRE_RECHAZADO)).toHaveCount(0);
});
