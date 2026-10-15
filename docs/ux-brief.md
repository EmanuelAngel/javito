# Brief de UX — Javito

> Salido de `impeccable shape` sobre [`docs/proyecto/propuesta.md`](./proyecto/propuesta.md) y
> [`docs/proyecto/historias-de-usuario.md`](./proyecto/historias-de-usuario.md), el 2026-10-15.
>
> **Qué es:** el brief de diseño confirmado. Define quién llega, qué tiene que pasar, cómo se compone cada
> superficie y qué no se debe inventar. **Qué no es:** no es código, no es el contrato de dirección de
> `impeccable` (ese vive en el surface brief), y no reemplaza a la propuesta ni a las historias de usuario.
> La verdad de producto vive en [`PRODUCT.md`](../PRODUCT.md).

---

## 1. Quién llega y para qué

Una persona sola con **14 hábitos personales**, de pie, con una mano, en el celular, interrumpiendo otra
cosa —una clase, el trabajo, la calle—. El registro tiene que costarle **dos segundos**.

El **Administrador** es la misma persona en otro momento: mantener el sistema, no usarlo. Y el **docente
que evalúa** entra a buscar evidencia de los requisitos de cátedra, no a usar el producto.

Javito es **mobile-first**: 390px es el marco de trabajo y la computadora es la adaptación.

## 2. Resultado y prueba

El éxito es que marcar un hábito cueste dos segundos y que, al mirar la pantalla, la persona sepa si está
sosteniendo el ritmo sin abrir un reporte.

La prueba real es la lista de 14 hábitos del usuario: **11 diarios y 3 semanales** (`Ejercicio pesado` 3
veces por semana, `Practicar dibujo` 2, `Practicar cocina` 1). No hay **ningún** hábito de tipo contador ni
de abstinencia en uso: los dos se implementan, pero no pueden dominar la pantalla ni la tarjeta.

## 3. Dirección elegida

- **Autoridad visual: la existente, heredada.** Rosé Pine Dawn (claro) y OLED (oscuro) + shadcn-svelte +
  Lucide + Inter / JetBrains Mono, con `frontend/web/src/routes/layout.css` como autoridad.
  **No se reemplaza ni se re-deriva.**
- **Tesis estructural:** mobile-first de verdad. Todo se compone primero en vertical y a ~390px.
- **Armazón fijo:** barra superior compacta (marca + avatar) y barra inferior de cinco destinos:
  `Hoy · Hábitos · Rutinas · Artículos · Perfil`. Admin **no** es un sexto tab: vive dentro de `/perfil`,
  porque el rol es raro y la barra es para el trabajo diario.
- **Composición de `/today`: la tira de rutinas como espina del día.** Una rutina a la vez, con su propio
  progreso visible, y el toque de deselección para llegar a `Sin rutina`.
- **Momento focal:** la fila del hábito con su control de marcado. Es donde se gana o se pierde todo lo
  demás.

### El estado `Sin rutina` (decisión del usuario)

Un hábito recién creado **nace sin rutina** (`HU-HAB-01`), así que en una composición de una-rutina-a-la-vez
quedaría invisible. Se resuelve así:

1. **Se llega deseleccionando la parada activa** de la tira: tocarla de nuevo la apaga.
2. **El estado lleva nombre en el panel**, no en la tira: el encabezado dice `Sin rutina · 2`.
3. **Se llega solo en el momento que importa:** al crear un hábito sin rutina, la pantalla queda
   deseleccionada con el hábito nuevo en la primera fila.
4. **No persiste al recargar.** Nadie abre Javito y aterriza en un estado donde la tira no resalta nada.

## 4. Mapa de superficies

La §3.5 de la propuesta lista 10 vistas. Este mapa las corrige.

| Ruta | Qué es | Modo |
| :--- | :--- | :--- |
| `/login` | Entrar | Operate |
| `/registro` | Crear cuenta | Operate |
| `/today` | Hoy: la tira de rutinas y el día | Operate |
| `/habits` | Mis hábitos, activos y archivados | Operate |
| `/habits/[id]` | Detalle: cuadrícula 30/60/365, totales, editar, archivar | Operate |
| `/historial` | Registros paginados con filtro de hábito y fecha | Operate |
| `/routines` | Rutinas: ABM, orden y búsqueda asíncrona de hábitos | Operate |
| `/articles` | Catálogo paginado | Read |
| `/articles/[id]` | Leer un artículo | Read |
| `/articles/nuevo` · `/articles/[id]/editar` | Publicar y corregir (Admin) | Operate |
| `/admin/categorias` | ABM de categorías (Admin) | Operate |
| `/admin/usuarios` | Listado, alta, rol y activación (Admin) | Operate |
| `/perfil` | Datos, avatar, tema, salir, y las entradas de Admin | Operate |

