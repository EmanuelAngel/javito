using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>
/// Proyección del modelo a lo que viaja por HTTP. El modelo nunca se serializa: así la tabla puede
/// cambiar sin romper el contrato del SPA.
/// </summary>
public static class MapeoDeRutinas
{
    public static RutinaDto ADto(Rutina rutina) =>
        new(rutina.Id, rutina.Nombre, rutina.Descripcion, rutina.Activo);

    public static RutinaDetalleDto ADetalleDto(Rutina rutina, IReadOnlyList<HabitoEnRutina> habitos) =>
        new(
            rutina.Id,
            rutina.Nombre,
            rutina.Descripcion,
            rutina.Activo,
            habitos.Select(habito => new HabitoEnRutinaDto(habito.Id, habito.Titulo, habito.Orden)).ToList());
}
