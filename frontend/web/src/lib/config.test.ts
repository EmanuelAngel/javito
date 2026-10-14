import { describe, expect, it } from 'vitest';
import { API_URL } from './config';

describe('config', () => {
	it('la URL de la API apunta a la WebAPI local del puerto documentado', () => {
		const url = new URL(API_URL);

		expect(url.protocol).toBe('http:');
		expect(url.hostname).toBe('localhost');
		expect(url.port).toBe('5256');
	});
});
