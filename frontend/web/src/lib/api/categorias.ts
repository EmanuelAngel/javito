// Módulo de categorías: solo declara el contrato y envuelve `pedir`. Sin `fetch` propio ni
// validación de runtime: el contrato lo define la WebAPI, que es nuestra.

import { pedir } from './pedir';

export type Categoria = {
	id: number;
	nombre: string;
	descripcion: string | null;
	activo: boolean;
};

export type DatosDeCategoria = {
	nombre: string;
	descripcion: string | null;
};

export function listar(activo?: boolean): Promise<Categoria[]> {
	const query = activo === undefined ? '' : `?activo=${activo}`;
	return pedir<Categoria[]>(`/api/categorias${query}`);
}

export function crear(datos: DatosDeCategoria): Promise<Categoria> {
	return pedir<Categoria>('/api/categorias', {
		method: 'POST',
		body: JSON.stringify(datos)
	});
}

export function editar(id: number, datos: DatosDeCategoria): Promise<Categoria> {
	return pedir<Categoria>(`/api/categorias/${id}`, {
		method: 'PUT',
		body: JSON.stringify(datos)
	});
}

export function cambiarActivo(id: number, activo: boolean): Promise<Categoria> {
	return pedir<Categoria>(`/api/categorias/${id}/activo`, {
		method: 'PUT',
		body: JSON.stringify({ activo })
	});
}
