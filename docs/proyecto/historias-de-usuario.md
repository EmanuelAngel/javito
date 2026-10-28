# Historias de usuario de Javito

El **qué** del sistema, escrito desde quien lo usa. Es el puente entre la
[propuesta aprobada](./propuesta.md) y los issues: este documento no agrega requisitos que la propuesta no
tenga, los **descompone** en unidades de valor verificables.

> **Relación con el issue-tracking.** El issue sigue siendo el **cómo/cuándo** (una rama, un PR). Este
> documento es el **qué**: estable, se lee de corrido y no depende de en qué momento del trabajo estés.
> Cuando una historia se encara, se deriva a un issue con el template `feature.yml` — mismo `rol`, misma
> `historia`, mismos `criterios`, mismo `fuera de alcance`, así que derivar es copiar y pegar.

## Cómo leer cada historia

- **ID**: `HU-<módulo>-nn`. El número no cambia nunca, aunque la historia se edite.
- **Formato**: `Como <rol> quiero <acción> para <beneficio>`, igual que el template de issue.
- **Criterios**: reglas verificables, una por línea, en el mismo formato de checkbox del template. Son lo
  que se tilda para cerrar el issue.
- **Trazabilidad**: el requisito de cátedra que la historia evidencia, cuando aplica.

Las decisiones tomadas en la sesión de grilling y las que quedaron abiertas están al final, en
[Decisiones](#decisiones-de-la-sesión) y [Preguntas abiertas](#preguntas-abiertas).

## Actores

| Actor | Quién es | Qué puede hacer |
| :--- | :--- | :--- |
| **Visitante** | Cualquier persona sin sesión. | Registrarse e iniciar sesión. Nada más. |
| **Usuario** | La persona que registra sus hábitos. | Sus hábitos, sus registros, sus rutinas, su perfil y leer artículos. |
| **Administrador** | Quien cura el contenido y mantiene el sistema. | Todo lo del Usuario, más categorías, artículos y usuarios. |

No hay personas con nombre y apellido. El sistema tiene un solo humano detrás; inventar a "Alex, 28 años"
sería teatro. Los roles, en cambio, son reales: son los que evalúa la cátedra y los que restringe
`[Authorize]`.

**Reglas transversales**: la semana arranca el **lunes** y cierra el **domingo**. Toda fecha de registro es
un día calendario, sin hora. Un hábito solo se puede registrar una vez por día: los registros del mismo día
**acumulan** valor, no se duplican.

---

## 1. Acceso

| ID | Trazabilidad |
| :--- | :--- |
| `HU-ACC-01` | Login/Logout, JWT |

### HU-ACC-01 — Registrarme

**Como Visitante quiero crear una cuenta con nombre, email y contraseña para que mis hábitos queden guardados y no se pierdan.**

- [ ] El email se guarda en minúsculas y sin espacios
- [ ] Un email ya registrado devuelve un error que se muestra en el formulario
- [ ] La contraseña nunca se guarda en texto plano
- [ ] Una contraseña demasiado corta la rechaza el formulario **y** el servidor
- [ ] La cuenta nace con rol `Usuario` y activa
- [ ] Al registrarme quedo con sesión iniciada, sin pasar por el login
- [ ] No se pide avatar en el registro

**Fuera de alcance**: verificación por email, recuperación de contraseña, login con Google.

### HU-ACC-02 — Iniciar sesión

**Como Visitante quiero entrar con mi email y contraseña para acceder a mis hábitos desde cualquier navegador.**

- [ ] Email o contraseña incorrectos devuelven el mismo mensaje, sin decir cuál de los dos falló
- [ ] Un usuario desactivado no puede entrar, aunque la contraseña sea correcta
- [ ] La sesión sobrevive a recargar la página
- [ ] Al recargar, la aplicación recuerda quién soy sin volver a pedirme la contraseña
- [ ] Un token vencido me devuelve al login, no a una pantalla vacía

### HU-ACC-03 — Cerrar sesión

**Como Usuario quiero cerrar sesión para dejar el navegador limpio cuando uso una computadora compartida.**

- [ ] Al cerrar sesión el token se descarta
- [ ] Volver a una pantalla privada después de cerrar sesión me lleva al login

---

## 2. Perfil

| ID | Trazabilidad |
| :--- | :--- |
| `HU-PER-01` | Edición de datos personales |
| `HU-PER-02` | **Avatar** · Manejo de archivos |

### HU-PER-01 — Ver y editar mis datos

**Como Usuario quiero ver y corregir mi nombre y mi email para mantener mi cuenta al día.**

- [ ] Puedo editar nombre y email; el rol no
- [ ] El email sigue las mismas reglas que en el registro, incluida la unicidad
- [ ] Si el cambio falla, el formulario muestra el motivo y no pierde lo que escribí

### HU-PER-02 — Subir mi avatar

**Como Usuario quiero subir una foto de perfil para reconocer mi cuenta de un vistazo.**

- [ ] Solo acepta imágenes, y lo valida el servidor además del navegador
- [ ] Un archivo demasiado grande se rechaza con un mensaje claro
- [ ] El avatar anterior se reemplaza, no se acumula
- [ ] Sin avatar, la interfaz muestra un reemplazo (inicial del nombre)
- [ ] El avatar aparece en la barra superior sin recargar la página

---

## 3. Categorías (Administrador)

| ID | Trazabilidad |
| :--- | :--- |
| `HU-CAT-01` | |
| `HU-CAT-02` | |
| `HU-CAT-03` | |
| `HU-CAT-04` | Baja lógica |

### HU-CAT-01 — Ver el listado de categorías

**Como Administrador quiero ver todas mis categorías juntas para administrarlas de un vistazo.**

- [ ] El listado las trae todas de una vez: son pocas y no se pagina
- [ ] El listado distingue activas de desactivadas
- [ ] Puedo buscar por nombre

### HU-CAT-02 — Crear una categoría

**Como Administrador quiero crear una categoría para clasificar hábitos y artículos con el mismo vocabulario.**

- [ ] El nombre es obligatorio y no puede repetirse (comparando sin distinguir mayúsculas)
- [ ] Puedo agregar una descripción
- [ ] La categoría nace activa y disponible en los selectores

### HU-CAT-03 — Editar una categoría

**Como Administrador quiero corregir el nombre o la descripción de una categoría para arreglar un error de tipeo sin perder lo que ya clasifiqué.**

- [ ] Cambiar el nombre no afecta a los hábitos ni a los artículos que la usan
- [ ] El nombre sigue sin poder repetirse

### HU-CAT-04 — Desactivar y reactivar una categoría

**Como Administrador quiero dar de baja una categoría sin borrarla para que el historial no pierda su clasificación.**

- [ ] Desactivar no borra la fila ni toca los hábitos o artículos que la usan
- [ ] Una categoría desactivada desaparece de los selectores de alta y edición
- [ ] Los hábitos y artículos que ya la tenían siguen mostrándola
- [ ] Puedo reactivarla y vuelve a los selectores

---

## 4. Hábitos (Usuario)

| ID | Trazabilidad |
| :--- | :--- |
| `HU-HAB-01` | |
| `HU-HAB-02` | |
| `HU-HAB-03` | |
| `HU-HAB-04` | Baja lógica |
| `HU-HAB-05` | |

### HU-HAB-01 — Crear un hábito

**Como Usuario quiero definir un hábito con sus instrucciones y su frecuencia para saber exactamente qué tengo que hacer y cuántas veces por semana.**

- [ ] Pido título y descripción; la descripción es opcional
- [ ] Elijo la categoría de un desplegable con todas las activas
- [ ] Elijo el tipo: `sí/no`, `contador` o `abstinencia`
- [ ] Si es contador, pido la unidad (`vasos`, `páginas`, `minutos`) y la meta del día
- [ ] Si es `sí/no`, la meta y la unidad no se piden
- [ ] Un hábito de abstinencia se marca igual que uno `sí/no`, con otro texto: tildar significa *hoy me mantuve*
- [ ] Elijo la frecuencia: **todos los días** o **N veces por semana**
- [ ] Si es semanal, la cantidad es obligatoria y va de 1 a 6
- [ ] El hábito nace activo y aparece en la pantalla del día
- [ ] Con 14 hábitos cargados no aparece ningún límite artificial

**Fuera de alcance**: días fijos de la semana (lunes/miércoles/viernes), recordatorios, hábitos compartidos, y el contador en vivo de abstinencia (días, horas y minutos) con su botón de recaída.

### HU-HAB-02 — Ver mis hábitos

**Como Usuario quiero ver mis hábitos separando activos de archivados para saber qué estoy sosteniendo hoy y qué dejé.**

- [ ] El listado muestra solo mis hábitos, nunca los de otra cuenta
- [ ] Distingo activos de archivados sin que los archivados ensucien la vista principal
- [ ] Cada hábito muestra su categoría y su frecuencia

### HU-HAB-03 — Editar un hábito

**Como Usuario quiero corregir la meta, la descripción o la categoría de un hábito para ajustarlo sin perder lo que ya registré.**

- [ ] Puedo cambiar título, descripción, categoría, tipo, meta, unidad y frecuencia
- [ ] Los cambios rigen desde ese día; **los días ya registrados conservan la meta con la que se cerraron**
- [ ] Cambiar el tipo (de `sí/no` a `contador`) no borra los registros anteriores
- [ ] Editar un hábito ajeno no es posible, ni siquiera escribiendo la URL a mano

### HU-HAB-04 — Archivar y reactivar un hábito

**Como Usuario quiero archivar un hábito que ya no sigo para sacarlo de mi vista sin borrar su historial.**

- [ ] Archivar lo saca de la pantalla del día y de los selectores de rutina
- [ ] El historial del hábito archivado se conserva y sigue siendo consultable
- [ ] Reactivarlo lo devuelve a la pantalla del día, sin huecos fantasma en los días que estuvo archivado

**Fuera de alcance**: borrado definitivo desde la interfaz.

### HU-HAB-05 — Elegir la categoría de un hábito

**Como Usuario quiero elegir la categoría de un hábito de una lista para clasificarlo sin escribir nada.**

- [ ] Puedo elegir la categoría al crear el hábito y al editarlo
- [ ] El desplegable trae todas las categorías activas: son pocas, así que no se busca ni se pagina
- [ ] Una categoría desactivada no aparece entre las opciones
- [ ] Un hábito puede quedar sin categoría
- [ ] El hábito conserva la categoría que eligió, aunque después se desactive

---

## 5. Registros del día

| ID | Trazabilidad |
| :--- | :--- |
| `HU-REG-01` | **AJAX** (CRUD sin recargar) |
| `HU-REG-02` | |
| `HU-REG-03` | |
| `HU-REG-04` | |
| `HU-REG-05` | |
| `HU-REG-06` | |

### HU-REG-01 — Marcar el día

**Como Usuario quiero marcar un hábito en un toque desde la pantalla principal para que registrar me cueste dos segundos mientras trabajo.**

- [ ] Un hábito `sí/no` se marca y se desmarca con un solo toque
- [ ] Un hábito `contador` sube y baja con `+` y `−`, sin escribir números
- [ ] El cambio se guarda al instante, sin recargar la página ni abrir un formulario
- [ ] Al llegar a la meta, el hábito se marca como completado solo
- [ ] Puedo bajar de la meta después de haberla alcanzado
- [ ] Si la conexión falla, la interfaz lo avisa y no se queda mostrando un estado falso

### HU-REG-02 — Deshacer el día

**Como Usuario quiero borrar todo lo que registré hoy en un hábito para corregir un error de dedo sin dejar el día a medias.**

- [ ] Puedo reiniciar el día de un hábito: vuelve a cero y queda sin completar
- [ ] El reinicio no toca los días anteriores
- [ ] Si el hábito era semanal, el reinicio devuelve el cupo a la semana

### HU-REG-03 — Corregir un día pasado

**Como Usuario quiero completar un día que me olvidé para que un descuido de las 23:59 no me arruine la semana.**

- [ ] Puedo registrar y corregir cualquier día de los últimos 7
- [ ] Más atrás de 7 días, el registro es de solo lectura
- [ ] La corrección usa las mismas reglas que el día de hoy
- [ ] Un día corregido vale igual que uno registrado en el momento

**Fuera de alcance**: backfill libre de meses enteros.

### HU-REG-04 — Ver la semana de un vistazo

**Como Usuario quiero ver los últimos 7 días de cada hábito para saber si estoy sosteniendo el ritmo sin abrir un reporte.**

- [ ] Cada tarjeta muestra sus 7 días, de lunes a domingo
- [ ] Un día cumplido se distingue de uno vacío sin depender solo del color
- [ ] Los días de un hábito semanal que todavía no llegó a su cuota se muestran **neutros, nunca en rojo**
- [ ] Hoy se distingue de los días anteriores

### HU-REG-05 — Anotar una nota del día

**Como Usuario quiero dejar una nota corta en el día de un hábito para recordar por qué salió bien o mal.**

- [ ] Cada día de cada hábito admite una nota de texto libre
- [ ] La nota es opcional: registrar sin nota sigue siendo de un toque
- [ ] La nota se guarda junto al registro del día, no la reemplaza
- [ ] Un día pasado y corregible también admite nota

### HU-REG-06 — Cumplir un hábito que no es diario

**Como Usuario quiero ver cuántas veces llevo esta semana de un hábito como `Ejercicio pesado 3 veces por semana` para saber si voy bien sin que la app me marque fracasos.**

- [ ] La tarjeta muestra el progreso en la forma `2 / 3 esta semana`
- [ ] Los días marcados de la semana se pintan; los que faltan quedan neutros
- [ ] La semana cierra el domingo y el lunes arranca un cupo nuevo
- [ ] Un día de descanso no cuenta como incumplimiento
- [ ] Al llegar a la cuota, el hábito de esa semana queda cumplido
- [ ] Pasarse de la cuota no rompe nada

---

## 6. Rutinas (Usuario)

| ID | Trazabilidad |
| :--- | :--- |
| `HU-RUT-01` | **N:M** (`RutinaHabito`) |
| `HU-RUT-02` | **N:M** · **Búsqueda asíncrona** |
| `HU-RUT-03` | **N:M** |
| `HU-RUT-04` | |
| `HU-RUT-05` | Baja lógica |

### HU-RUT-01 — Crear una rutina

**Como Usuario quiero agrupar hábitos bajo un nombre como `Mañana` o `Corte de escritorio` para hacerlos en bloque y en orden.**

- [ ] Pido nombre obligatorio y descripción opcional
- [ ] La rutina nace activa, vacía y sin hábitos
- [ ] Puedo tener varias rutinas en el mismo momento del día

### HU-RUT-02 — Agregar hábitos a una rutina

**Como Usuario quiero agregar hábitos a una rutina buscándolos para no recorrer la lista completa cada vez.**

- [ ] Busco el hábito por nombre mientras escribo y lo agrego desde ahí
- [ ] La búsqueda consulta el servidor y solo devuelve **mis** hábitos activos
- [ ] El mismo hábito no se puede agregar dos veces a la misma rutina
- [ ] Un hábito puede estar en varias rutinas a la vez
- [ ] Puedo quitar un hábito de la rutina sin archivarlo

### HU-RUT-03 — Ordenar los hábitos de una rutina

**Como Usuario quiero ordenar los hábitos dentro de la rutina para que el orden sea el que quiero hacer, no el que se cargó.**

- [ ] Puedo reordenar los hábitos y el orden persiste al recargar
- [ ] Agregar un hábito nuevo no rompe el orden de los anteriores
- [ ] El orden es de cada rutina, no global

### HU-RUT-04 — Filtrar mi día por rutina

**Como Usuario quiero filtrar la pantalla principal por rutina para ver solo lo que me toca ahora y no los 14 hábitos juntos.**

- [ ] Puedo elegir una rutina y la pantalla muestra solo sus hábitos, en su orden
- [ ] Puedo volver a ver todos
- [ ] El filtro no cambia lo que registro: marcar dentro del filtro marca el hábito real

### HU-RUT-05 — Editar y desactivar una rutina

**Como Usuario quiero renombrar o dar de baja una rutina para que la pantalla del día no se me llene de bloques viejos.**

- [ ] Puedo editar nombre y descripción
- [ ] Desactivarla la quita de los filtros sin borrarla ni borrar sus hábitos
- [ ] Puedo reactivarla con sus hábitos y su orden intactos
- [ ] Archivar un hábito lo quita de las rutinas sin romperlas

---

## 7. Artículos

| ID | Trazabilidad |
| :--- | :--- |
| `HU-ART-01` | **Manejo de archivos** |
| `HU-ART-02` | |
| `HU-ART-03` | Baja lógica |
| `HU-ART-04` | Paginado por servidor |
| `HU-ART-05` | |

### HU-ART-01 — Publicar un artículo (Administrador)

**Como Administrador quiero escribir un artículo sobre hábitos y subirle una portada para fundamentar la práctica con algo más que casillas.**

- [ ] Escribo título, resumen y contenido en Markdown
- [ ] Elijo la categoría de un desplegable con todas las categorías activas
- [ ] Subo una imagen de portada, validada como imagen y por tamaño
- [ ] El artículo nace activo y con fecha de publicación
- [ ] Un artículo sin portada se puede publicar igual
- [ ] La vista previa del Markdown muestra lo que va a ver el lector

### HU-ART-02 — Editar un artículo (Administrador)

**Como Administrador quiero corregir un artículo publicado para arreglar un error sin despublicarlo.**

- [ ] Puedo editar título, resumen, contenido, categoría y portada
- [ ] Reemplazar la portada no acumula imágenes huérfanas
- [ ] La fecha de publicación original no cambia al editar

### HU-ART-03 — Desactivar un artículo (Administrador)

**Como Administrador quiero retirar un artículo para que deje de aparecer sin borrarlo.**

- [ ] Un artículo desactivado desaparece del catálogo y no se puede abrir por URL
- [ ] Se puede reactivar
- [ ] Publicar, editar y desactivar son exclusivos del rol Administrador: un Usuario que intente hacerlo recibe un rechazo del servidor, no solo un botón oculto

### HU-ART-04 — Ver el catálogo de artículos

**Como Usuario quiero recorrer los artículos paginados para encontrar algo útil sin cargar todo el blog de una vez.**

- [ ] El catálogo llega paginado desde el servidor, solo la página actual
- [ ] Solo muestra artículos activos, del más nuevo al más viejo
- [ ] Cada tarjeta muestra portada, título, resumen, categoría y fecha
- [ ] Puedo filtrar por categoría
- [ ] Un artículo desactivado nunca aparece en el catálogo

### HU-ART-05 — Leer un artículo

**Como Usuario quiero leer el artículo completo con su formato para aprovechar el contenido y no un muro de texto plano.**

- [ ] El Markdown se renderiza con títulos, listas y negritas
- [ ] La portada se ve en la cabecera del artículo
- [ ] Se ve la categoría y la fecha
- [ ] Volver al catálogo conserva la página en la que estaba

---

## 8. Historial

| ID | Trazabilidad |
| :--- | :--- |
| `HU-HIS-01` | Paginado por servidor |
| `HU-HIS-02` | |

### HU-HIS-01 — Ver mi historial de registros

**Como Usuario quiero consultar mis registros paginados y filtrables para revisar un período concreto sin traer años de datos.**

- [ ] El historial es paginado en el servidor
- [ ] Puedo filtrar por hábito y por rango de fechas
- [ ] Solo veo mis registros
- [ ] Cada fila muestra fecha, hábito, valor alcanzado, meta de ese día y si se completó
- [ ] Las notas aparecen cuando el día tiene una

### HU-HIS-02 — Ver el detalle histórico de un hábito

**Como Usuario quiero abrir un hábito y ver su histórico completo para juzgar mi consistencia real, no la de esta semana.**

- [ ] El detalle muestra una cuadrícula de 30, 60 o 365 días, a elección
- [ ] Muestra el total de veces cumplido y el porcentaje del período
- [ ] La cuadrícula se lee igual que la semanal: cumplido, vacío y neutro son distinguibles sin depender del color
- [ ] Desde el detalle puedo editar el hábito

---

## 9. Usuarios (Administrador)

| ID | Trazabilidad |
| :--- | :--- |
| `HU-USU-01` … `HU-USU-05` | Restricción por rol |

### HU-USU-01 — Ver el listado de usuarios (Administrador)

**Como Administrador quiero ver los usuarios paginados para saber quién está usando el sistema y en qué estado.**

- [ ] El listado es paginado en el servidor
- [ ] Muestra nombre, email, rol y estado
- [ ] Puedo buscar por nombre o email
- [ ] Distingo activos de desactivados

### HU-USU-02 — Crear un usuario (Administrador)

**Como Administrador quiero dar de alta una cuenta a mano para habilitar a alguien sin pasar por el registro público.**

- [ ] El camino normal sigue siendo el registro público; esto es la excepción
- [ ] Asigno la contraseña inicial y se la comunico yo: no hay envío de emails en el sistema
- [ ] Puedo elegir el rol desde el alta
- [ ] Valen las mismas reglas de email y contraseña que en el registro

### HU-USU-03 — Editar los datos de un usuario (Administrador)

**Como Administrador quiero corregir el nombre o el email de una cuenta para resolver un alta mal cargada.**

- [ ] Puedo editar nombre y email
- [ ] El email sigue sin poder repetirse
- [ ] La contraseña de otro usuario no se muestra ni se puede leer

### HU-USU-04 — Cambiar el rol de un usuario (Administrador)

**Como Administrador quiero cambiar el rol de otra cuenta para dar o quitar permisos sin tocar la base de datos.**

- [ ] Solo el Administrador puede cambiar roles
- [ ] Un Usuario que lo intente recibe un rechazo del servidor
- [ ] El cambio de rol se aplica en el siguiente inicio de sesión
- [ ] **No puedo cambiarme el rol a mí mismo**

### HU-USU-05 — Activar y desactivar un usuario (Administrador)

**Como Administrador quiero desactivar una cuenta para cortarle el acceso sin borrar sus datos.**

- [ ] Un usuario desactivado no puede iniciar sesión
- [ ] Desactivar no borra sus hábitos, registros ni rutinas
- [ ] Puedo reactivarlo
- [ ] **No puedo desactivarme a mí mismo**: el sistema nunca se queda sin administrador

---

## Requisitos de cátedra

Lo que pide la consigna y ningún usuario pide. No son historias: son **restricciones** que las historias de
arriba tienen que respetar. Esta tabla es además el borrador de la matriz de evidencias del `README.md`.

| ID | Requerimiento mínimo | Dónde se evidencia | Historias que lo cubren |
| :--- | :--- | :--- | :--- |
| `RT-01` | 6 tablas relacionadas, 1:N y N:M | 7 tablas; la N:M es `RutinaHabito` | `HU-RUT-01` … `HU-RUT-05` |
| `RT-02` | Login, roles, `[Authorize]` y avatar | JWT con rol; endpoints protegidos | `HU-ACC-01` … `HU-ACC-03`, `HU-PER-02`, `HU-USU-04` |
| `RT-03` | Manejo de archivos adicional al avatar | Imagen de portada del artículo | `HU-ART-01`, `HU-ART-02` |
| `RT-04` | Al menos un ABM completo por AJAX | Toda la aplicación es SPA; ningún CRUD recarga la página | Todos los módulos |
| `RT-05` | Paginado por servidor | Historial, artículos y usuarios | `HU-HIS-01`, `HU-ART-04`, `HU-USU-01` |
| `RT-06` | Búsqueda asíncrona de entidades relacionadas | Hábitos al armar una rutina | `HU-RUT-02` |
| `RT-07` | Web API REST con JWT | La SPA la consume con Bearer; colección exportada | Todos los módulos |

---

## Decisiones de la sesión

Lo que quedó cerrado, y por qué.

| # | Decisión | Razón |
| :--- | :--- | :--- |
| 1 | Las historias viven en este documento; los issues son el **cómo/cuándo** | Un dato en un solo lugar: las historias son el qué, estable; los issues son la fase, efímera |
| 2 | Actores = roles (`Visitante`, `Usuario`, `Administrador`), sin personas inventadas | Es lo que evalúa la cátedra y lo que restringe `[Authorize]` |
| 3 | Formato idéntico a `feature.yml` (rol, historia, criterios, fuera de alcance) | Derivar una historia a issue es copiar y pegar, sin reescribir criterios |
| 4 | Una historia = un objetivo de valor, no una pantalla ni una fase | "CRUD de rutinas" no es una historia: son cinco |
| 5 | Los requisitos de cátedra son restricciones (`RT-xx`), no historias | Un actor "cátedra" contamina el vocabulario de las historias |
| 6 | ~~Agrupación por módulo~~ — **superada por la #24** | El orden de este documento era el orden en que se encaran las cosas; la #24 lo reemplaza por el orden por dependencia de [orden-de-implementacion.md](./orden-de-implementacion.md) |
| 7 | Las rutinas tienen ABM completo en `/routines`, no solo un selector | Es donde el N:M se ve funcionando en la defensa |
| 8 | CRUD completo de usuarios por el Administrador | Decisión explícita del usuario, más amplia que la recomendación inicial |
| 9 | La abstinencia entra como `sí/no` con la semántica de "día limpio": tildar significa *hoy me mantuve* | Cuesta lo mismo que cualquier hábito. El contador en vivo se paga después: es un cálculo sobre el historial ya guardado, más una columna si algún día se quiere precisión en horas y minutos |
| 10 | Frecuencia: **todos los días** o **N veces por semana**, sin días fijos | Los 14 hábitos reales del usuario incluyen tres que no son diarios; los días fijos exigen mantener un calendario y no hay notificaciones que lo aprovechen |
| 11 | La cuota semanal es libre: la semana se cumple al llegar a la cuota, sin importar qué días | No reta por adelantado y no obliga a configurar un calendario |
| 12 | Un hábito semanal muestra `2 / 3 esta semana`, con días neutros | Sin eso, "3 veces por semana" se convierte en seis días de fracaso |
| 13 | Se puede corregir cualquier día de los últimos 7; más atrás, solo lectura | Un olvido a las 23:59 no debe costar el día, pero el backfill libre deja reescribir el pasado a gusto |
| 14 | Búsqueda asíncrona **solo donde hay volumen**: los hábitos al armar una rutina. Las categorías se eligen en desplegable, en todas las pantallas | Buscar tres categorías no demuestra el requisito; buscar entre 14 hábitos sí |
| 15 | `HABITO` suma `descripcion`, opcional | Los hábitos reales del usuario son ejercicios: sin instrucciones, el título no alcanza |
| 16 | Los hábitos `contador` llevan meta opcional; los `sí/no` no piden meta | Es la única forma de que "8 vasos" exista |
| 17 | Los artículos se leen con sesión iniciada; no hay catálogo público | La propuesta aprobada dice "lectura para usuarios autenticados" |
| 18 | **No hay racha.** Ni columna ni cálculo | Así lo pediste. Además evita definir qué la corta, que era la pregunta abierta más molesta |
| 19 | El Administrador asigna la contraseña inicial; no hay email en el sistema | Sin envío de correo, una invitación no existe |
| 20 | Nadie puede desactivarse ni bajarse el rol a sí mismo | Es la forma barata de que el sistema no se quede sin administrador |
| 21 | El listado de categorías **no** se pagina | Son pocas y la consigna exceptúa del paginado a las tablas maestras chicas; paginar acá es ceremonia sin problema detrás |
| 22 | La categoría de un hábito se elige en un **desplegable**, no con búsqueda asíncrona | La misma razón por la que no se pagina: si son pocas, buscar es teatro. La búsqueda asíncrona queda donde hay volumen real: los hábitos dentro de una rutina |
| 23 | La propuesta **se corrige mientras la ventana esté abierta**: hasta el `2026-10-15 23:59`. Después vuelve a quedar congelada | El usuario abrió la ventana: el documento se ajusta para que no prometa lo que ya no se hace, y a partir de esa hora es otra vez el registro de lo aprobado |
| 24 | El **orden de encare** vive en [orden-de-implementacion.md](./orden-de-implementacion.md), y **reemplaza a la #6**: los módulos se ordenan por dependencia real, no por el orden narrativo de este documento | Este documento agrupa por módulo para leerse de corrido; esa agrupación no dice qué bloquea a qué. El orden de encare se deduce de las FK y de las dependencias que las FK no muestran (categoría como clasificador compartido, infra de archivos, paginado, meta congelada). Separarlo deja las historias estables y el orden revisable |

## Preguntas abiertas

Ninguna. Las que había quedaron cerradas, y las que dependen de cómo se implementa algo se cierran en el
documento de feature del módulo que las toca (ver [orden-de-implementacion.md](./orden-de-implementacion.md),
sección 7).

Resueltas:

1. ~~**Los dos ajustes de `database.sql`**~~ — ya están escritos: `HABITO` tiene `descripcion VARCHAR(255)
   NULL`, `frecuencia_tipo ENUM('diaria','semanal')` y `frecuencia_veces TINYINT NULL` (1–6, solo cuando el
   tipo es `semanal`). No agregó tablas ni relaciones: el DER sigue con 7 tablas y la N:M intacta.
