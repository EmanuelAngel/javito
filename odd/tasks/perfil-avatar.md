# El avatar del usuario (PER-02) · estrena la infra de archivos

Cierra `PER-02` y con eso el último renglón de la **Puerta 2** (`RT-02`, 2 de 3). La base ya tiene
`usuario.avatar_url` y `articulo.imagen_portada_url`; lo que falta es el sistema para **guardar archivos
subidos** y su primer consumidor. La infra se escribe acá y la reusa `ART-01` (portada del artículo).

## Specs

**S1 — «Solo acepta imágenes, y lo valida el servidor además del navegador»** (HU-PER-02, literal).
El servidor no confía en el nombre ni en el `Content-Type` del archivo: lee la **firma** (magic bytes) y
sólo acepta JPEG (`FF D8 FF`), PNG (`89 50 4E 47 0D 0A 1A 0A`) y WebP (`RIFF` + `WEBP`). La extensión con
la que se guarda sale del tipo **detectado**, no del nombre que mandó el cliente. Un archivo que no es
imagen se rechaza con `400` y `codigo: "archivo_no_es_imagen"`.

**S2 — «Un archivo demasiado grande se rechaza con un mensaje claro»** (HU-PER-02, literal).
El tope es 5 MB, como en `~/dev/inmobiliaria-lab2`. Pasado el tope, la respuesta es `413` con
`codigo: "archivo_demasiado_grande"` y un `detail` que dice el límite en MB. El tope del cuerpo
multipart se alinea con el de Kestrel (30 MB) para que **todo cuerpo que el servidor acepte** pase por
la validación del almacén y reciba ese mensaje; por encima de 30 MB responde Kestrel, antes de la API.

**S3 — «El avatar anterior se reemplaza, no se acumula»** (HU-PER-02, literal).
Después de subir, la carpeta `avatares/` tiene **un solo archivo** de esa cuenta, la columna
`avatar_url` apunta al nuevo y la URL anterior deja de servirse (`404`). El orden es: guardar el nuevo,
actualizar la fila, borrar el viejo; si la fila no se actualiza, el nuevo se borra y no queda huérfano.

**S4 — «Sin avatar, la interfaz muestra un reemplazo (inicial del nombre)»** (HU-PER-02, literal).
`usuario.avatar_url` nace `NULL` en el seed. Sin avatar, la barra y la pantalla de perfil muestran un
círculo con la **inicial** del nombre (nunca un ícono genérico ni una imagen rota).

**S5 — «El avatar aparece en la barra superior sin recargar la página»** (HU-PER-02, literal).
`POST /api/perfil/avatar` devuelve el `UsuarioDto` actualizado; el SPA reemplaza el usuario de la sesión
con esa respuesta y el header se repinta solo. `UsuarioDto` gana `avatarUrl: string | null`, así que
`/api/sesion` y `/api/sesion/yo` también lo traen.

**S6 — La infra es genérica, no un caso especial del avatar.** `IAlmacenDeArchivos.GuardarImagenAsync(archivo, subcarpeta)`
y `Borrar(rutaRelativa)` trabajan por subcarpeta (`avatares`, y mañana `articulos/portadas`) y devuelven
la **ruta relativa web** (`/uploads/avatares/{guid}.png`), que es lo que entra en la columna
`VARCHAR(255)`. Nada de blobs en la base y nada de nombres del cliente en el disco.

**S7 — Las pruebas no ensucian el repositorio.** La raíz del almacenamiento es configurable
(`Archivos:Ruta`); el valor por defecto es `backend/Javito.Api/wwwroot/uploads` (ignorado por git) y el
fixture de pruebas apunta a un temporal que borra al terminar. Un pedido sin token no escribe ningún
archivo.

**S8 — Los rechazos no tocan nada.** Un pedido rechazado (sin archivo, vacío, no-imagen, demasiado
grande) devuelve su `ProblemDetails` con `codigo` estable y deja la fila y la carpeta como estaban.

**S9 — El gate cierra igual.** `pnpm lint` 0 · `pnpm check` 0 · `pnpm test:web` verde · `pnpm test:e2e`
verde (el recorrido nuevo del avatar incluido).

## Tasks

- **T1 — Infra de archivos + `POST /api/perfil/avatar` + `avatarUrl` en el contrato** (`S1`–`S3`,
  `S6`–`S8`) · inline · rama `feat/perfil-avatar` · commit
  `feat(api): guardar imágenes subidas y exponer el avatar del usuario`
