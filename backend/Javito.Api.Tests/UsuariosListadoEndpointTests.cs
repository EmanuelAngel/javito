using System.Net;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// USU-01 (HU-USU-01): el listado de usuarios del administrador. Una prueba por criterio de la
/// historia. Afirma sobre las cuatro cuentas del seed, en el orden en que las devuelve la API:
/// id 4 «Cuenta desactivada» (inactiva), id 3 «Otra cuenta», id 2 «Usuario de prueba» e id 1
/// «Administración Javito», que es el más nuevo primero porque el id es AUTO_INCREMENT.
/// </summary>
[Collection(ColeccionDeUsuarios.Nombre)]
public class UsuariosListadoEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const int CuentasDelSeed = 4;

    private readonly HttpClient _administrador = fabrica.CreateClient();
    private readonly HttpClient _usuario = fabrica.CreateClient();
    private readonly HttpClient _anonimo = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAdministradorAsync(_administrador);
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
    }

    public Task DisposeAsync()
    {
        _administrador.Dispose();
        _usuario.Dispose();
        _anonimo.Dispose();
        return Task.CompletedTask;
    }

    // --- S1: paginado en el servidor -----------------------------------------------------------

    [Fact]
    public async Task Listar_pagina_en_el_servidor_y_declara_el_total_de_registros()
    {
        var pagina = await LeerPaginaAsync("/api/usuarios?pagina=1&tamanioDePagina=2");

        Assert.Equal(1, pagina.GetProperty("pagina").GetInt32());
        Assert.Equal(2, pagina.GetProperty("tamanioDePagina").GetInt32());
        Assert.Equal(CuentasDelSeed, pagina.GetProperty("totalDeRegistros").GetInt32());
        Assert.Equal(2, pagina.GetProperty("totalDePaginas").GetInt32());
        Assert.Equal([4, 3], Ids(pagina));

        var siguiente = await LeerPaginaAsync("/api/usuarios?pagina=2&tamanioDePagina=2");

        Assert.Equal([2, 1], Ids(siguiente));
    }

    [Fact]
    public async Task Listar_con_una_pagina_fuera_de_rango_devuelve_400()
    {
        var respuesta = await _administrador.GetAsync("/api/usuarios?pagina=0");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(
            await ContieneElCampoInvalidoAsync(respuesta, "Pagina"),
            "El 400 tiene que nombrar el campo inválido.");
    }

    // --- S2 y S14: nombre, email, rol y estado; el hash nunca viaja ------------------------------

    [Fact]
    public async Task Listar_devuelve_nombre_email_rol_y_estado_y_ningun_indicio_del_hash()
    {
        var pagina = await LeerPaginaAsync("/api/usuarios?tamanioDePagina=50");

        foreach (var cuenta in pagina.GetProperty("elementos").EnumerateArray())
        {
            var campos = cuenta.EnumerateObject().Select(campo => campo.Name).ToList();

            Assert.Equal(["id", "nombre", "email", "rol", "activo"], campos);
        }

        var desactivada = pagina.GetProperty("elementos")[0];

        Assert.Equal("Cuenta desactivada", desactivada.GetProperty("nombre").GetString());
        Assert.Equal("desactivado@javito.local", desactivada.GetProperty("email").GetString());
        Assert.Equal("Usuario", desactivada.GetProperty("rol").GetString());
        Assert.False(desactivada.GetProperty("activo").GetBoolean());

        Assert.Equal(
            "Administrador",
            pagina.GetProperty("elementos")[3].GetProperty("rol").GetString());
    }

    // --- S3: un solo texto que busca en nombre o en email ---------------------------------------

    [Fact]
    public async Task Buscar_por_q_encuentra_por_nombre_y_por_email()
    {
        Assert.Equal([3], Ids(await LeerPaginaAsync("/api/usuarios?q=Otra")));
        Assert.Equal([1], Ids(await LeerPaginaAsync("/api/usuarios?q=admin@javito")));
        Assert.Equal(
            CuentasDelSeed,
            (await LeerPaginaAsync("/api/usuarios?q=@javito.local")).GetProperty("totalDeRegistros").GetInt32());
    }

    [Fact]
    public async Task Buscar_por_q_trata_los_comodines_de_like_como_texto()
    {
        Assert.Empty(Ids(await LeerPaginaAsync("/api/usuarios?q=%25")));
        Assert.Empty(Ids(await LeerPaginaAsync("/api/usuarios?q=_")));
    }

    // --- S4: los activos y los desactivados se distinguen ---------------------------------------

    [Fact]
    public async Task Filtrar_por_estado_separa_los_activos_de_los_desactivados()
    {
        Assert.Equal(
            CuentasDelSeed,
            (await LeerPaginaAsync("/api/usuarios")).GetProperty("totalDeRegistros").GetInt32());

        var activos = await LeerPaginaAsync("/api/usuarios?activo=true&tamanioDePagina=50");
        Assert.Equal([3, 2, 1], Ids(activos));
        Assert.All(
            activos.GetProperty("elementos").EnumerateArray(),
            cuenta => Assert.True(cuenta.GetProperty("activo").GetBoolean()));

        var desactivados = await LeerPaginaAsync("/api/usuarios?activo=false&tamanioDePagina=50");
        Assert.Equal([4], Ids(desactivados));
        Assert.All(
            desactivados.GetProperty("elementos").EnumerateArray(),
            cuenta => Assert.False(cuenta.GetProperty("activo").GetBoolean()));
    }

    // --- S12: sólo el Administrador, y un rechazo no toca ninguna fila ---------------------------

    [Fact]
    public async Task Listar_sin_sesion_devuelve_401_no_autenticado()
    {
        var respuesta = await _anonimo.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("no_autenticado", await CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Listar_como_usuario_devuelve_403_sin_permiso_y_no_cambia_ninguna_cuenta()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await _usuario.GetAsync("/api/usuarios?q=admin");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("sin_permiso", await CodigoAsync(respuesta));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    private async Task<JsonElement> LeerPaginaAsync(string ruta)
    {
        var respuesta = await _administrador.GetAsync(ruta);
        respuesta.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        // Clon: el JsonElement dejaría de valer al salir del using y la prueba lo lee afuera.
        return documento.RootElement.Clone();
    }

    private static List<int> Ids(JsonElement pagina) =>
        pagina.GetProperty("elementos")
            .EnumerateArray()
            .Select(cuenta => cuenta.GetProperty("id").GetInt32())
            .ToList();

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        return documento.RootElement.TryGetProperty("codigo", out var codigo)
            ? codigo.GetString()
            : null;
    }

    private static async Task<bool> ContieneElCampoInvalidoAsync(
        HttpResponseMessage respuesta,
        string campo)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        return documento.RootElement.TryGetProperty("errors", out var errores) &&
            errores.TryGetProperty(campo, out _);
    }

    private static async Task<int> ContarCuentasAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand("SELECT COUNT(*) FROM usuario", conexion);

        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }
}
