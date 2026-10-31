// Estado de sesión del SPA: token JWT + usuario, con persistencia en localStorage.
//
// El módulo se importa tanto desde las rutas (SSR deshabilitado, pero igual corre en vitest
// con el project `server`, entorno node) como desde `pedir.ts`. Por eso toda lectura y
// escritura de localStorage se guarda contra `typeof localStorage === 'undefined'`: en node
// el identificador no existe y tiraría ReferenceError. Ver odd/tasks/acc-02.md, S5 y S11.

const CLAVE_ALMACENAMIENTO = 'javito-sesion';

export type Usuario = {
	id: number;
	nombre: string;
	email: string;
	rol: string;
};

export type Sesion = {
	token: string;
	usuario: Usuario;
};

function hayAlmacenamiento(): boolean {
	return typeof localStorage !== 'undefined';
}

function leerPersistida(): Sesion | null {
	if (!hayAlmacenamiento()) {
		return null;
	}

	try {
		const crudo = localStorage.getItem(CLAVE_ALMACENAMIENTO);

		if (!crudo) {
			return null;
		}

		const datos = JSON.parse(crudo) as Partial<Sesion>;

		if (!datos || typeof datos.token !== 'string' || !datos.usuario) {
			return null;
		}

		return { token: datos.token, usuario: datos.usuario };
	} catch {
		// localStorage puede fallar (modo privado, cuota) o la clave puede estar corrupta:
		// en ambos casos arranca sin sesión, nunca bloquea la app.
		return null;
	}
}

class EstadoDeSesion {
	// La única fuente del estado: lo leen el guard de (app), pedir.ts y el redirect
	// centralizado del 401 en el shell.
	actual: Sesion | null = $state(leerPersistida());

	// Acceso directo al token para el guard y para pedir.ts; null si no hay sesión.
	get token(): string | null {
		return this.actual?.token ?? null;
	}

	get usuario(): Usuario | null {
		return this.actual?.usuario ?? null;
	}

	iniciar(sesionNueva: Sesion): void {
		this.actual = sesionNueva;

		if (hayAlmacenamiento()) {
			localStorage.setItem(CLAVE_ALMACENAMIENTO, JSON.stringify(sesionNueva));
		}
	}

	// Reemplaza el usuario con la fila actual que devuelve GET /api/sesion/yo. El token no
	// cambia: los claims del JWT se congelan al firmarse, el usuario si puede variar.
	refrescarUsuario(usuario: Usuario): void {
		if (!this.actual) {
			return;
		}

		this.actual = { ...this.actual, usuario };

		if (hayAlmacenamiento()) {
			localStorage.setItem(CLAVE_ALMACENAMIENTO, JSON.stringify(this.actual));
		}
	}

	// Logout local. Con token de 8 hs sin refresh, vencer el token también termina acá.
	cerrar(): void {
		this.actual = null;

		if (hayAlmacenamiento()) {
			localStorage.removeItem(CLAVE_ALMACENAMIENTO);
		}
	}
}

export const sesion = new EstadoDeSesion();
