import { describe, expect, it, vi } from 'vitest';
import { isRedirect } from '@sveltejs/kit';
import type { Usuario } from './sesion.svelte';
import { ROL_ADMINISTRADOR, exigirAdministrador } from './roles';

const conRol = (rol: string) => async () => ({ usuario: { rol } as Usuario });

describe('exigirAdministrador', () => {
	it('deja pasar al Administrador sin redirigir', async () => {
		await expect(exigirAdministrador(conRol(ROL_ADMINISTRADOR))).resolves.toBeUndefined();
	});

	it('manda al Usuario a /hoy con un 302', async () => {
		const rebote = await exigirAdministrador(conRol('Usuario')).catch((e: unknown) => e);

		expect(isRedirect(rebote)).toBe(true);
		expect(rebote).toMatchObject({ status: 302, location: '/hoy' });
	});
});

type Carga = (evento: { parent: ReturnType<typeof conRol> }) => Promise<unknown>;

describe('pantallas de administración', () => {
	// El `load` real espera un LoadEvent entero; los guards sólo usan `parent`.
	const cargar = async (importar: () => Promise<{ load: unknown }>) =>
		(await importar()).load as Carga;

	const pantallas = [
		['/admin/categorias', () => import('../routes/(app)/admin/categorias/+page')],
		['/articulos/nuevo', () => import('../routes/(app)/articulos/nuevo/+page')]
	] as const;

	it.each(pantallas)(
		'%s rebota al Usuario antes de pedir nada a la API',
		async (_ruta, importar) => {
			vi.resetModules();
			const listar = vi.fn().mockResolvedValue([]);
			vi.doMock('$lib/api/categorias', () => ({ listar }));
			const load = await cargar(importar);

			const rebote = await load({ parent: conRol('Usuario') }).catch((e: unknown) => e);

			expect(rebote).toMatchObject({ status: 302, location: '/hoy' });
			expect(listar).not.toHaveBeenCalled();
		}
	);

	it.each(pantallas)(
		'%s deja pasar al Administrador y pide las categorías',
		async (_ruta, importar) => {
			vi.resetModules();
			const listar = vi.fn().mockResolvedValue([{ id: 1 }]);
			vi.doMock('$lib/api/categorias', () => ({ listar }));
			const load = await cargar(importar);

			await expect(load({ parent: conRol(ROL_ADMINISTRADOR) })).resolves.toEqual({
				categorias: [{ id: 1 }]
			});
			expect(listar).toHaveBeenCalledOnce();
		}
	);
});
