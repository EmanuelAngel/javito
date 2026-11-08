import { expect, test, type Page } from '@playwright/test';

// HIS-01 y HIS-02: el recorrido del historial. Cubre las cinco columnas de la fila (S4), que la
// paginación la resuelve el servidor (S1), el filtro por hábito (S2) y que sólo aparece lo propio
// (S3); y, desde el detalle de un hábito, la cuadrícula de 30 y 365 días (S6) y el atajo para
// editarlo (S9).
//
// Corre contra la base de desarrollo y no escribe nada: el historial es de sólo lectura, así que el
// seed del usuario de prueba alcanza y la prueba puede repetirse sin resetear.

const USUARIO = {
	email: 'usuario@javito.local',
	password: 'Javito2026'
};

async function entrar(page: Page): Promise<void> {
	await page.getByLabel('Email').fill(USUARIO.email);
	await page.getByLabel('Contraseña').fill(USUARIO.password);
	await page.getByRole('button', { name: 'Entrar' }).click();
}

test('el historial: paginado, filtrable y sólo mío', async ({ page }) => {
	// 1. Sin sesión, la ruta privada manda al login y recuerda el destino.
	await page.goto('/historial');
	await expect(page).toHaveURL(/\/login\?destino=/);

	await entrar(page);

	await expect(page).toHaveURL(/\/historial$/);
	await expect(page.getByRole('heading', { name: 'Historial' })).toBeVisible();

	// 2. S4: cada fila lleva fecha, hábito, valor alcanzado, meta de ese día y si se completó.
	for (const columna of ['Fecha', 'Hábito', 'Valor', 'Meta', 'Estado']) {
		await expect(page.getByRole('columnheader', { name: columna })).toBeVisible();
	}

	// 3. S1: el servidor pagina. El seed tiene más registros que una página, y la tabla trae seis.
	const filas = page.locator('tbody tr');
	await expect(filas).toHaveCount(6);

	const paginador = page.getByRole('navigation', { name: 'Paginación del historial' });
	await expect(paginador).toContainText('Página 1 de');
	await paginador.getByRole('button', { name: 'Siguiente' }).click();
	await expect(page).toHaveURL(/pagina=2/);

	// 4. S2 y S3: el filtro pide al servidor los registros de un hábito propio y vuelve a la página 1.
	await page.getByLabel('Hábito').selectOption({ label: 'Leer' });
	await expect(page).toHaveURL(/habitoId=/);
	await expect(page).not.toHaveURL(/pagina=2/);

	const habitosEnLaTabla = await page.locator('tbody td:nth-child(2)').allTextContents();
	expect(habitosEnLaTabla.length).toBeGreaterThan(0);
	expect(new Set(habitosEnLaTabla)).toEqual(new Set(['Leer']));

	// El filtro ofrece sólo los hábitos propios: los del seed de otras cuentas no están.
	const opciones = await page.getByLabel('Hábito').locator('option').allTextContents();
	expect(opciones).not.toContain('Tender la cama');
});

test('el detalle de un hábito: la cuadrícula, el período y el atajo a editar', async ({ page }) => {
	// 1. El hábito de la primera fila del historial es el enlace al detalle (S6).
	await page.goto('/historial');
	await expect(page).toHaveURL(/\/login\?destino=/);

	await entrar(page);
	await expect(page).toHaveURL(/\/historial$/);

	const primeraFila = page.locator('tbody tr').first();
	const habito = (await primeraFila.locator('td:nth-child(2)').innerText()).trim();

	await primeraFila.getByRole('link', { name: habito }).click();
	await expect(page).toHaveURL(/\/historial\/\d+$/);

	const id = page.url().split('/').pop();
	const cuadricula = page.getByRole('group', { name: 'Los días del período' });

	await expect(page.getByRole('heading', { name: habito })).toBeVisible();

	// 2. S6: sin `?dias=`, el período es el mes, y la cuadrícula tiene una celda por día.
	await expect(cuadricula.getByRole('img')).toHaveCount(30);

	// 3. S6: el período vive en la URL y redibuja la cuadrícula entera. Un enlace copiado abre el
	//    mismo período, así que la dirección escrita es la prueba, no sólo el cambio de celdas.
	await page.getByRole('button', { name: '365 días' }).click();
	await expect(page).toHaveURL(new RegExp(`/historial/${id}\\?dias=365$`));
	await expect(cuadricula.getByRole('img')).toHaveCount(365);

	// 4. S9: del detalle se sale a editar, con el formulario ya cargado en modo edición.
	await page.getByRole('link', { name: 'Editar hábito' }).click();
	await expect(page).toHaveURL(new RegExp(`/habitos\\?editar=${id}$`));
	await expect(page.getByRole('heading', { name: 'Editar hábito' })).toBeVisible();
	await expect(page.getByLabel('Título', { exact: true })).toHaveValue(habito);
});
