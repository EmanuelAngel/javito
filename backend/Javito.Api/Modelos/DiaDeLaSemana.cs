namespace Javito.Api.Modelos;

/// <summary>
/// Un día de la semana en curso con su marca. No es una tabla: sale de cruzar el calendario con
/// <c>registro_habito</c>, y por eso un día sin fila es un día sin marca, no un error.
/// </summary>
public sealed record DiaDeLaSemana(DateOnly Fecha, bool Completado);
