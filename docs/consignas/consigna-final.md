---
fecha-de-entrega: 2026-11-05
---

# Proyecto Integrador Final: Sitio Web MVC

## Presentación

El proyecto final consiste en la concepción, diseño e implementación de un sistema web MVC. La temática del proyecto es de libre elección por parte del grupo/estudiante, pero debe poseer una complejidad funcional y un calibre técnico equivalente al sistema de la Inmobiliaria trabajado en clase.

## Condiciones de Entrega y Evaluación

- **Para Regularizar**: El proyecto debe estar con un alto grado de finalización, cumpliendo con la estructura funcional base y respondiendo correctamente a los requerimientos mínimos de arquitectura y código descritos abajo.
- **Para Promocionar**: El proyecto debe estar 100% terminado, listo para ejecución local sin errores, con datos de prueba cargados, documentación completa y listo para la presentación y defensa oral.

## Requerimientos Mínimos Técnicos

### Modelo de Datos y Persistencia

Al menos 6 clases/tablas principales relacionadas entre sí. Debe incluir al menos una relación 1:N y una relación N:M, o una entidad que dependa de otras dos (ej. Contrato → Inmueble + Inquilino).

### Seguridad, Autenticación y Roles

- Login/Logout funcional.
- Seguridad en el backend y manejo de roles (ej. Administrador, Empleado, Cliente).
- Al menos una funcionalidad relevante restringida exclusivamente por rol.
- Gestión de Avatar personalizado en el perfil de usuario.

### Manejo de Archivos

- Procesamiento y almacenamiento de archivos/imágenes en el servidor, adicional al avatar del usuario (ej. catálogo de fotos, comprobantes, adjuntos).

### Módulo SPA / AJAX con Vue.js o similar

- Al menos un ABM/CRUD completo desarrollado con Vue.js u otro framework reactivo, donde todas las operaciones (Alta, Baja, Modificación y Listado) se resuelvan vía AJAX/JSON sin recargar la página.

### Paginado por Servidor (Server-Side Pagination)

- Todos los listados principales deben contar con paginado servido desde el backend (se debe solicitar y recibir únicamente la página actual por petición). Quedan exceptuadas solo tablas maestras muy pequeñas.

### Búsqueda Asíncrona de Entidades Relacionadas

- Al seleccionar entidades relacionadas en un formulario (ej. seleccionar un Cliente al crear una Orden), debe hacerse mediante búsqueda asíncrona vía AJAX en el servidor. Está prohibido traer todos los registros de la base de datos para llenar un `<select>` masivo.

### Web API REST con JWT

- Implementación de endpoints de Web API autenticados mediante JSON Web Tokens (JWT). Si la aplicación web no consume esta API de manera nativa, se debe adjuntar la colección de pruebas correspondiente.

## Contenido y Estructura del Repositorio (Entrega)

La entrega se realizará adjuntando el enlace al repositorio público de GitHub, el cual debe incluir:

- Archivo .gitignore configurado adecuadamente.
- Diagrama de Entidad-Relación (DER) o Diagrama de Clases del modelo.
- Archivo README.md obligatorio que contenga:
- Descripción general del proyecto y el problema que resuelve.
- Matriz de Evidencias de Requerimientos Mínimos (Tabla que certifique el cumplimiento de cada punto).

## Tabla de Evidencias en el README.md

Cada proyecto debe incluir una tabla en el README.md detallando exactamente dónde y cómo implementaron cada requerimiento. Ejemplo:

| Requerimiento Mínimo | Estado | Enlace al Código / Controladores | Evidencia (Captura / GIF / Video) |
|---|---|---|---|
| 1. 4 Tablas y Relación 1:N | Cumplido | [DataContext.cs](URL_LINEA_CODIGO) | Diagrama DER adjunto en /docs/der.png |
| 2. Login, Roles y Avatar | Cumplido | [AccountController.cs](URL_LINEA_CODIGO) | Captura de pantalla de perfil con avatar |
| 3. Manejo de Archivos (Extra) | Cumplido | [ProductosController.cs#L45](URL) | Video corto / GIF mostrando subida de comprobante |
| 4. ABM en Vue.js + AJAX | Cumplido | [VueComponent.js](URL) | Video corto / GIF del ABM dinámico |
| 5. Paginado por Servidor | Cumplido | [Paginador.cs](URL) | Captura del inspector de red enviando page=2 |
| 6. Selección Asíncrona (AJAX) | Cumplido | [BuscarClientes.cshtml](URL) | GIF buscando entidades en desplegable |
| 7. Web API + JWT | Cumplido | [ApiController.cs](URL) | Captura de Postman con Bearer Token |

## Requisitos Adicionales según Condición del Alumno

- Estudiantes que entregan para **REGULARIZAR**:
    - Enlace al repositorio con el proyecto en estado funcional avanzado.
    - Diagrama DER y README.md con la Matriz de Evidencias completada hasta el estado actual (al menos 70% de los puntos).

- Estudiantes que se postulan para **PROMOCIONAR**:
    - Todos los puntos anteriores completados al 100%.
    - Script de Base de Datos (.sql o dump) con datos de prueba precargados.
    - Credenciales de Usuarios de prueba para cada rol (ej. Admin / Empleado) especificadas en el README.md.
    - Colección de Postman / Bruno / Insomnia exportada en el repositorio para probar la Web API con JWT (solo cuando la web no consume la API).

## Defensa

Luego del cierre de la tarea, se harán las presentaciones y defensas de los proyectos que estén listos para promocionar.
 