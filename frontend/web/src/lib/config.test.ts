import { describe, expect, it } from 'vitest';
import { API_URL, urlDeArchivo } from './config';

describe('config', () => {
	it('la URL de la API apunta a la WebAPI local del puerto documentado', () => {
		const url = new URL(API_URL);

		expect(url.protocol).toBe('http:');
		expect(url.hostname).toBe('localhost');
		expect(url.port).toBe('5256');
	});

	it('una ruta del almacén se vuelve absoluta contra la WebAPI', () => {
		// La base guarda `/uploads/avatares/x.png`: el <img> del SPA necesita el origen de la API,
		// porque el archivo no vive en el origen del SPA.
		expect(urlDeArchivo('/uploads/avatares/x.png')).toBe(`${API_URL}/uploads/avatares/x.png`);
	});
});
