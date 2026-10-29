# Plan de cierre — del 29/10 al 05/11

Este documento responde **una** pregunta: *qué se hace cada día hasta la entrega*. No reemplaza al
[orden de implementación](./orden-de-implementacion.md), que responde otra: *en qué orden se construye
el producto*. Cuando los dos se contradicen, acá manda éste, porque tiene fecha encima.

Base: [consigna final](../consignas/consigna-final.md) (entrega `2026-11-05`), capacidad declarada por
el autor: **6-8 h por día**. Las estimaciones de horas son estimaciones, no mediciones: se corrigen al
cerrar cada día, y el mecanismo de corrección está en §6.

---

## 1. El presupuesto real

**El 05/11 no es un día de código.** Ese día se entrega el enlace, se completa la matriz de evidencias y
se graban las capturas. Días de código: **7** (29/10 al 04/11).

| Concepto | Horas |
| :--- | --: |
| Código, 7 días × 7 h | **49** |
| Evidencias (capturas, GIF, DER en imagen, README) | 4 |
| **Total disponible** | **49** (las 4 de evidencias salen de ahí) |

---

## 2. Los 7 requisitos, y qué historia mínima los cierra

La consigna pide una **matriz de evidencias** en el README con estos siete puntos. La columna
"historia mínima" es lo más barato que los cierra de verdad —no lo que la propuesta prometió, sino lo
que la cátedra exige:

| Requisito | Lo que pide la consigna | Historia mínima | Horas |
| :--- | :--- | :--- | --: |
| `RT-07` | Endpoints Web API con JWT, consumidos con `Bearer` | `ACC-02` | — (viene con la Fundación) |
| `RT-02` | Login/Logout, seguridad de roles, **una** funcionalidad restringida por rol, avatar | `ACC-02`, `ACC-03`, `CAT-*` (rol), `PER-02` | — |
| `RT-04` | Un ABM completo por AJAX, sin recargar | `CAT-01` → `CAT-04` | 6 |
| `RT-03` | Archivos en el servidor, **adicional al avatar** | `ART-01` (portada) | 3 |
| `RT-05` | Paginado por servidor en los listados principales | `ART-04` | 4 |
| `RT-01` | 6 tablas relacionadas, con 1:N y **N:M** | `RUT-01`, `RUT-02` (la N:M `rutina_habito`) | 6 |
| `RT-06` | Búsqueda asíncrona de entidades relacionadas (prohibido llenar un `<select>` con todo) | `RUT-02` (buscador de hábitos) | — (misma historia) |

Dos correcciones que importan para el calendario:

- **`RT-02` no pide cambio de rol.** Pide login, logout, roles, una funcionalidad restringida y avatar.
  `HU-USU-04` (cambio de rol) es un requisito **propio** de Javito, no de la cátedra: son ~2 h que no
  hacen falta para cumplir, y por eso quedan fuera salvo que sobre tiempo.
- **`RT-05` no pide tres listados.** Pide que los listados principales estén paginados. Si el único
  listado principal que se entrega es el catálogo, `ART-04` alcanza. Las tres listas son la vara de la
  [propuesta](./propuesta.md), más alta que la de la cátedra.
- **`RT-01` y `RT-06` los cierra el mismo módulo.** Rutinas son 6 h y pagan dos requisitos: es el mejor
  retorno por hora del cronograma. La contra: son 6 h para un módulo que no es el corazón del producto.

### La barra de cada condición

| Condición | Barra de la consigna | Requisitos |
| :--- | :--- | :--- |
| **Regularizar** | Matriz de evidencias completada hasta el estado actual, **al menos 70% de los puntos** | **5 de 7** |
| **Promocionar** | Todo al 100%, script SQL con datos y credenciales por rol | **7 de 7** |

Las dos últimas piezas de promoción **ya están hechas**: `database.sql` trae datos de prueba y el README
documenta las credenciales de los cuatro usuarios. O sea: promocionar no depende de trabajo extra, depende
de cerrar los 7 requisitos.

Y el atajo que eso habilita, que conviene tener decidido de antemano: **si se cae Rutinas, quedan 5 de 7 y
la entrega regulariza igual**. Es el único corte que no rompe nada del resto del sistema.

---

## 3. Las puertas

