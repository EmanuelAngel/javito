namespace Javito.Api.Contratos;

/// <summary>Disponibilidad del servicio y de su base de datos.</summary>
public sealed record RespuestaDeSalud(string Estado, string Servicio, string BaseDeDatos);
