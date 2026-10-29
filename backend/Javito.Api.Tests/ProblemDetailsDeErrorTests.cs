using Javito.Api.Contratos;

namespace Javito.Api.Tests;

/// <summary>S3: el código de negocio viaja como extensión estable de ProblemDetails.</summary>
public class ProblemDetailsDeErrorTests
{
    [Fact]
    public void Un_error_de_negocio_conserva_su_codigo_y_estado()
    {
        var error = new ErrorDeNegocio(
            "nombre-duplicado",
            "La categoría ya existe",
            "Ya existe una categoría con ese nombre.");

        var problema = MapeoDeErrores.ProblemDetails(error);

        Assert.Equal(409, problema.Status);
        Assert.Equal("La categoría ya existe", problema.Title);
        Assert.Equal("Ya existe una categoría con ese nombre.", problema.Detail);
        Assert.Equal("nombre-duplicado", problema.Extensions["codigo"]);
    }
}