Tres cortes con fecha. Si una puerta no cierra, se aplica el recorte de §6 **ese mismo día**, no el 4/11.

| Puerta | Cuándo | Tiene que estar cerrado | Si no cierra |
| :--- | :--- | :--- | :--- |
| **1** | sáb 31/10 | Fundación + Acceso + ABM de Categorías → **`RT-07`, `RT-02` (login y roles), `RT-04`** | Se recorta producto, no arquitectura: sin login no hay nada |
| **2** | lun 02/11 | Hábitos + Registros + avatar → **`RT-02` completo** | Se cae Artículos entero y se replanifica a 5/7 |
| **3** | mié 04/11 | Artículos (archivos + paginado) + Rutinas → **`RT-01`, `RT-03`, `RT-05`, `RT-06`** | Se cae Rutinas: entrega para regularizar con 5/7 |
| **4** | jue 05/11 | Sin código: capturas, matriz de evidencias, DER, entrega del enlace | — |

```mermaid
gantt
    dateFormat YYYY-MM-DD
    axisFormat %d/%m
    title Del 29/10 al 05/11

    section Puerta 1
    Fundación y login (ACC-02)      :a1, 2026-10-29, 2d
    Categorías, ABM por AJAX        :a2, 2026-10-31, 1d

    section Puerta 2
    Hábitos y registros del día     :b1, 2026-11-01, 1d
    Avatar e infraestructura        :b2, 2026-11-02, 1d

    section Puerta 3
    Artículos y paginado            :c1, 2026-11-03, 1d
    Rutinas (N:M y búsqueda)        :c2, 2026-11-04, 1d

    section Entrega
    Evidencias y matriz del README   :d1, 2026-11-05, 1d
```

---

## 4. Día por día

| Día | Fecha | Horas | Qué se hace | Se cierra |
| :--- | :--- | --: | :--- | :--- |
| 1 | jue 29/10 | 7 | Conexión MySQL y patrón de repositorio, contrato de errores de la API, DTOs. Endpoint de humo con un dato real | — |
| 2 | vie 30/10 | 7 | `ACC-02`: JWT, guard de rutas en la SPA, cliente HTTP con `Bearer`, shell con barra superior | **`RT-07`** |
| 3 | sáb 31/10 | 7 | `ACC-03` (logout) y `CAT-01`…`CAT-04`: el ABM completo por AJAX | **Puerta 1** · `RT-04` |
| 4 | dom 01/11 | 7 | `HAB-01`, `HAB-02` y `REG-01` (marcar el día) | — |
| 5 | lun 02/11 | 7 | `REG-04` (la semana de un vistazo), `PER-01` y `PER-02` (avatar + infra de archivos) | **Puerta 2** · `RT-02` |
| 6 | mar 03/11 | 7 | `ART-01` (portada, reusa la infra de archivos) y `ART-04` (catálogo con paginado de servidor) | `RT-03`, `RT-05` |
| 7 | mié 04/11 | 7 | `RUT-01` y `RUT-02`: la N:M y el buscador asíncrono de hábitos | **Puerta 3** · `RT-01`, `RT-06` |
| 8 | jue 05/11 | — | Capturas y GIF por requisito, matriz de evidencias en el README, DER en imagen, enlace | **Entrega** |

**Qué queda afuera de esos 8 días, y no es opcional:** `ACC-01` (registro), `HAB-03`/`04`/`05`,
`REG-02`/`03`/`05`/`06`, `RUT-03`/`04`/`05`, `ART-02`/`03`/`05`, `HIS-01`/`02`, `USU-01`…`USU-05`.
Son **21 de las 37 historias**.

---

## 5. La respuesta a "70% de producto"

El objetivo declarado era *todos los requisitos + 70% de producto*. Los números:

| Escenario | Historias | % de las 37 | Horas |
| :--- | --: | --: | --: |
| Los 7 requisitos, sin nada más | **16** | **43%** | ~52 |
| Los 7 requisitos + las 21 restantes | 37 | 100% | ~88 |

**El 70% no entra: los requisitos solos ya se llevan el 43% del producto y todo el calendario.** La brecha
entre "cumplir" y "el producto que imaginaste" son ~35 horas más, o sea cinco días que no existen. El
corte real no está entre 43% y 70%, está entre **cerrar los 7** y **cerrar 5**.

---

## 6. La brecha, y cómo se cierra

