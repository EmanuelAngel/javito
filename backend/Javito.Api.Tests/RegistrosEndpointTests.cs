using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// S2…S5: el registro es un recurso identificado por (hábito, fecha). Una prueba por regla: la meta
/// sale del hábito, el toque es idempotente, desmarcar borra, el contador baja y nadie marca lo ajeno.
/// Las pruebas escriben en fechas de 2001, donde el seed no sembró nada, y limpian lo suyo: el weekly
/// del seed («Ejercicio pesado» 2 de 3) es lo que afirman las pruebas del día.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class RegistrosEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>Lunes: la semana que se cuenta empieza acá.</summary>
    private static readonly DateOnly UnLunes = new(2001, 1, 1);

    private readonly HttpClient _cliente = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_cliente);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _cliente.Dispose();
    }

    [Fact]
    public async Task Marcar_guarda_la_meta_del_habito_y_deriva_el_completado()
    {
        // El cuerpo manda una meta y un completado falsos: los dos tienen que venir del hábito.
        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/habitos/15/registros/{UnLunes:yyyy-MM-dd}",
            new { valor = 4, metaObjetivo = 1, completado = true });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var registro = await respuesta.Content.ReadFromJsonAsync<RegistroGuardadoDePrueba>();
        Assert.NotNull(registro);
        Assert.Equal(4m, registro.Valor);
        Assert.Equal(10m, registro.MetaObjetivo);
        Assert.False(registro.Completado);
    }

    [Fact]
    public async Task Repetir_el_mismo_toque_deja_el_mismo_estado_y_una_sola_marca()
    {
        var ruta = $"/api/habitos/15/registros/{UnLunes:yyyy-MM-dd}";

        var primero = await _cliente.PutAsJsonAsync(ruta, new { valor = 10 });
        var segundo = await _cliente.PutAsJsonAsync(ruta, new { valor = 10 });

        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);

        var guardado = await segundo.Content.ReadFromJsonAsync<RegistroGuardadoDePrueba>();
        Assert.NotNull(guardado);
        Assert.True(guardado.Completado);

        var dia = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{UnLunes:yyyy-MM-dd}");
        Assert.NotNull(dia);
        var habito = Assert.Single(dia.Habitos, habito => habito.Id == 15);
        Assert.Equal(10m, habito.Registro!.Valor);
    }

    [Fact]
    public async Task Desmarcar_borra_el_registro_y_repetirlo_tambien_responde_204()
    {
        var ruta = $"/api/habitos/1/registros/{UnLunes:yyyy-MM-dd}";
        await _cliente.PutAsJsonAsync(ruta, new { valor = 1 });

        var primeraBaja = await _cliente.DeleteAsync(ruta);
        var segundaBaja = await _cliente.DeleteAsync(ruta);

        Assert.Equal(HttpStatusCode.NoContent, primeraBaja.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segundaBaja.StatusCode);

        var dia = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{UnLunes:yyyy-MM-dd}");
        Assert.NotNull(dia);
        Assert.Null(Assert.Single(dia.Habitos, habito => habito.Id == 1).Registro);
    }

    [Fact]
    public async Task El_contador_baja_de_la_meta_y_el_completado_vuelve_a_falso()
    {
        var ruta = $"/api/habitos/15/registros/{UnLunes:yyyy-MM-dd}";

        var alcanzada = await _cliente.PutAsJsonAsync(ruta, new { valor = 10 });
        var bajada = await _cliente.PutAsJsonAsync(ruta, new { valor = 5 });

        var conMeta = await alcanzada.Content.ReadFromJsonAsync<RegistroGuardadoDePrueba>();
        var porDebajo = await bajada.Content.ReadFromJsonAsync<RegistroGuardadoDePrueba>();

        Assert.True(conMeta!.Completado);
        Assert.False(porDebajo!.Completado);
        Assert.Equal(5m, porDebajo.Valor);
    }

    [Fact]
    public async Task Un_habito_de_otra_cuenta_devuelve_404_sin_escribir_nada()
    {
        var ruta = $"/api/habitos/18/registros/{UnLunes:yyyy-MM-dd}";
        var antes = await ContarRegistrosAsync(18);

        var alta = await _cliente.PutAsJsonAsync(ruta, new { valor = 1 });
        var baja = await _cliente.DeleteAsync(ruta);

        Assert.Equal(HttpStatusCode.NotFound, alta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, baja.StatusCode);
        // El seed ya marca el hábito de la otra cuenta: lo que se afirma es que el intento no lo toca.
        Assert.Equal(antes, await ContarRegistrosAsync(18));
    }

    [Fact]
    public async Task Un_habito_archivado_no_se_marca()
    {
        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/habitos/17/registros/{UnLunes:yyyy-MM-dd}",
            new { valor = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("habito_archivado", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Una_fecha_fuera_de_rango_devuelve_400()
    {
        var antes = await _cliente.PutAsJsonAsync(
            "/api/habitos/15/registros/1999-12-31",
            new { valor = 1 });
        var despues = await _cliente.PutAsJsonAsync(
            "/api/habitos/15/registros/2099-01-01",
            new { valor = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, antes.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, despues.StatusCode);

        var problema = await antes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("fecha_fuera_de_rango", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Un_valor_imposible_para_el_tipo_devuelve_400()
    {
        // Un sí/no vale 1; desmarcarlo es DELETE, no un cero.
        var siNo = await _cliente.PutAsJsonAsync(
            $"/api/habitos/1/registros/{UnLunes:yyyy-MM-dd}",
            new { valor = 2 });

        // Un contador no admite negativos.
        var contador = await _cliente.PutAsJsonAsync(
            $"/api/habitos/15/registros/{UnLunes:yyyy-MM-dd}",
            new { valor = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, siNo.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, contador.StatusCode);

        var problema = await siNo.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("valor_fuera_de_rango", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Un_toque_sin_valor_devuelve_400_con_el_error_del_campo()
    {
        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/habitos/15/registros/{UnLunes:yyyy-MM-dd}",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty("Valor", out var mensajes));
        Assert.NotEmpty(mensajes.EnumerateArray());
    }

    [Fact]
    public async Task La_semana_sube_al_marcar_y_baja_al_desmarcar()
    {
        var ruta = $"/api/habitos/15/registros/{UnLunes:yyyy-MM-dd}";

        await _cliente.PutAsJsonAsync(ruta, new { valor = 10 });
        var marcado = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{UnLunes:yyyy-MM-dd}");

        await _cliente.DeleteAsync(ruta);
        var desmarcado = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{UnLunes:yyyy-MM-dd}");

        Assert.Equal(1, Assert.Single(marcado!.Habitos, habito => habito.Id == 15).Semana.Cumplidos);
        Assert.Equal(0, Assert.Single(desmarcado!.Habitos, habito => habito.Id == 15).Semana.Cumplidos);
    }

    private static async Task<long> ContarRegistrosAsync(int habitoId)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            "SELECT COUNT(*) FROM registro_habito WHERE habito_id = @habitoId",
            conexion);
        comando.Parameters.AddWithValue("@habitoId", habitoId);

        return (long)(await comando.ExecuteScalarAsync())!;
    }

    /// <summary>Borra sólo lo que estas pruebas escriben: el seed vive en fechas relativas a hoy.</summary>
    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            "DELETE FROM registro_habito WHERE fecha < '2002-01-01'",
            conexion);
        await comando.ExecuteNonQueryAsync();
    }
}

/// <summary>El registro tal como lo devuelve el PUT: el cliente no manda meta ni completado.</summary>
internal sealed record RegistroGuardadoDePrueba(decimal Valor, decimal MetaObjetivo, bool Completado);