- **T2 — Perfil y avatar en la barra, sin recargar** (`S3`–`S5`, `S9`) · inline · commit
  `feat(web): subir el avatar desde el perfil y mostrarlo en la barra`

## Log

**L1** — «Tenemos que hacer lo del avatar (PER-02). Eso incluye manejar archivos. Fijate en la
inmobiliaria (~/dev/inmobiliaria-lab2) cómo está hecho el sistema para manejar archivos y si lo podemos
usar acá (o al menos basarnos en ese para no complicarnos). Teniendo en cuenta que también el futuro
módulo de artículos requerirá subir portadas.»

**L2** — Revisión del antecedente (`~/dev/inmobiliaria-lab2`): `Services/IAlmacenadorArchivos.cs` +
`AlmacenadorArchivosLocal.cs` guardan en `wwwroot/uploads/{subcarpeta}/{guid}.{ext}` (servido por
`app.UseStaticFiles()`), validan vacío / 5 MB / extensión permitida, devuelven la ruta relativa web y
borran de forma idempotente con guarda de path traversal. La columna guarda **sólo la ruta**
(`ImagenInmueble.Url`, `Inmueble.ImagenPortada`), y el controlador orquesta: guardar nuevo, persistir,
borrar el viejo, y si falla lo persistido borra el nuevo. **Se reusa el diseño con cuatro correcciones**:
(1) validar la firma real del archivo, no la extensión del nombre; (2) devolver `ErrorDeNegocio`
(`codigo` estable) en vez de lanzar `InvalidOperationException`, que en esta API daría 500; (3) raíz de
almacenamiento configurable, porque acá las pruebas corren contra el mismo árbol y escribirían dentro del
repo; (4) montar el directorio con `PhysicalFileProvider` en `/uploads` en vez de depender del webroot
implícito. El resto —ruta relativa en la columna, `guid` como nombre, borrado idempotente y compensación
en el controlador— pasa tal cual, y sirve igual para la portada de `ART-01`.

**L3** — Cierre. **T1** (`82b3719`): `Archivos/` con `IAlmacenDeArchivos` + `AlmacenDeArchivosLocal`,
`FirmasDeImagen` (firma JPEG/PNG/WebP) y `ReglasDeArchivos` (topes y errores con `codigo`),
`PerfilController` con `POST avatar` y `avatarUrl` en `UsuarioDto`; 6 pruebas de endpoint nuevas, 80/80 de
API. **T2** (`ec9573d`): `avatar.svelte` (imagen o inicial), ruta `/perfil`, header leyendo de la sesión
para repintar sin recargar, `urlDeArchivo` para volver absoluta la ruta del almacén, y `pedir.ts` deja de
estampar `Content-Type: application/json` cuando el cuerpo es `FormData`; vitest 37/37 (dos de componente)
y un e2e nuevo.

Tres decisiones que costaron pensarlas:

- **El tope del cuerpo multipart.** Con 6 MB, un cuerpo de 7 MB no llegaba al controlador: el lector de
  formularios fallaba, MVC se lo tragaba y quedaba `archivo_requerido`, un mensaje que miente. El tope se
  alineó con el de Kestrel (30 MB) para que todo cuerpo aceptado pase por la validación del almacén y
  reciba el `413` con el límite en MB. Por encima de 30 MB responde Kestrel, antes de la API.
- **El `Input` vendorizado no sirve para archivos.** Enlaza `value` además de `files`, Svelte reescribe el
  `value` de un input de archivo y el navegador tira `InvalidStateError` → la pantalla se caía al 500. Lo
  encontró el e2e, no la prueba de componente: el campo de archivo se reemplazó por un `<input type="file">`
  propio y el vendorizado no se toca (regla del repo).
- **`pedir.ts` cambió para todos.** La condición pasó de «hay cuerpo» a «el cuerpo es un string»: ningún
  llamador existente manda un cuerpo que no sea JSON o nada, así que para ellos no cambia nada, y la prueba
  nueva deja fijado que el JSON sigue saliendo con `application/json`.

Gate: `pnpm lint` 0 · `pnpm check` 0 (80/80 de API y svelte-check 0/0) · `pnpm test:web` 37/37 ·
`pnpm test:e2e` 3/3 (el recorrido nuevo incluido, sin tocar los dos que ya estaban). La rama
`feat/perfil-avatar` queda con las dos unidades; falta tu decisión de mergear o no.
