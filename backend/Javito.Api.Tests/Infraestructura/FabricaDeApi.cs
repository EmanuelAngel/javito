using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Javito.Api.Tests.Infraestructura;

/// <summary>
/// WebApplicationFactory apuntada al schema de pruebas. El fixture de xUnit exige un único
/// constructor público, así que las variantes se piden por métodos estáticos.
/// </summary>
public sealed class FabricaDeApi : WebApplicationFactory<Program>
{
    /// <summary>
    /// Clave de firma de las pruebas. No es un secreto: la API real la toma de user-secrets y falla
    /// al arrancar si falta, así que el host de pruebas tiene que declarar la suya.
    /// </summary>
    public const string ClaveDeJwtDePruebas = "clave-de-pruebas-para-firmar-jwt-con-32-bytes-o-mas";

    private readonly string _cadena;
    private readonly string _entorno;

    /// <summary>
    /// Raíz de los archivos subidos durante la prueba. Un temporal por fábrica: sin esto el almacén
    /// escribiría dentro de <c>backend/Javito.Api/wwwroot/uploads</c>, que es del proceso de
    /// desarrollo y no de los tests.
    /// </summary>
    private readonly string _rutaDeArchivos =
        Path.Combine(Path.GetTempPath(), $"javito-archivos-{Guid.NewGuid():N}");

    public string RutaDeArchivos => _rutaDeArchivos;

    public FabricaDeApi() : this(BaseDeDatosDePrueba.CadenaDeConexion, "Development", prepararBase: true)
    {
    }

    private FabricaDeApi(string cadena, string entorno, bool prepararBase)
    {
        _cadena = cadena;
        _entorno = entorno;

        if (prepararBase)
        {
            BaseDeDatosDePrueba.AsegurarAsync().GetAwaiter().GetResult();
        }
    }

    /// <summary>API apuntada a otra cadena, para ejercitar la rama de base caída.</summary>
    public static FabricaDeApi ConCadena(string cadena) => new(cadena, "Development", prepararBase: false);

    /// <summary>API con otro entorno, para las ramas que sólo corren fuera de Development.</summary>
    public static FabricaDeApi EnEntorno(string entorno) =>
        new(BaseDeDatosDePrueba.CadenaDeConexion, entorno, prepararBase: true);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_entorno);
        builder.UseSetting("ConnectionStrings:Javito", _cadena);
        builder.UseSetting("Jwt:Clave", ClaveDeJwtDePruebas);
        builder.UseSetting("Archivos:Ruta", _rutaDeArchivos);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_rutaDeArchivos))
        {
            Directory.Delete(_rutaDeArchivos, recursive: true);
        }
    }
}
