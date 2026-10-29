using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>
/// Proyección del modelo a lo que viaja por HTTP. El modelo nunca se serializa: así la tabla puede
/// cambiar sin romper el contrato del SPA.
/// </summary>
public static class MapeoDeCategorias
{
    public static CategoriaDto ADto(Categoria categoria) =>
        new(categoria.Id, categoria.Nombre, categoria.Descripcion, categoria.Activo);
}
