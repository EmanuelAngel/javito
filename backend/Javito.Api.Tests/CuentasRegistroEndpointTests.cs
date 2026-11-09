using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// ACC-01 (HU-ACC-01): el registro público. Una prueba por regla de la historia; en cada rechazo se
/// mira el <c>COUNT(*)</c> antes y después. Todo lo que crea esta clase lleva el prefijo
/// <see cref="Prefijo"/> en el email y se borra antes y después de correr.
/// </summary>
[Collection(ColeccionDeUsuarios.Nombre)]
public class CuentasRegistroEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "prueba-registro-";

    private const string Contrasena = "Secreta2026";

    private readonly HttpClient _anonimo = fabrica.CreateClient();

    public async Task InitializeAsync() => await LimpiarAsync();

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _anonimo.Dispose();
    }

    [Fact]
    public async Task Registrar_sin_sesion_devuelve_201_con_sesion_iniciada_y_rol_Usuario_activo()
    {
        var respuesta = await RegistrarAsync("Ana", $"{Prefijo}ana@javito.test", Contrasena);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>();
        Assert.NotNull(sesion);
        Assert.False(string.IsNullOrEmpty(sesion.Token));
        Assert.Equal("Ana", sesion.Usuario.Nombre);
        Assert.Equal("Usuario", sesion.Usuario.Rol);
        Assert.Null(sesion.Usuario.AvatarUrl);

        // El token sirve sin pasar por el login.
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sesion.Token);
        var yo = await cliente.GetAsync("/api/sesion/yo");
        Assert.Equal(HttpStatusCode.OK, yo.StatusCode);

        Assert.Equal(1, await ContarAsync($"{Prefijo}ana@javito.test", "activo = 1 AND rol = 'Usuario'"));
        Assert.DoesNotContain("hash", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registrar_guarda_el_email_en_minusculas_y_sin_espacios()
    {
        var respuesta = await RegistrarAsync("Ana", $"  {Prefijo}MAYUS@Javito.Test  ", Contrasena);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(1, await ContarAsync($"{Prefijo}mayus@javito.test", "email = @email COLLATE utf8mb4_bin"));
    }

    [Fact]
    public async Task Registrar_guarda_la_contrasena_hasheada_y_el_hash_verifica()
    {
        await RegistrarAsync("Ana", $"{Prefijo}hash@javito.test", Contrasena);

        var hash = await LeerHashAsync($"{Prefijo}hash@javito.test");

        Assert.NotEqual(Contrasena, hash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Contrasena, hash));
    }

    [Fact]
    public async Task Registrar_con_email_ya_registrado_devuelve_409_email_repetido_y_no_escribe()
    {
        await RegistrarAsync("Ana", $"{Prefijo}repe@javito.test", Contrasena);
        var antes = await ContarCuentasAsync();

        var respuesta = await RegistrarAsync("Otra", $"{Prefijo.ToUpperInvariant()}REPE@javito.test", Contrasena);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("email_repetido", await CodigoAsync(respuesta));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Registrar_con_contrasena_corta_devuelve_400_por_campo_y_no_escribe()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await RegistrarAsync("Ana", $"{Prefijo}corta@javito.test", "corta");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.True(documento.RootElement.GetProperty("errors").TryGetProperty("Password", out _));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Registrar_ignora_un_rol_enviado_por_el_cliente()
    {
        var respuesta = await _anonimo.PostAsJsonAsync("/api/cuentas", new
        {
            nombre = "Intruso",
            email = $"{Prefijo}rol@javito.test",
            password = Contrasena,
            rol = "Administrador"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(1, await ContarAsync($"{Prefijo}rol@javito.test", "rol = 'Usuario'"));
    }

    // --- Ayudas -------------------------------------------------------------------------------

    private Task<HttpResponseMessage> RegistrarAsync(string nombre, string email, string password) =>
        _anonimo.PostAsJsonAsync("/api/cuentas", new { nombre, email, password });

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        return documento.RootElement.TryGetProperty("codigo", out var codigo)
            ? codigo.GetString()
            : null;
    }

    private static async Task<int> ContarCuentasAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand("SELECT COUNT(*) FROM usuario", conexion);

        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    private static async Task<int> ContarAsync(string email, string condicion)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(
            $"SELECT COUNT(*) FROM usuario WHERE email = @email AND {condicion}",
            conexion);
        comando.Parameters.AddWithValue("@email", email);

        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    private static async Task<string> LeerHashAsync(string email)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(
            "SELECT password_hash FROM usuario WHERE email = @email",
            conexion);
        comando.Parameters.AddWithValue("@email", email);

        return (string)(await comando.ExecuteScalarAsync())!;
    }

    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(
            "DELETE FROM usuario WHERE email LIKE @patron",
            conexion);
        comando.Parameters.AddWithValue("@patron", $"%{Prefijo}%");
        await comando.ExecuteNonQueryAsync();
    }
}
