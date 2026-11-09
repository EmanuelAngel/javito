# ACC-01 · Registro público (issue #68, HU-ACC-01)

Rama `feat/registro-publico`. Un Visitante crea su cuenta y queda con sesión iniciada.

## Specs

- **S1** — "El email se guarda en minúsculas y sin espacios"
- **S2** — "Un email ya registrado devuelve un error que se muestra en el formulario" (`409 email_repetido`, campo `email`)
- **S3** — "La contraseña nunca se guarda en texto plano" (BCrypt, sin hash en la respuesta)
- **S4** — "Una contraseña demasiado corta la rechaza el formulario **y** el servidor" (8 a 72, igual que el alta de administración)
- **S5** — "La cuenta nace con rol `Usuario` y activa"
- **S6** — "Al registrarme quedo con sesión iniciada, sin pasar por el login" (la respuesta es un `SesionDto`)
- **S7** — "No se pide avatar en el registro"
- **S8** — Decisión: `POST /api/cuentas` anónimo, sin campo `rol`; `POST /api/usuarios` sigue siendo sólo de administración.

## Tasks

- T1 · S1-S8 · backend: endpoint + pruebas · commit: pendiente
- T2 · S2, S4, S6 · SPA: `/registro`, enlace desde login, pruebas · commit: pendiente

## Log

- L1: "Issue #68"
