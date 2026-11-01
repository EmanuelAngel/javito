import { expect, test } from '@playwright/test';

// S10: el recorrido de mis hábitos y el día, el que cierra la Puerta 2 del plan.
//
// Cubre el alta por la UI (HAB-01), el listado (HAB-02) y marcar el día sin recargar (REG-01), con el
// contador subiendo y bajando de la meta. El hábito es propio y descartable a propósito: el seed del
// usuario de prueba guarda el histórico que cuenta la semana de la cátedra («Ejercicio pesado» va 2
// de 3) y este recorrido no lo toca.
//
// Corre contra la base de desarrollo. Si el hábito quedó de una corrida anterior se reusa, y la prueba
// lo devuelve a cero, así puede repetirse sin resetear nada. `pnpm db:reset` lo borra.

const NOMBRE = 'Prueba e2e';

const USUARIO = {
	email: 'usuario@javito.local',
	password: 'Javito2026'
};

test('mis hábitos: alta por la UI y marcar el día sin recargar', async ({ page }) => {
	// 1. Sin sesión, la ruta privada manda al login y recuerda el destino.
	await page.goto('/habitos');
	await expect(page).toHaveURL(/\/login\?destino=/);

	await page.getByLabel('Email').fill(USUARIO.email);
	await page.getByLabel('Contraseña').fill(USUARIO.password);
	await page.getByRole('button', { name: 'Entrar' }).click();

	await expect(page).toHaveURL(/\/habitos$/);
	await expect(page.getByRole('heading', { name: 'Hábitos' })).toBeVisible();

	// 2. Alta por AJAX. Si la fila ya existe de una corrida anterior, se reusa: el alta no es
	//    idempotente por nombre, así que no se puede volver a mandar a ciegas.
	const enElListado = page.locator('[data-test="fila-habito"]').filter({ hasText: NOMBRE });

	if ((await enElListado.count()) === 0) {
		await page.getByLabel('Título', { exact: true }).fill(NOMBRE);
		await page.getByLabel('Categoría').selectOption({ label: 'Cuerpo' });
		await page.getByLabel('Tipo').selectOption('contador');

		// Meta y unidad sólo se piden para un contador.
		await page.getByLabel('Meta').fill('3');
		await page.getByLabel('Unidad').fill('vasos');

		await page.getByRole('button', { name: 'Crear hábito' }).click();
	}

	// 3. El listado muestra categoría y frecuencia, que es lo que pide HAB-02.
	await expect(enElListado).toBeVisible();
	await expect(enElListado).toContainText('Cuerpo');
	await expect(enElListado).toContainText('Todos los días');

	// 4. En el día el hábito es un contador: se maneja con + y −, sin escribir números.
	await page.goto('/hoy');

	const enElDia = page.getByRole('listitem').filter({ hasText: NOMBRE });
	await expect(enElDia).toBeVisible();

	const sumar = enElDia.getByRole('button', { name: /Sumar/ });
	const restar = enElDia.getByRole('button', { name: /Restar/ });

	// Se normaliza a cero para que el resto de los pasos no dependa de corridas anteriores.
	for (let intento = 0; intento < 5 && (await restar.isEnabled()); intento++) {
		await restar.click();
	}

	await expect(enElDia).toContainText('0 / 3');

	// 5. Marcar: el valor cambia en la misma pantalla, sin recargar ni abrir un formulario.
	await sumar.click();
	await expect(enElDia).toContainText('1 / 3');
	await expect(page).toHaveURL(/\/hoy$/);

	await sumar.click();
	await expect(enElDia).toContainText('2 / 3');

	await sumar.click();
	await expect(enElDia).toContainText('3 / 3');

	// 6. Persistió: después de recargar, el día sigue diciendo lo mismo.
	await page.reload();
	await expect(page.getByRole('listitem').filter({ hasText: NOMBRE })).toContainText('3 / 3');

	// 7. Se puede bajar de la meta después de haberla alcanzado.
	const alVolver = page.getByRole('listitem').filter({ hasText: NOMBRE });
	await alVolver.getByRole('button', { name: /Restar/ }).click();
	await expect(alVolver).toContainText('2 / 3');

	// 8. Y queda en cero para la próxima corrida, que va a encontrar la fila ya creada.
	for (let intento = 0; intento < 3; intento++) {
		const bajar = page
			.getByRole('listitem')
			.filter({ hasText: NOMBRE })
			.getByRole('button', { name: /Restar/ });

		if (!(await bajar.isEnabled())) {
			break;
		}

		await bajar.click();
	}

	await expect(page.getByRole('listitem').filter({ hasText: NOMBRE })).toContainText('0 / 3');
});
