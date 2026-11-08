using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// El fixture nunca debe tocar la base de desarrollo. Estas pruebas no necesitan MySQL: verifican la
/// transformación del script, que es donde se borraba javito en cada corrida.
/// </summary>
public class ScriptDePruebasTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 2);

    [Fact]
    public void Adaptar_renombra_todas_las_sentencias_que_nombran_una_base()
    {
        var adaptado = ScriptDePruebas.Adaptar(ScriptDePruebas.LeerDatabaseSql(), Hoy);

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
        var adaptado = ScriptDePruebas.Adaptar(ScriptDePruebas.LeerDatabaseSql(), Hoy);

        // Si alguien reemplaza "javito" por "javito_test" a lo bruto, los mails del seed se rompen.
        Assert.Contains("admin@javito.local", adaptado);
    }

    [Fact]
    public void Adaptar_siembra_con_la_fecha_que_recibe_y_no_con_la_del_servidor()
    {
        var adaptado = ScriptDePruebas.Adaptar(ScriptDePruebas.LeerDatabaseSql(), new DateOnly(2026, 11, 2));

        // Si el seed preguntara CURDATE() por su cuenta, una corrida que cruza la medianoche siembra un
        // día y las pruebas consultan el siguiente.
        Assert.DoesNotContain("CURDATE()", adaptado);
        Assert.Contains("DATE('2026-11-02')", adaptado);
    }

    [Fact]
    public void Adaptar_falla_si_el_script_nombra_otra_base()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(
            () => ScriptDePruebas.Adaptar("CREATE DATABASE otra;\n", Hoy));

        Assert.Contains("otra", excepcion.Message);
    }
}
