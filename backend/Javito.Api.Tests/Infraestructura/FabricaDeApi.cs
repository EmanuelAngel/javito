using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Javito.Api.Tests.Infraestructura;

/// <summary>
/// WebApplicationFactory apuntada al schema de pruebas. El fixture de xUnit exige un único
/// constructor público, así que las variantes se piden por métodos estáticos.
/// </summary>
public sealed class FabricaDeApi : WebApplicationFactory<Program>
{
    private readonly string _cadena;
    private readonly string _entorno;

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
    }
}
