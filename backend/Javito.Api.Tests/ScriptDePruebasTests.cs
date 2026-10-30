using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// El fixture nunca debe tocar la base de desarrollo. Estas pruebas no necesitan MySQL: verifican la
/// transformación del script, que es donde se borraba javito en cada corrida.
/// </summary>
public class ScriptDePruebasTests
{
    [Fact]
    public void Adaptar_renombra_todas_las_sentencias_que_nombran_una_base()
    {
        var adaptado = ScriptDePruebas.Adaptar(ScriptDePruebas.LeerDatabaseSql());

        Assert.Contains("DROP DATABASE IF EXISTS javito_test", adaptado);
        Assert.Contains("CREATE DATABASE javito_test", adaptado);
        Assert.Contains("USE javito_test;", adaptado);
        Assert.DoesNotContain("DROP DATABASE IF EXISTS javito;", adaptado);
        Assert.DoesNotContain("CREATE DATABASE javito ", adaptado);
        Assert.DoesNotContain("USE javito;", adaptado);
    }

    [Fact]
    public void Adaptar_conserva_los_datos_del_seed()
    {
        var adaptado = ScriptDePruebas.Adaptar(ScriptDePruebas.LeerDatabaseSql());

        // Si alguien reemplaza "javito" por "javito_test" a lo bruto, los mails del seed se rompen.
        Assert.Contains("admin@javito.local", adaptado);
    }

    [Fact]
    public void Adaptar_falla_si_el_script_nombra_otra_base()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(
            () => ScriptDePruebas.Adaptar("CREATE DATABASE otra;\n"));

        Assert.Contains("otra", excepcion.Message);
    }
}
