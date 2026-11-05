namespace Javito.Api.Contratos;

/// <summary>Un hábito dentro de una rutina, ya con el orden en el que se hace.</summary>
public sealed record HabitoEnRutinaDto(int Id, string Titulo, int Orden);