Suma de todo lo que hay que construir, estimado en horas de trabajo efectivo:

| Bloque | Historias | Horas |
| :--- | :--- | --: |
| Fundación + `ACC-02` | `ACC-02` | 12 |
| Acceso | `ACC-03` | 0.5 |
| Categorías (`RT-04`) | `CAT-01`…`04` | 6 |
| Hábitos | `HAB-01`, `02` | 5 |
| Registros | `REG-01`, `04` | 7 |
| Perfil (`RT-02`) | `PER-01`, `02` | 5 |
| Artículos: archivos (`RT-03`) | `ART-01` | 3 |
| Artículos: paginado (`RT-05`) | `ART-04` | 4 |
| Rutinas (`RT-01`, `RT-06`) | `RUT-01`, `02` | 6 |
| Evidencias del README | — | 4 |
| **Total** | **16** | **52.5** |
| **Disponible** | | **49** |
| **Brecha** | | **−3.5** |

Faltan ~3 h. Dos palancas, y hay que elegir una **antes del 31/10**, no cuando apriete:

| Palanca | Qué cuesta | Qué gana |
| :--- | :--- | :--- |
| **A · Subir el sábado y el domingo a 8 h** y sumar 2 h el 05/11 a la mañana | Menos descanso en el tramo medio | ~4 h · los 7 requisitos quedan al alcance |
| **B · Planificar a 5/7 y sacar Rutinas** | Se pierde `RT-01` y `RT-06`: no hay promoción | ~6 h de aire y entrega tranquila |

Recomendación: **irse por A hasta la Puerta 2**. Si el lunes 02/11 a la noche la Puerta 2 no cerró,
se cambia a B en ese momento, y el miércoles a la noche se entrega para regularizar sin drama. Lo que no
se hace es llegar al 04/11 sin haber decidido.

**Regla de corrección:** al cerrar cada día se anota lo hecho contra lo estimado. Una desviación de más
de 2 h en un día obliga a recortar **ese mismo día**, del final de la lista: primero
`RUT-05`, después `ART-05`, después Rutinas completa.

---

## 7. Las evidencias no son trámite

La matriz del README exige, por requisito, enlace al código **y** evidencia visual. Se graba todo el
05/11 en una sola pasada, con la aplicación ya funcionando:

| Requisito | Qué hay que capturar |
| :--- | :--- |
| `RT-01` | El DER y la tabla `rutina_habito` con filas |
| `RT-02` | Login, perfil con avatar visible, y una pantalla de Categorías abierta con rol `Usuario` que recibe 403 |
| `RT-03` | Subida de una portada de artículo y el archivo en `wwwroot/uploads/` |
| `RT-04` | GIF del ABM de categorías completo (alta, edición, baja, listado) sin recargar |
| `RT-05` | Inspector de red mostrando `?pagina=2` y la respuesta con solo esa página |
| `RT-06` | GIF del buscador de hábitos filtrando mientras se escribe |
| `RT-07` | Postman o Bruno con `Bearer` devolviendo 200 y, sin el token, 401 |

Si un requisito no tiene captura, la matriz miente y la defensa lo muestra. Por eso la evidencia se graba
**cuando el requisito se cierra**, no todos juntos el último día: un GIF por puerta cerrada vale más que
siete el 05/11 a las 23:00.

---

## 8. Riesgos con nombre propio

1. **La Fundación es la estimación menos confiable.** Doce horas para repositorios, contrato de errores,
   DTOs, JWT, guard y shell es optimista. Si el jueves a la noche no hay un endpoint devolviendo un dato
   real desde MySQL, la Puerta 1 se corre un día y hay que aplicar §6 ese mismo jueves.
2. **Archivos tiene dos consumidores** (`PER-02` y `ART-01`) y una sola implementación válida: si la
   subida y la validación se escriben dos veces, se pierden 4 h y probablemente `RT-03`.
3. **El paginado también**: `ART-04` fija el helper. Escribirlo pensando en que después lo usen
   Historial y Usuarios no cuesta nada; reescribirlo cuesta 3 h.
4. **La defensa es después del cierre, y solo para promocionar.** No hay que ensayar el 05/11: hay que
   entregar. Pero el README se lee en voz alta en esa defensa, así que la matriz se escribe para ser
   leída, no para cumplir.
