# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Un solo usuario real, y hay que diseñar para él: la persona que registra **sus 14 hábitos personales** a lo
largo del día, **desde el celular que lleva encima**. El registro de un hábito es una acción de dos segundos
que interrumpe otra cosa —una clase, el trabajo, la calle— así que se hace de pie, con una sola mano y de
paso. Su estado de ánimo al llegar es "no quiero pensar", no "quiero explorar".

La computadora es la superficie que **se adapta después**, no el punto de partida: sirve para lo que la
pantalla chica hace mal — recorrer el historial largo, escribir un artículo, administrar usuarios.

El segundo rol es real y también decide pantallas: el **Administrador**, que cura categorías, publica
artículos y administra cuentas. Es la misma persona, pero en otro momento y con otra intención: mantener el
sistema, no usarlo.

Los tres actores que el sistema reconoce son `Visitante`, `Usuario` y `Administrador`. No hay personas
inventadas con nombre y apellido: el sistema tiene un humano detrás y los roles son los que restringe
`[Authorize]`.

La audiencia de esta entrega incluye además al **docente que evalúa** el cumplimiento de la consigna de
cátedra. No usa el producto como producto: lo abre, lo recorre y busca evidencia de los requisitos.

## Product Purpose

Javito es un seguimiento de hábitos personales en la web. Registra el día de cada hábito en un toque,
muestra la consistencia de la semana de un vistazo y no cobra ni limita nada.

Existe para resolver cinco fallas concretas de las herramientas que ya hay: fricción al registrar,
dependencia exclusiva del celular, cobros por funciones básicas, interfaces saturadas de ruido o de
mecánicas de juego, y la ausencia de fundamento sobre cómo se sostiene un hábito.

El éxito es que registrar un hábito cueste dos segundos y que, al mirar la pantalla del día, la persona
sepa si está sosteniendo el ritmo sin abrir un reporte.

## Positioning

Un seguimiento de hábitos **web, gratuito y sin racha**. El mecanismo que un vecino de categoría no podría
copiar honestamente es la combinación de:

- **Cupo semanal libre en vez de calendario fijo.** Un hábito es "todos los días" o "N veces por semana"
  (1–6). La semana se cumple al llegar a la cuota, sin importar qué días, y los días que faltan se
  muestran **neutros, nunca en rojo**. Sin eso, "3 veces por semana" se convierte en seis días de fracaso
  — que es exactamente lo que hacen las apps con streak.
- **Cero gamificación.** No hay vidas, oro, niveles, castigos ni racha. Ni columna ni cálculo.
- **Sin límites artificiales**: ni cantidad de hábitos, ni extensión del historial.

## Operating Context

- **Mobile-first.** El marco que manda es la pantalla del celular, en la mano y a una sola mano: todo se
  compone primero en vertical y a ~390px de ancho, y la computadora es la adaptación. No es una app
  instalada: es una aplicación web.
- El registro es una interrupción corta, no una sesión, y compite contra lo que la persona estaba haciendo.
- La semana arranca el **lunes** y cierra el **domingo**. Toda fecha de registro es un día calendario, sin
  hora.
- Un hábito se registra **una vez por día**: los registros del mismo día acumulan valor, no se duplican.
- Se puede registrar y corregir **cualquier día de los últimos 7**; más atrás, el registro es de solo
  lectura.
- Dos roles en uso: el mismo humano como `Usuario` (sus hábitos, registros, rutinas, perfil, lectura de
  artículos) y como `Administrador` (categorías, artículos, usuarios).
- Evaluación de cátedra: una entrega única, monorepo, todo en `localhost`, sin despliegue.
- Los artículos se leen **con sesión iniciada**; no hay catálogo público.
- Un hábito se organiza en **rutinas** (bloques por momento del día, ej. `Mañana`), que son N:M con el
  hábito y llevan orden propio.

## Capabilities and Constraints

Confirmado y en alcance: autenticación con JWT y roles; registro público; CRUD de usuarios por el
Administrador; CRUD de categorías (baja lógica); CRUD de hábitos con archivado; CRUD de rutinas con orden y
filtro; registro diario rápido (sí/no, contador con `+`/`−` y notas) con anulación del día; artículos en
Markdown con imagen de portada; cuadrícula de 7 días por tarjeta y detalle histórico de 30/60/365 días;
paginado en servidor (historial, artículos, usuarios) y búsqueda asíncrona en un solo lugar: **agregar
hábitos a una rutina**. Las categorías se eligen siempre en desplegable; son pocas y no se paginan ni se
buscan.

Tres tipos de hábito: `sí/no`, `contador` (con unidad y meta del día) y `abstinencia`. La abstinencia se
marca igual que un `sí/no`, con otra semántica: tildar significa *hoy me mantuve*.

Fuera de alcance, y no debe insinuarse en la interfaz: offline, app móvil nativa, integración con
calendarios externos, notificaciones push, temporizadores en tiempo real, días fijos de la semana,
recordatorios, hábitos compartidos, borrado definitivo desde la interfaz, verificación por email,
recuperación de contraseña, login con Google, y el contador en vivo de abstinencia (días, horas y minutos)
con su botón de recaída.