**Superficies nuevas contra la §3.5:** `/login`, `/registro`, `/historial` y la **edición** de artículo.
La §3.5 no listaba las tres primeras y solo tenía `/articles/nuevo`, aunque `HU-ART-02` pide editar.

**No hay superficie Persuade.** Javito no tiene landing, ni marketing, ni página de precios.

### Decisiones por superficie que el build no debe inventar

- **`/habits/[id]` es la superficie de corrección.** La cuadrícula de 30/60/365 **es** el editor: tocar un
  día de los últimos 7 lo corrige, con las mismas reglas que el día de hoy; más atrás es solo lectura. Eso
  resuelve `HU-REG-03` sin inventar una pantalla aparte.
- **`/today` no corrige días pasados.** Marca hoy rápido; el pasado se corrige en el detalle, donde la
  cuadrícula ya lo muestra.
- **Reordenar en `/routines` con botones de subir/bajar en un modo de orden**, no con arrastre: el arrastre
  táctil en listas largas es una trampa en el celular.
- **`/habits`:** el listado separa activos de archivados sin que los archivados ensucien la vista
  principal, y cada fila muestra categoría y frecuencia.
- **`/routines`:** el alta de hábitos a una rutina es el **único** lugar con búsqueda asíncrona del
  sistema. Las categorías se eligen siempre en desplegable y nunca se buscan ni se paginan.

## 5. Estados que importan

- **Vacío real:** cuenta recién creada, cero hábitos. `/today` tiene que enseñar qué hacer, no mostrar un
  panel en blanco.
- **`Sin rutina`:** el estado deseleccionado de la tira (ver §3).
- **Un hábito nuevo** aparece al instante y sin rutina.
- **Falla de guardado:** la fila no puede quedar mostrando un estado falso (`HU-REG-01`). Actualización
  optimista con vuelta atrás y aviso en la fila.
- **Token vencido:** volver al login, nunca a una pantalla vacía (`HU-ACC-02`).
- **Semanal cumplido** y **semanal pasado de cuota:** ninguno de los dos se pinta de rojo, y pasarse no
  rompe nada (`HU-REG-06`).
- **Rangos realistas:** 14 hábitos activos es el caso típico (1 vacío, ~30 techo); 3 rutinas (1 a 6);
  artículos hoy en cero; historial de más de un año, siempre paginado en el servidor.

## 6. Anti-objetivos

Sin racha, sin confeti, sin niveles, sin castigo, y **sin rojo por lo que todavía no se cumplió**. Sin
modales como primera opción. Sin inventar navegación nueva. Sin colores sueltos de Tailwind: solo tokens.
Sin reemplazar Rosé Pine. Un cambio de configuración rige desde hoy: **el historial no se reescribe**.

## 7. Restricciones

`adapter-static` (SPA que consume la WebAPI con Bearer), objetivos táctiles de **44px** mínimo, nada depende
de `:hover`, cumplido/vacío/neutro se distinguen **sin depender solo del color**, tema claro/oscuro con
`mode-watcher`, shadcn-svelte vendorizado que no se reformatea a mano, un solo pack de íconos, e interfaz en
español.

## 8. Decisiones abiertas — no las inventes

1. **Orden de las rutinas en la tira.** El DER le da orden a `RUTINA_HABITO`, pero **no a `RUTINA`**: la
   tira necesita uno. O se ordena por creación, o `RUTINA` suma una columna.
2. **Qué rutina queda seleccionada al abrir:** la primera, o la que corresponde al momento del día.
3. **Dónde vive el toggle de tema.** En los sketches quedó dentro de `/perfil` (la barra superior es marca
   + avatar). Hay que confirmarlo.
4. **Si `/historial` merece ruta propia** o es una pestaña dentro de `/habits`.
