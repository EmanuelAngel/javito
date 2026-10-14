# Metodología de issue-tracking

Cómo se planifica, se trackea y se cierra el trabajo de Javito. Kanban sobre GitHub Issues y GitHub
Projects, para un único integrante.

## Los tres niveles

| Nivel | Primitiva de GitHub | Regla |
| :--- | :--- | :--- |
| **Epic = Fase** | Milestone | El milestone es la épica. Su barra de progreso es el avance de la fase. |
| **Feature / Task / Bug** | Issue | Un issue = un card del Kanban = una unidad de trabajo. |
| **Sub-task** | Sub-issue | Sólo si necesita seguimiento propio. Si no, checklist en el body. |

Las fases `S0`…`S12` ya están definidas con fechas y entregables en
[el cronograma](../proyecto/cronograma.md). Esos son los milestones: **no se inventa una capa nueva
de épicas por encima**.

`M0` (propuesta aprobada) ya pasó. `M1` (entrega final) no lleva milestone propia: la fecha de `S12`
**es** el `2026-11-05`. `M2` (defensa oral) es un issue `type:task` con checklist, porque la fecha
todavía no está confirmada.

## La regla que ordena todo

> **Labels = qué es. Campos del proyecto = cómo se gestiona.**

Un dato vive en un solo lugar. Nada se duplica entre labels y campos, porque un dato en dos lados es
un dato en el que ya no se puede confiar.

### Labels

Exactamente **un** label de tipo por issue (lo pone el template, no hay que acordarse) y **un** label
de área.

| Label | Eje | Significado |
| :--- | :--- | :--- |
| `type:feature` | Tipo | Funcionalidad visible para el usuario. |
| `type:task` | Tipo | Trabajo técnico o de soporte: CI, migración, tooling, datos de prueba. |
| `type:bug` | Tipo | Algo que debería funcionar de cierta manera y no lo hace. |
| `api` | Área | `backend/` |
| `web` | Área | `frontend/web/` |
| `db` | Área | `database.sql` |
| `docs` | Área | `docs/`, README |

Los colores salen de Rosé Pine Dawn, la misma paleta que usa la SPA.

**No existe `type:subtask`.** Si es un sub-issue, la jerarquía ya lo dice. Duplicar el mismo dato en
dos ejes es la forma más rápida de que ninguno de los dos sea confiable.

### Campos del proyecto

| Campo | Valores | Para qué |
| :--- | :--- | :--- |
| `Status` | `Backlog` · `Ready` · `In progress` · `In review` · `Done` | Las columnas del Kanban. |
| `Priority` | `P0` · `P1` · `P2` | Ordenar el tablero. Un campo ordena; un label, no. |
| `Size` | `S` · `M` · `L` | Cuánto pesa el PR. |

- `P0` bloquea la entrega o bloquea a otro issue · `P1` lo pide la consigna o el cronograma de la
  fase · `P2` es mejora, no requerido.
- `S` ≤150 líneas ponderadas · `M` ≤400 · `L` ≤600.

## Reglas duras

1. **Nada de trabajo sin issue.** Apareció algo en el camino: issue nuevo, aunque sea `P0` y lo
   resuelvas en el momento. Sin esto el tablero deja de contar la historia, y un tablero que miente es
   peor que no tener tablero.
2. **WIP = 1 en `In progress`.** Es la regla que hace funcionar a Kanban. Solo, sin límite, terminás
   con siete cards abiertas y ninguna cerrada.
3. **Un issue = una rama = un PR.** De ahí sale la trazabilidad completa, gratis.
4. **Un PR no cruza el límite de una fase.** Techo más fuerte y más útil que el conteo de líneas.
5. **Presupuesto del PR: `código + tests/4 ≤ 600`** líneas no-vendor. Cuatro líneas de test valen una
   de código. Es un **techo de seguridad, no un objetivo**: el riesgo acá no es que nadie te revise el
   PR, es que en dos días no te acordés qué tocaba ese diff.
6. **El área va en el título.** `feat(auth): ...`, `chore(api): ...`, `fix(web): ...` — el mismo
   prefijo que el commit. El label de área queda para filtrar en el tablero.

## Definition of Done

No hay reviewer que te frene, así que el DoD **es** la barrera:

- `pnpm check` y `pnpm lint` verdes
- Cada regla nueva llegó con su prueba, y viste RED antes del fix
- Docs y README actualizados si cambió el contrato de la API
- Los criterios de aceptación del issue, tildados
- Commit convencional en español, sin atribución de IA
- El issue lo cierra el PR, con `Closes #N` en el body

## El flujo: del issue al doc de `odd/`

```txt
Planificación        docs/proyecto/cronograma.md
      ↓
Issue detallado      gh issue create   ← el contrato: qué, por qué, criterios de aceptación
      ↓
odd/tasks/<f>.md     ← derivado: Specs, Tasks, Log con evidencia
      ↓
Implementación → PR → merge → Done
```

> **El issue es la fuente. El doc de `odd/` es derivado. Si se contradicen, gana el issue.**

Trabajo chico: **sólo issue**, el doc no nace. Trabajo grande: issue primero, y el doc se deriva de
él. El doc nunca agrega requisitos que el issue no tiene: los **descompone**.

## Comandos

```sh
# Crear
gh issue create --template feature.yml
gh issue create --template bug.yml --title 'fix(api): ...'

# Leer (los labels vienen en el JSON, sin GraphQL)
gh issue list --state open --json number,title,labels
gh issue view 12

# Vincular una sub-task: los node IDs salen de acá, sin GraphQL
gh issue view 12 --json id --jq .id
gh api graphql -f query='mutation{ addSubIssue(input:{issueId:"PADRE", subIssueId:"HIJO"}){ subIssue{ number } } }'

# Cerrar una fase
gh api --method PATCH repos/EmanuelAngel/javito/milestones/<n> -f state=closed
```

## Por qué no hay Issue Types nativos

GitHub tiene tipos nativos (`Epic`, `Feature`, `Task`, `Bug`) **sólo para organizaciones**.
`EmanuelAngel` es una cuenta personal, y se verifica así:

```txt
repository(owner:"EmanuelAngel", name:"pomody") { issueTypes { totalCount } }  → null
repository(owner:"github",       name:"docs")   { issueTypes { totalCount } }  → 7
```

Devuelve `null`, no una lista vacía: la capacidad no está atada al dueño usuario.

Igual, los labels son la mejor opción acá, no un premio consuelo: `gh issue view --json` **no expone
`issueType`** en su lista de campos, así que un tipo nativo exigiría GraphQL en cada lectura. Los
labels salen gratis en el JSON, y un subagente los lee sin ceremonia.

Un día, si el repo pasa a una organización, los labels se apagan y los tipos nativos se encienden con
los **mismos nombres**. Por eso el label es `type:feature` y no `type:story`.

La única debilidad: los labels no restringen nada — nada impide poner dos `type:*` en el mismo issue.
Lo tapa el hecho de que cada template pre-carga el suyo. Si igual empezás a desviarte, la barrera es una
GitHub Action que valide "exactamente un `type:*`".
