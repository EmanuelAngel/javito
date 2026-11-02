using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>Proyección del hábito a lo que viaja por HTTP.</summary>
public static class MapeoDeHabitos
{
    public static HabitoDto ADto(Habito habito) => new(
        habito.Id,
        habito.Titulo,
        habito.Descripcion,
        habito.CategoriaId,
        habito.Tipo,
        habito.Meta,
        habito.Unidad,
        new FrecuenciaDto(habito.FrecuenciaTipo, habito.FrecuenciaVeces),
        habito.Activo);
}
