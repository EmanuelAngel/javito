using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Los hábitos del usuario y sus marcas. El registro es un recurso con identidad (hábito, fecha), así
/// que se escribe con PUT —crea o reemplaza, sin contar deltas— y se desmarca con DELETE, porque «sin
/// marcar» es la ausencia de la fila.
/// </summary>
[ApiController]
[Authorize]
[Route("api/habitos")]
public class HabitosController(
    IHabitoRepositorio repositorio,
    ICategoriaRepositorio categorias) : ControllerBase
{
    /// <summary>El valor más alto que admite la columna: DECIMAL(8, 2).</summary>
    private const decimal ValorMaximo = 999999.99m;

    /// <summary>La cuota semanal va de 1 a 6: siete días son la frecuencia diaria.</summary>
    private const int CuotaMaxima = 6;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<HabitoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] bool? activo,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var habitos = await repositorio.ListarAsync(usuarioId, activo, q, cancellationToken);

        return Ok(habitos.Select(MapeoDeHabitos.ADto).ToList());
    }

    [HttpPost]
    [ProducesResponseType<HabitoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearAsync(
        [FromBody] HabitoEntradaDto entrada,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var (habito, error) = await ValidarAsync(entrada, existente: null, cancellationToken);

        if (error is not null)
        {
            return error;
        }

        habito!.UsuarioId = usuarioId;
        habito.Id = await repositorio.CrearAsync(habito, cancellationToken);

        return Created($"/api/habitos/{habito.Id}", MapeoDeHabitos.ADto(habito));
    }

    /// <summary>
    /// Corrige un hábito sin tocar lo registrado: la meta nueva rige desde la próxima marca y los días
    /// ya cerrados conservan la suya, porque viven en <c>registro_habito.meta_objetivo</c> (S2, L3). El
    /// tipo se puede cambiar y las marcas anteriores quedan (S3). Un hábito ajeno no existe (S4) y un
    /// archivado también se edita (L5).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<HabitoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditarAsync(
        int id,
        [FromBody] HabitoEntradaDto entrada,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        // La pertenencia se pregunta primero: un hábito ajeno no existe para quien pregunta, y el
        // 404 no depende de que el cuerpo sea válido.
        var existente = await repositorio.BuscarAsync(id, usuarioId, cancellationToken);

        if (existente is null)
        {
            return NoEncontrado(id);
        }

        var (habito, error) = await ValidarAsync(entrada, existente, cancellationToken);

        if (error is not null)
        {
            return error;
        }

        // La edición no cambia de dueño ni desarchiva: eso último es de #70.
        habito!.Id = existente.Id;
        habito.UsuarioId = existente.UsuarioId;
        habito.Activo = existente.Activo;

        return await repositorio.ActualizarAsync(habito, cancellationToken)
            ? Ok(MapeoDeHabitos.ADto(habito))
            : NoEncontrado(id);
    }

    [HttpPut("{id:int}/registros/{fecha}")]
    [ProducesResponseType<RegistroDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarAsync(
        int id,
        string fecha,
        [FromBody] RegistroEntradaDto entrada,
        CancellationToken cancellationToken)
    {
        var (dia, habito, error) = await PrepararAsync(id, fecha, cancellationToken);

        if (error is not null)
        {
            return error;
        }

        var valor = entrada.Valor!.Value;

        if (valor < 0 || valor > ValorMaximo || (EsBinario(habito!.Tipo) && valor != 1))
        {
            return RespuestasDeError.De(ValorFueraDeRango(habito.Tipo));
        }

        // La meta se copia del hábito y el completado se deriva acá: el cliente manda un solo número.
        var metaObjetivo = habito.Meta ?? 1m;

        var registro = await repositorio.GuardarRegistroAsync(
            new Registro
            {
                HabitoId = id,
                Fecha = dia!.Value,
                ValorAlcanzado = valor,
                MetaObjetivo = metaObjetivo,
                Completado = valor >= metaObjetivo
            },
            cancellationToken);

        return Ok(MapeoDeRegistros.ADto(registro));
    }

    [HttpDelete("{id:int}/registros/{fecha}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesmarcarAsync(
        int id,
        string fecha,
        CancellationToken cancellationToken)
    {
        var (dia, _, error) = await PrepararAsync(id, fecha, cancellationToken);

        if (error is not null)
        {
            return error;
        }

        await repositorio.BorrarRegistroAsync(id, dia!.Value, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Las reglas del hábito, compartidas por el alta y la edición: título no vacío, tipo y frecuencia
    /// conocidos, categoría existente y vigente, y meta y unidad derivadas del tipo. Devuelve el hábito
    /// listo para guardar o el error que corresponde.
    /// </summary>
    /// <param name="existente">
    /// El hábito que se edita, o <c>null</c> en el alta. La edición conserva la categoría que el hábito
    /// ya tenía —incluida <c>null</c>— aunque después se haya desactivado (L4: el catálogo no le quita
    /// lo que ya eligió); cambiar a otra exige que exista y esté activa.
    /// </param>
    private async Task<(Habito? Habito, IActionResult? Error)> ValidarAsync(
        HabitoEntradaDto entrada,
        Habito? existente,
        CancellationToken cancellationToken)
    {
        var titulo = (entrada.Titulo ?? string.Empty).Trim();

        // [Required] acepta «   »: el recorte lo decide el servidor, no el navegador.
        if (titulo.Length == 0)
        {
            ModelState.AddModelError(nameof(entrada.Titulo), "El título no puede quedar vacío.");
        }

        var tipo = entrada.Tipo ?? string.Empty;
        if (tipo is not (TiposDeHabito.SiNo or TiposDeHabito.Contador or TiposDeHabito.Abstinencia))
        {
            ModelState.AddModelError(nameof(entrada.Tipo), TipoInvalido());
        }

        var frecuenciaTipo = entrada.Frecuencia?.Tipo ?? string.Empty;
        if (frecuenciaTipo is not (TiposDeHabito.Diaria or TiposDeHabito.Semanal))
        {
            ModelState.AddModelError("Frecuencia.Tipo", FrecuenciaInvalida());
        }

        if (!ModelState.IsValid)
        {
            return (null, ValidationProblem(ModelState));
        }

        if (existente is null || entrada.CategoriaId != existente.CategoriaId)
        {
            var categoria = await ValidarCategoriaAsync(entrada.CategoriaId, cancellationToken);
            if (categoria.Error is not null)
            {
                return (null, categoria.Error);
            }
        }

        // Meta y unidad sólo tienen sentido en un contador: en sí/no y abstinencia se guardan nulas
        // aunque el cliente las mande, porque tildar vale 1.
        decimal? meta = null;
        string? unidad = null;

        if (tipo == TiposDeHabito.Contador)
        {
            if (entrada.Meta is null || entrada.Meta <= 0)
            {
                return (null, RespuestasDeError.De(new ErrorDeNegocio(
                    "meta_requerida",
                    "Falta la meta",
                    "Un hábito de contador necesita una meta diaria mayor que cero.",
                    StatusCodes.Status400BadRequest)));
            }

            if (string.IsNullOrWhiteSpace(entrada.Unidad))
            {
                return (null, RespuestasDeError.De(new ErrorDeNegocio(
                    "unidad_requerida",
                    "Falta la unidad",
                    "Un hábito de contador necesita la unidad que se cuenta (vasos, páginas, minutos…).",
                    StatusCodes.Status400BadRequest)));
            }

            meta = entrada.Meta;
            unidad = entrada.Unidad.Trim();
        }

        int? veces = null;

        if (frecuenciaTipo == TiposDeHabito.Semanal)
        {
            if (entrada.Frecuencia!.Veces is not { } cuota || cuota < 1 || cuota > CuotaMaxima)
            {
                return (null, RespuestasDeError.De(new ErrorDeNegocio(
                    "frecuencia_fuera_de_rango",
                    "Frecuencia fuera de rango",
                    $"Un hábito semanal se repite entre 1 y {CuotaMaxima} veces por semana.",
                    StatusCodes.Status400BadRequest)));
            }

            veces = cuota;
        }

        return (new Habito
        {
            CategoriaId = entrada.CategoriaId,
            Titulo = titulo,
            Descripcion = string.IsNullOrWhiteSpace(entrada.Descripcion) ? null : entrada.Descripcion.Trim(),
            Tipo = tipo,
            Meta = meta,
            Unidad = unidad,
            FrecuenciaTipo = frecuenciaTipo,
            FrecuenciaVeces = veces
        }, null);
    }

    /// <summary>
    /// La categoría es obligatoria y tiene que existir y estar activa. Se devuelve 400 con un `codigo`
    /// y no un 404 porque el recurso que falla es el alta, no una ruta inexistente.
    /// </summary>
    private async Task<(Categoria? Categoria, ObjectResult? Error)> ValidarCategoriaAsync(
        int? categoriaId,
        CancellationToken cancellationToken)
    {
        if (categoriaId is null)
        {
            return (null, RespuestasDeError.De(new ErrorDeNegocio(
                "categoria_requerida",
                "Falta la categoría",
                "Elegí la categoría del hábito: el catálogo compartido es obligatorio en el alta.",
                StatusCodes.Status400BadRequest)));
        }

        var categoria = await categorias.BuscarAsync(categoriaId.Value, cancellationToken);

        if (categoria is null)
        {
            return (null, RespuestasDeError.De(new ErrorDeNegocio(
                "categoria_inexistente",
                "Categoría inexistente",
                $"No hay ninguna categoría con id {categoriaId}.",
                StatusCodes.Status400BadRequest)));
        }

        return categoria.Activo
            ? (categoria, null)
            : (null, RespuestasDeError.De(new ErrorDeNegocio(
                "categoria_inactiva",
                "Categoría inactiva",
                $"«{categoria.Nombre}» está inactiva: elegí una del catálogo vigente.",
                StatusCodes.Status400BadRequest)));
    }

    /// <summary>
    /// Las tres condiciones que comparten marcar y desmarcar: fecha legible y en rango, hábito propio y
    /// hábito activo. Devuelve las tres cosas en una tupla porque no se puede usar <c>out</c> con async.
    /// </summary>
    private async Task<(DateOnly? Dia, Habito? Habito, IActionResult? Error)> PrepararAsync(
        int id,
        string fecha,
        CancellationToken cancellationToken)
    {
        if (!FechaIso.TryParsear(fecha, out var dia))
        {
            return (null, null, RespuestasDeError.De(FechaIso.ErrorDeFormato(fecha)));
        }

        if (!FechaIso.EstaEnRango(dia))
        {
            return (null, null, RespuestasDeError.De(FechaIso.ErrorDeRango(dia)));
        }

        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return (null, null, RespuestasDeError.De(ErroresDeSesion.SesionIlegible()));
        }

        var habito = await repositorio.BuscarAsync(id, usuarioId, cancellationToken);

        if (habito is null)
        {
            return (null, null, NoEncontrado(id));
        }

        if (!habito.Activo)
        {
            return (null, null, RespuestasDeError.De(new ErrorDeNegocio(
                "habito_archivado",
                "Hábito archivado",
                $"«{habito.Titulo}» está archivado: reactivalo antes de marcarlo.",
                StatusCodes.Status400BadRequest)));
        }

        return (dia, habito, null);
    }

    private static bool EsBinario(string tipo) =>
        tipo is TiposDeHabito.SiNo or TiposDeHabito.Abstinencia;

    private static ErrorDeNegocio ValorFueraDeRango(string tipo) => new(
        "valor_fuera_de_rango",
        "Valor fuera de rango",
        EsBinario(tipo)
            ? $"Un hábito «{tipo}» se marca con valor 1: para desmarcarlo se borra la marca."
            : "El valor tiene que ir de 0 a 999999,99.",
        StatusCodes.Status400BadRequest);

    private static string TipoInvalido() =>
        $"El tipo tiene que ser {TiposDeHabito.SiNo}, {TiposDeHabito.Contador} o {TiposDeHabito.Abstinencia}.";

    private static string FrecuenciaInvalida() =>
        $"La frecuencia tiene que ser {TiposDeHabito.Diaria} o {TiposDeHabito.Semanal}.";

    /// <summary>Los errores previstos se devuelven, no se lanzan: el manejador queda para lo imprevisto.</summary>
    private static ObjectResult NoEncontrado(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Hábito inexistente",
        $"No hay ningún hábito con id {id} en tu cuenta.",
        StatusCodes.Status404NotFound));
}
