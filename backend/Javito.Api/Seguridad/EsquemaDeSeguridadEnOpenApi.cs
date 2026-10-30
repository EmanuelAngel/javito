using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Javito.Api.Seguridad;

/// <summary>
/// Declara el esquema <c>bearer</c> en el documento OpenAPI y lo exige en todas las operaciones.
/// Sin esto la UI de Scalar no tiene dónde pegar el token y <c>RT-07</c> no se puede evidenciar.
/// </summary>
public sealed class EsquemaDeSeguridadEnOpenApi : IOpenApiDocumentTransformer
{
    public const string Esquema = "bearer";

    public Task TransformAsync(
        OpenApiDocument documento,
        OpenApiDocumentTransformerContext contexto,
        CancellationToken cancellationToken)
    {
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        documento.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token que devuelve POST /api/sesion. Dura 8 horas y no se renueva."
        };

        // El requisito a nivel documento aplica a todas las operaciones: no hay que tocar cada una.
        documento.Security ??= [];
        documento.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(Esquema, documento, null)] = []
        });

        return Task.CompletedTask;
    }
}
