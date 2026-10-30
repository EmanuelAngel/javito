import { describe, expect, it } from 'vitest';
import { destinoSeguro, rutaDeLogin } from './navegacion';

describe('destinoSeguro', () => {
	it('acepta una ruta interna con su query', () => {
		expect(destinoSeguro('/habitos?archivados=true')).toBe('/habitos?archivados=true');
	});

	it('rechaza una URL absoluta y devuelve /hoy', () => {
		expect(destinoSeguro('https://otro.host/robo')).toBe('/hoy');
	});

	it('rechaza el protocolo relativo // y la barra invertida', () => {
		expect(destinoSeguro('//otro.host')).toBe('/hoy');
		expect(destinoSeguro('/\\otro.host')).toBe('/hoy');
	});

	it('sin destino devuelve /hoy', () => {
		expect(destinoSeguro(null)).toBe('/hoy');
		expect(destinoSeguro('')).toBe('/hoy');
	});
});

describe('rutaDeLogin', () => {
	it('codifica el destino para que sobreviva al query string', () => {
		expect(rutaDeLogin('/habitos?archivados=true')).toBe(
			'/login?destino=%2Fhabitos%3Farchivados%3Dtrue'
		);
	});
});