Vinculante: es una **aplicación web mobile-first** — se compone primero para la pantalla del celular en el
navegador. No hay app nativa ni envoltorio nativo, y eso no la vuelve una aplicación de escritorio.

Restricciones técnicas vinculantes: WebAPI ASP.NET Core (.NET 10) y SPA SvelteKit 3 con `adapter-static`;
acceso a datos con ADO.NET puro y SQL a mano, **sin ORM**; MySQL 8.0; la meta de un registro queda fija en
la fila para no reescribir el historial cuando el usuario cambia su meta después.

Terminología en español en el contrato JSON de la API (`estado`, `servicio`) y en los nombres de prueba, por
coherencia con el dominio en español del DER; identificadores de framework en inglés.

## Brand Commitments

El nombre es **Javito**.

El idioma del producto es español rioplatense neutro en la interfaz, con voseo donde suena natural
("Levantá el backend"). Los textos no prometen nada que el producto no haga.

**La identidad visual ya está comprometida en código y se hereda, no se reemplaza:** los temas Rosé Pine
(Dawn claro, OLED oscuro) y el mapeo semántico de shadcn viven en
`frontend/web/src/routes/layout.css`, que es la autoridad. Los componentes vienen de shadcn-svelte
vendorizado en `frontend/web/src/lib/componentes/ui/` (todavía vacío) y los íconos son Lucide, un solo pack,
sin shim ni alias. No se usan colores sueltos de Tailwind: se usan los tokens semánticos. No hay modo claro
u oscuro por defecto: `mode-watcher` sigue al sistema y lo persiste.

## Evidence on Hand

**Real:** los 14 hábitos que la persona usa hoy, con su frecuencia. Es la evidencia dura de densidad,
tipos y contenido:

| Hábito | Frecuencia |
| :--- | :--- |
| Liberación de cuello y hombros | diaria |
| Estiramiento de muñeca y antebrazo | diaria |
| Extensión de espalda de pie | diaria |
| Caminata corta por la habitación | diaria |
| Respiración cuadrada | diaria |
| Descanso visual 20-20-20 | diaria |
| Anclaje sensorial 5-4-3-2-1 | diaria |
| Reset mental y reflexión | diaria |
| Hidratación con agua | diaria |
| Té de hierbas consciente | diaria |
| Meditar | diaria |
| Ejercicio pesado | 3 veces por semana |
| Practicar dibujo | 2 veces por semana |
| Practicar cocina | 1 vez por semana |

Once diarios y tres semanales. Esto define el rango real: **14 hábitos activos en una sola pantalla, 11
de ellos todos los días y ninguno con un día fijo.**

**Ausencias que no se deben inventar:**

- **Ningún hábito `contador` y ninguno de `abstinencia` en uso real.** Los dos tipos se implementan y se
  diseñan, pero son el camino menos ejercitado: no pueden dominar la tarjeta ni la pantalla. El caso
  canónico del contador (`8 vasos`) es ilustrativo, no evidencia.
- El usuario autorizó **agregar hábitos ilustrativos** si hace falta cubrir formas que su lista no tiene.
- No hay testimonios, usuarios, métricas, prensa, precios ni clientes. No hay artículos escritos: el módulo
  de artículos existe por requisito de cátedra y todavía no tiene contenido.
- No hay imágenes ni assets propios más allá del favicon.
- La WebAPI expone hoy un solo endpoint: `GET /api/health`.

## Product Principles

1. **Registrar primero, decorar después.** Cualquier decisión que agregue un paso al marcado del día está
   mal, aunque se vea mejor.
2. **Neutro no es fracaso.** Lo que no se cumplió todavía se muestra en neutro. El rojo se reserva para lo
   que de verdad salió mal.
3. **El historial es intocable.** Los días ya registrados conservan la meta con la que se cerraron. Un
   cambio de configuración rige desde hoy, nunca hacia atrás.
4. **La interfaz no reta.** Sin racha, sin castigo, sin culpa. La app informa el ritmo; el juicio no es de
   ella.
5. **Familiaridad ganada sobre sorpresa.** Es una superficie de tarea: el instrumento desaparece en el
   trabajo. La personalidad vive en los detalles precisos, no en reinventar controles.

## Accessibility & Inclusion

- Un día cumplido, uno vacío y uno neutro deben distinguirse **sin depender solo del color**: el estado
  lleva forma, ícono o marca además del tono. No es opcional: es lo que sostiene la lectura de la
  cuadrícula semanal y del mapa histórico.
- Registro con teclado: `+` y `−` de un contador no pueden exigir escribir números a mano.
- Modo claro y oscuro siguen la preferencia del sistema operativo.
- No hay requisito de estándar formal declarado (nivel WCAG) ni de internacionalización: el producto es
  monolingüe en español.
