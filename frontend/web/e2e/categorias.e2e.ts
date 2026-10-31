import { expect, test, type Locator } from '@playwright/test';

// Primer recorrido de extremo a extremo: el camino crítico de la Puerta 1 del plan de cierre.
// Cubre el guard de rutas, el login con JWT, el ABM de categorías por AJAX (CAT-01…CAT-04) y la
// salida. Corre contra la WebAPI real y la base de desarrollo.
//
// La baja de una categoría es lógica y la API no tiene DELETE, así que la prueba crea una fila fija
// y la reusa si ya existe: es idempotente sin resetear la base. Queda una categoría inactiva por
// corrida, y `pnpm db:reset` la borra.

const NOMBRE = 'Pruebas e2e';

const ADMINISTRADOR = {
	email: 'admin@javito.local',
	password: 'Javito2026'
};

test('la Puerta 1 de punta a punta: guard, login y ABM de categorías', async ({ page }) => {
	// 1. Sin sesión, una ruta privada manda al login y recuerda a dónde ibas (?destino=).
	await page.goto('/admin/categorias');
	await expect(page).toHaveURL(/\/login\?destino=/);

	// 2. Con la cuenta de Administración vuelve a la ruta pedida.
	await page.getByLabel('Email').fill(ADMINISTRADOR.email);
	await page.getByLabel('Contraseña').fill(ADMINISTRADOR.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).toHaveURL(/\/admin\/categorias$/);
	await expect(page.getByRole('heading', { name: 'Categorías' })).toBeVisible();

	// 3. El shell muestra el enlace, porque la cuenta es Administrador.
	await expect(page.getByRole('link', { name: 'Categorías' })).toBeVisible();

	// 4. Alta por AJAX, sin recargar. Si la fila quedó de una corrida anterior, el 409 confirma que
	//    ya existe y la prueba sigue con esa misma fila.
	const fila = page.getByRole('row', { name: new RegExp(NOMBRE) });

	await page.getByLabel('Nombre', { exact: true }).fill(NOMBRE);
	await page.getByLabel('Descripción', { exact: true }).fill('Creada por el e2e');
	await page.getByRole('button', { name: 'Crear categoría' }).click();

	await expect(fila).toBeVisible();

	// La corrida anterior pudo dejarla desactivada: se normaliza para que la prueba sea repetible.
	await asegurarActiva(fila);

	// 5. Edición por AJAX.
	await fila.getByRole('button', { name: 'Editar' }).click();

	const descripcion = `Editada por el e2e ${Date.now()}`;
	await page.getByLabel('Descripción', { exact: true }).fill(descripcion);
	await page.getByRole('button', { name: 'Guardar cambios' }).click();

	await expect(fila.getByRole('cell', { name: descripcion })).toBeVisible();

	// 6. Búsqueda por nombre: filtra el listado sin tocar el servidor.
	await page.getByLabel('Buscar por nombre').fill(NOMBRE);
	await expect(fila).toBeVisible();
	await expect(page.getByRole('row', { name: /Cuerpo/ })).toHaveCount(0);
	await page.getByLabel('Buscar por nombre').fill('');

	// 7. Baja lógica: la fila queda y se marca inactiva.
	await asegurarActiva(fila);

	await fila.getByRole('button', { name: 'Desactivar' }).click();
	await expect(fila.getByText('Inactiva', { exact: true })).toBeVisible();

	// 8. Reactivación: vuelve a las activas.
	await fila.getByRole('button', { name: 'Reactivar' }).click();
	await expect(fila.getByText('Activa', { exact: true })).toBeVisible();

	// 9. Salir descarta la sesión, y la ruta privada vuelve a pedir login.
	await page.getByRole('button', { name: 'Salir' }).click();
	await expect(page).toHaveURL(/\/login$/);

	await page.goto('/admin/categorias');
	await expect(page).toHaveURL(/\/login\?destino=/);
});

/// Deja la fila en estado activo sin importar cómo la encontró: la baja es lógica, así que su
/// estado sobrevive de una corrida a la siguiente.
async function asegurarActiva(fila: Locator) {
	if (await fila.getByText('Inactiva', { exact: true }).isVisible()) {
		await fila.getByRole('button', { name: 'Reactivar' }).click();
	}

	await expect(fila.getByText('Activa', { exact: true })).toBeVisible();
}
