import { expect, test } from '@playwright/test';

// RUT-04: filtrar el día por rutina. Cubre que la rutina muestra sólo sus hábitos y en su orden
// (S13), que se vuelve a ver todos (S14), que marcar dentro del filtro marca el hábito real (S15) y
// que el selector ofrece sólo las rutinas activas (S10).
//
// Corre contra la base de desarrollo con las rutinas del seed. Marca «Respiración cuadrada» y la
// desmarca al final, así que el registro de hoy queda como estaba.

const MANANA = [
	'Hidratación con agua',
	'Liberación de cuello y hombros',
	'Respiración cuadrada',
	'Meditar'
];

test('filtrar el día por rutina', async ({ page }) => {
	await page.goto('/hoy');
	await page.getByLabel('Email').fill('usuario@javito.local');
	await page.getByLabel('Contraseña').fill('Javito2026');
	await page.getByRole('button', { name: 'Entrar' }).click();
	await expect(page).toHaveURL(/\/hoy$/);

	const selector = page.getByLabel('Rutina');
	const filas = page.getByRole('listitem');

	// S10: la rutina desactivada del seed no se ofrece.
	await expect(selector.getByRole('option')).toHaveText([
		'Todos los hábitos',
		'Mañana',
		'Corte de escritorio',
		'Noche'
	]);
	const todas = await filas.count();

	// S13: sólo los hábitos de la rutina, en el orden de la rutina.
	await selector.selectOption({ label: 'Mañana' });
	await expect(page).toHaveURL(/\/hoy\?rutinaId=1$/);
	await expect(filas).toHaveCount(MANANA.length);
	for (const [indice, titulo] of MANANA.entries()) {
		await expect(filas.nth(indice)).toContainText(titulo);
	}

	// S15: marcar dentro del filtro marca el hábito real, que se ve marcado sin el filtro. Es un
	// hábito sí/no: su fila tiene un solo botón, el que alterna.
	const marcar = (titulo: string) => filas.filter({ hasText: titulo }).getByRole('button');
	// El toque se pinta al instante y el pedido sigue en vuelo: hay que esperarlo antes de recargar,
	// o la recarga lo corta.
	const guardado = () =>
		page.waitForResponse((r) => r.url().includes('/api/') && r.request().method() !== 'GET');
	const respiracion = marcar('Respiración cuadrada');
	const antes = await respiracion.getAttribute('aria-pressed');
	await Promise.all([guardado(), respiracion.click()]);
	await expect(respiracion).not.toHaveAttribute('aria-pressed', antes ?? '');

	// S14: volver a ver todos.
	await selector.selectOption({ label: 'Todos los hábitos' });
	await expect(page).toHaveURL(/\/hoy$/);
	await expect(filas).toHaveCount(todas);
	await expect(marcar('Respiración cuadrada')).not.toHaveAttribute('aria-pressed', antes ?? '');

	// El estado sobrevive a recargar: lo guardó el servidor, no la vista.
	await page.reload();
	await expect(marcar('Respiración cuadrada')).not.toHaveAttribute('aria-pressed', antes ?? '');

	// Deja el registro de hoy como estaba.
	await Promise.all([guardado(), marcar('Respiración cuadrada').click()]);
	await expect(marcar('Respiración cuadrada')).toHaveAttribute('aria-pressed', antes ?? '');

	// Un id que no es de una rutina activa propia muestra todos: la desactivada (4) y la ajena (5).
	for (const id of [4, 5]) {
		await page.goto(`/hoy?rutinaId=${id}`);
		await expect(filas).toHaveCount(todas);
		await expect(selector).toHaveValue('');
	}
});
