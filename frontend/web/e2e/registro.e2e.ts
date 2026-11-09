import { expect, test } from '@playwright/test';

// ACC-01 (HU-ACC-01): el registro público, de punta a punta contra la WebAPI real y la base de
// DESARROLLO. El email lleva la hora de la corrida porque la baja es lógica y no hay DELETE: la cuenta
// queda en la base hasta el próximo `pnpm db:reset`, y un email fijo chocaría en la segunda corrida.

test('registrarme deja la sesión iniciada y el email repetido se muestra en el formulario', async ({
	page
}) => {
	const email = `pruebas-e2e-registro-${Date.now()}@javito.test`;
	const password = 'Javito2026';

	await page.goto('/registro');

	// No se pide avatar.
	await expect(page.getByLabel(/avatar/i)).toHaveCount(0);

	// 1. Contraseña corta: la rechaza el formulario y no se navega.
	await page.getByLabel('Nombre').fill('Pruebas e2e registro');
	await page.getByLabel('Email').fill(email);
	await page.getByLabel('Contraseña').fill('corta');
	await page.getByRole('button', { name: 'Crear cuenta' }).click();
	await expect(page).toHaveURL(/\/registro$/);

	// 2. Con una contraseña válida entra directo, sin pasar por el login.
	await page.getByLabel('Contraseña').fill(password);
	await page.getByRole('button', { name: 'Crear cuenta' }).click();
	await expect(page).toHaveURL(/\/hoy$/);

	// 3. Sin sesión, el mismo email vuelve como error del formulario.
	await page.evaluate(() => localStorage.clear());
	await page.goto('/registro');
	await page.getByLabel('Nombre').fill('Pruebas e2e registro');
	await page.getByLabel('Email').fill(email);
	await page.getByLabel('Contraseña').fill(password);
	await page.getByRole('button', { name: 'Crear cuenta' }).click();

	await expect(page).toHaveURL(/\/registro$/);
	await expect(page.getByText(/Ya existe una cuenta con el email/)).toBeVisible();
});

test('el login enlaza al registro', async ({ page }) => {
	await page.goto('/login');
	await page.getByRole('link', { name: 'Creá una' }).click();
	await expect(page).toHaveURL(/\/registro/);
});
