using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>Proyección del registro a lo que viaja por HTTP.</summary>
public static class MapeoDeRegistros
{
    public static RegistroDto ADto(Registro registro) =>
        new(registro.ValorAlcanzado, registro.MetaObjetivo, registro.Completado);
}
