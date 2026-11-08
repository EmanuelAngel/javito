import { flushSync, mount } from 'svelte';
import { afterEach, describe, expect, it } from 'vitest';
import type { HabitoDelDia } from '$lib/api/dias';
import FilaDelDia from './fila-del-dia.svelte';

// HU #75: un hábito que no es diario muestra su avance como «2 / 3 esta semana», y al llegar a la
// cuota —o pasarse— el badge lo dice con una tilde y una etiqueta, no sólo con el color.
describe('fila del día: progreso semanal', () => {
	afterEach(() => {
		document.body.innerHTML = '';
	});

	const lunes = new Date(Date.UTC(2026, 10, 2));

	function semana(cumplidos: number, objetivo: number) {
		return {
			cumplidos,
			objetivo,
			dias: Array.from({ length: 7 }, (_, indice) => ({
				fecha: new Date(lunes.getTime() + indice * 86_400_000).toISOString().slice(0, 10),
				completado: indice < cumplidos
			}))
		};
	}

	function semanal(cumplidos: number, objetivo = 3): HabitoDelDia {
		return {
			id: 12,
			titulo: 'Ejercicio pesado',
			categoria: 'Movimiento',
			tipo: 'si_no',
			meta: null,
			unidad: null,
			frecuencia: { tipo: 'semanal', veces: objetivo },
			semana: semana(cumplidos, objetivo),
			registro: null
		};
	}

	function montar(habito: HabitoDelDia) {
		const contenedor = document.createElement('ul');
		document.body.append(contenedor);

		mount(FilaDelDia, {
			target: contenedor,
			props: {
				habito,
				fecha: '2026-11-04',
				alternar: () => {},
				subir: () => {},
				bajar: () => {}
			}
		});
		flushSync();

		return contenedor;
	}

	function badge(raiz: HTMLElement) {
		return raiz.querySelector<HTMLElement>('[data-slot="badge"]');
	}

	it('bajo la cuota muestra «1 / 3 esta semana», sin tilde ni etiqueta de cuota cumplida', () => {
		const elemento = badge(montar(semanal(1)));

		expect(elemento?.textContent?.trim()).toBe('1 / 3 esta semana');
		expect(elemento?.querySelector('svg')).toBeNull();
		expect(elemento?.hasAttribute('aria-label')).toBe(false);
		expect(elemento?.className).toContain('bg-secondary');
	});

	it('al llegar a la cuota resalta el badge, suma la tilde y dice «cuota cumplida»', () => {
		const elemento = badge(montar(semanal(3)));

		expect(elemento?.textContent?.trim()).toBe('3 / 3 esta semana');
		expect(elemento?.querySelector('svg')?.getAttribute('aria-hidden')).toBe('true');
		expect(elemento?.getAttribute('aria-label')).toBe('3 / 3 esta semana, cuota cumplida');
		// Sin rol, un `span` genérico no anuncia su etiqueta en todos los lectores de pantalla.
		expect(elemento?.getAttribute('role')).toBe('img');
		expect(elemento?.className).toContain('bg-primary');
		expect(elemento?.className).not.toContain('bg-secondary');
	});

	it('pasarse de la cuota muestra «4 / 3 esta semana» y queda en el mismo estado, sin recortar', () => {
		const elemento = badge(montar(semanal(4)));

		expect(elemento?.textContent?.trim()).toBe('4 / 3 esta semana');
		expect(elemento?.querySelector('svg')).not.toBeNull();
		expect(elemento?.getAttribute('aria-label')).toBe('4 / 3 esta semana, cuota cumplida');
		expect(elemento?.className).toContain('bg-primary');
	});

	it('un hábito diario dice «Todos los días» y no lleva el badge semanal', () => {
		const diario: HabitoDelDia = {
			...semanal(0),
			id: 1,
			titulo: 'Hidratación con agua',
			frecuencia: { tipo: 'diaria', veces: null },
			semana: semana(0, 7)
		};
		const raiz = montar(diario);

		expect(badge(raiz)).toBeNull();
		expect(raiz.textContent).toContain('Todos los días');
		expect(raiz.textContent).not.toContain('esta semana');
	});
});
