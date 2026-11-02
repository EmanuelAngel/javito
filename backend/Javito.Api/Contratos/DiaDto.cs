namespace Javito.Api.Contratos;

/// <summary>
/// El día del usuario tal como lo expone la API: una proyección de sólo lectura sobre sus hábitos
/// activos. La identidad es la fecha, y la manda el cliente porque el servidor no sabe en qué huso
/// horario vive quien mira la pantalla.
/// </summary>
public sealed record DiaDto(DateOnly Fecha, IReadOnlyList<HabitoDelDiaDto> Habitos);

/// <summary>Un hábito dentro del día, con su marca de esa fecha y el progreso de la semana.</summary>
public sealed record HabitoDelDiaDto(
    int Id,
    string Titulo,
    string? Categoria,
    string Tipo,
    decimal? Meta,
    string? Unidad,
    FrecuenciaDto Frecuencia,
    SemanaDto Semana,
    RegistroDelDiaDto? Registro);

/// <summary>Frecuencia del hábito: diaria o N veces por semana.</summary>
public sealed record FrecuenciaDto(string Tipo, int? Veces);

/// <summary>Progreso de la semana en curso: de lunes a domingo.</summary>
public sealed record SemanaDto(int Cumplidos, int Objetivo);

/// <summary>La marca del día. <c>null</c> en el hábito significa que todavía no se marcó.</summary>
public sealed record RegistroDelDiaDto(decimal Valor, bool Completado);
