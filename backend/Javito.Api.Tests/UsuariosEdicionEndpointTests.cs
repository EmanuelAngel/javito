using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// USU-03 (HU-USU-03): el administrador corrige el nombre y el email de una cuenta. Una prueba por
/// regla de la historia, y en cada rechazo se mira la fila <em>antes y después</em> por la interfaz
/// pública (<c>GET api/usuarios/{id}</c>) más el <c>COUNT(*)</c>: lo que se afirma no es sólo el
/// código de estado, es que no se escribió nada.
/// </summary>
/// <remarks>
/// La base del módulo se recrea una sola vez por corrida y el listado afirma sobre las cuatro cuentas
/// del seed, así que todo lo que crea esta clase lleva <see cref="Prefijo"/> en el email y la clase lo
/// borra con un DELETE de verdad antes y después de correr. Las cuentas del seed no se editan nunca:
/// la edición se prueba sobre filas propias.
/// </remarks>
[Collection(ColeccionDeUsuarios.Nombre)]
public class UsuariosEdicionEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>Prefijo de los emails que crea esta clase. La limpieza borra por él y nada más.</summary>
    private const string Prefijo = "prueba-edicion-";

    private const string Contrasena = "Secreta2026";

    /// <summary>Email de una cuenta del seed: sirve de «email de otro» en la prueba del 409.</summary>
    private const string EmailDeOtraCuenta = "usuario@javito.local";

    private readonly HttpClient _administrador = fabrica.CreateClient();
    private readonly HttpClient _usuario = fabrica.CreateClient();
    private readonly HttpClient _anonimo = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAdministradorAsync(_administrador);
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _administrador.Dispose();
        _usuario.Dispose();
        _anonimo.Dispose();
    }

    // --- S9: se editan nombre y email -----------------------------------------------------------

    [Fact]
    public async Task Editar_devuelve_200_y_una_lectura_posterior_muestra_el_nombre_y_el_email_nuevos()
    {
        var id = await CrearAsync("Nombre anterior", $"{Prefijo}cambia@javito.test");

        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Nombre corregido",
            email = $"{Prefijo}corregido@javito.test"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var devuelta = documento.RootElement;

        // La respuesta es la fila resultante, no el cuerpo que se mandó: lo que la base guardó.
        Assert.Equal(id, devuelta.GetProperty("id").GetInt32());
        Assert.Equal("Nombre corregido", devuelta.GetProperty("nombre").GetString());
        Assert.Equal($"{Prefijo}corregido@javito.test", devuelta.GetProperty("email").GetString());
        Assert.Equal("Usuario", devuelta.GetProperty("rol").GetString());
        Assert.True(devuelta.GetProperty("activo").GetBoolean());

        // Y se verifica por la API, no leyendo la columna: el cambio quedó guardado.
        var guardada = await CuentaAsync(id);

        Assert.Equal("Nombre corregido", guardada.GetProperty("nombre").GetString());
        Assert.Equal($"{Prefijo}corregido@javito.test", guardada.GetProperty("email").GetString());

        // El listado, que es lo que ve el administrador, también lo muestra.
        var enElListado = Assert.Single(
            Elementos(await LeerPaginaAsync($"/api/usuarios?q={Prefijo}corregido")));

        Assert.Equal("Nombre corregido", enElListado.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Editar_normaliza_el_email_a_minusculas_y_sin_espacios()
    {
        var id = await CrearAsync("Email con caja", $"{Prefijo}caja@javito.test");

        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Email con caja",
            email = $"  {Prefijo}MiXeD-Mayusculas@Javito.TEST  "
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        const string normalizado = "prueba-edicion-mixed-mayusculas@javito.test";
        var guardada = await CuentaAsync(id);

        Assert.Equal(normalizado, guardada.GetProperty("email").GetString());

        // El listado busca por lo normalizado y encuentra la fila: lo que quedó guardado es el email
        // en minúsculas, no el texto que se escribió.
        Assert.Single(Elementos(await LeerPaginaAsync($"/api/usuarios?q={normalizado}")));
    }

    // --- S10 y S13: el email sigue sin poder repetirse -----------------------------------------

    [Fact]
    public async Task Editar_con_el_email_de_otra_cuenta_devuelve_409_email_repetido_y_no_cambia_la_fila()
    {
        var id = await CrearAsync("Nombre que no cambia", $"{Prefijo}duplicado@javito.test");
        var antes = await CuentaAsync(id);
        var filas = await ContarCuentasAsync();

        // Con otra caja y con espacios: la columna es insensible a mayúsculas y el chequeo previo
        // compara el email ya normalizado, así que este es el mismo email que el del seed. Sin
        // normalizar, el duplicado se escaparía hasta el UNIQUE y saldría un 500.
        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Nombre que no debería quedar",
            email = $"  {EmailDeOtraCuenta.ToUpperInvariant()}  "
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("email_repetido", await CodigoAsync(respuesta));

        await AfirmarIntactaAsync(id, antes, filas);
    }

    [Fact]
    public async Task Editar_conservando_el_mismo_email_devuelve_200()
    {
        var id = await CrearAsync("Nombre anterior", $"{Prefijo}mismo@javito.test");

        // El choque es con otra fila, no con la propia: guardar la cuenta con el email que ya tiene
        // es válido. Un chequeo de duplicados que no excluya el propio id rompe justo este caso.
        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Nombre nuevo",
            email = $"{Prefijo}mismo@javito.test"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var guardada = await CuentaAsync(id);

        Assert.Equal("Nombre nuevo", guardada.GetProperty("nombre").GetString());
        Assert.Equal($"{Prefijo}mismo@javito.test", guardada.GetProperty("email").GetString());
    }

    // --- S15: la edición es de datos, no de rol ni de estado ------------------------------------

    [Fact]
    public async Task Editar_no_cambia_el_rol_ni_el_estado_aunque_el_cuerpo_los_mande()
    {
        var id = await CrearAsync("Datos de la cuenta", $"{Prefijo}rol-estado@javito.test");

        // El contrato de la edición tiene dos campos: rol y estado son de la parte 2. Un cuerpo que
        // los mande igual no los escribe, porque el modelo se arma campo por campo y no se copia el
        // objeto que llegó.
        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Datos de la cuenta",
            email = $"{Prefijo}rol-estado@javito.test",
            rol = "Administrador",
            activo = false
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var guardada = await CuentaAsync(id);

        Assert.Equal("Usuario", guardada.GetProperty("rol").GetString());
        Assert.True(guardada.GetProperty("activo").GetBoolean(), "La cuenta sigue activa.");
    }

    // --- S11 y S14: la contraseña no se muestra ni se puede escribir ----------------------------

    [Fact]
    public async Task Editar_no_devuelve_ni_permite_cambiar_la_contrasena_de_la_cuenta()
    {
        var email = $"{Prefijo}contrasena@javito.test";
        var id = await CrearAsync("Cuenta con contraseña", email);

        // Un cuerpo con `password` no tiene dónde entrar: el contrato es (nombre, email).
        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Cuenta con contraseña",
            email,
            password = "OtraClaveDistinta2026"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        Assert.DoesNotContain(
            documento.RootElement.EnumerateObject().Select(campo => campo.Name),
            nombre => nombre.Contains("password", StringComparison.OrdinalIgnoreCase));

        // El hash tampoco viaja en la lectura de la cuenta.
        var guardada = await CuentaAsync(id);

        Assert.DoesNotContain(
            guardada.EnumerateObject().Select(campo => campo.Name),
            nombre => nombre.Contains("password", StringComparison.OrdinalIgnoreCase));

        // Y la contraseña no cambió: se prueba por su efecto, no leyendo password_hash. La cuenta
        // sigue entrando con la que se eligió en el alta.
        using var cliente = fabrica.CreateClient();
        var sesion = await cliente.PostAsJsonAsync("/api/sesion", new { email, password = Contrasena });

        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);

        var conLaOtra = await cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email, password = "OtraClaveDistinta2026" });

        Assert.Equal(HttpStatusCode.Unauthorized, conLaOtra.StatusCode);
    }

    // --- S9 y S16: las reglas de nombre y email son las del alta -------------------------------

    [Fact]
    public async Task Editar_con_el_nombre_en_blanco_devuelve_400_nombrando_el_campo_y_no_cambia_la_fila()
    {
        var id = await CrearAsync("Nombre válido", $"{Prefijo}blanco@javito.test");
        var antes = await CuentaAsync(id);
        var filas = await ContarCuentasAsync();

        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "   ",
            email = $"{Prefijo}blanco@javito.test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(
            await ContieneElCampoInvalidoAsync(respuesta, "Nombre"),
            "El 400 tiene que nombrar el campo Nombre en errors.");

        await AfirmarIntactaAsync(id, antes, filas);
    }

    [Fact]
    public async Task Editar_con_un_email_invalido_devuelve_400_nombrando_el_campo_y_no_cambia_la_fila()
    {
        var id = await CrearAsync("Email inválido", $"{Prefijo}invalido@javito.test");
        var antes = await CuentaAsync(id);
        var filas = await ContarCuentasAsync();

        var respuesta = await _administrador.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Email inválido",
            email = $"{Prefijo}sin-arroba"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(
            await ContieneElCampoInvalidoAsync(respuesta, "Email"),
            "El 400 tiene que nombrar el campo Email en errors.");

        await AfirmarIntactaAsync(id, antes, filas);
    }

    // --- El detalle de una cuenta: la pantalla lo necesita para precargar el formulario ---------

    [Fact]
    public async Task Obtener_por_id_devuelve_los_campos_del_listado_y_sin_el_hash()
    {
        var id = await CrearAsync("Cuenta para leer", $"{Prefijo}leer@javito.test");

        var cuenta = await CuentaAsync(id);

        // Los mismos cinco campos que el listado: el detalle no agrega nada que el listado esconda,
        // y en particular no agrega el hash (S14).
        Assert.Equal(
            ["id", "nombre", "email", "rol", "activo"],
            cuenta.EnumerateObject().Select(campo => campo.Name));
        Assert.Equal(id, cuenta.GetProperty("id").GetInt32());
        Assert.Equal("Cuenta para leer", cuenta.GetProperty("nombre").GetString());
        Assert.Equal($"{Prefijo}leer@javito.test", cuenta.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Obtener_por_id_inexistente_devuelve_404_no_encontrado()
    {
        var respuesta = await _administrador.GetAsync("/api/usuarios/999999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Obtener_por_id_sin_sesion_devuelve_401_y_como_usuario_403()
    {
        var id = await CrearAsync("Cuenta reservada", $"{Prefijo}solo-admin@javito.test");

        var anonima = await _anonimo.GetAsync($"/api/usuarios/{id}");

        Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);
        Assert.Equal("no_autenticado", await CodigoAsync(anonima));

        var sinPermiso = await _usuario.GetAsync($"/api/usuarios/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, sinPermiso.StatusCode);
        Assert.Equal("sin_permiso", await CodigoAsync(sinPermiso));
    }

    // --- 404 y S12: una cuenta que no existe, y sólo el Administrador ---------------------------

    [Fact]
    public async Task Editar_una_cuenta_inexistente_devuelve_404_no_encontrado()
    {
        var filas = await ContarCuentasAsync();

        var respuesta = await _administrador.PutAsJsonAsync("/api/usuarios/999999", new
        {
            nombre = "Fantasma",
            email = $"{Prefijo}fantasma@javito.test"
        });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(respuesta));
        Assert.Equal(filas, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Editar_sin_sesion_devuelve_401_no_autenticado_y_no_cambia_la_fila()
    {
        var id = await CrearAsync("Cuenta anónima", $"{Prefijo}anonima@javito.test");
        var antes = await CuentaAsync(id);
        var filas = await ContarCuentasAsync();

        var respuesta = await _anonimo.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Nombre anónimo",
            email = $"{Prefijo}anonima@javito.test"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("no_autenticado", await CodigoAsync(respuesta));

        await AfirmarIntactaAsync(id, antes, filas);
    }

    [Fact]
    public async Task Editar_como_usuario_devuelve_403_sin_permiso_y_no_cambia_la_fila()
    {
        var id = await CrearAsync("Cuenta sin permiso", $"{Prefijo}sin-permiso@javito.test");
        var antes = await CuentaAsync(id);
        var filas = await ContarCuentasAsync();

        var respuesta = await _usuario.PutAsJsonAsync($"/api/usuarios/{id}", new
        {
            nombre = "Nombre sin permiso",
            email = $"{Prefijo}sin-permiso@javito.test"
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("sin_permiso", await CodigoAsync(respuesta));

        await AfirmarIntactaAsync(id, antes, filas);
    }

    // --- Ayudas --------------------------------------------------------------------------------

    /// <summary>
    /// Crea la cuenta que se va a editar con el alta que ya existe (#85) y devuelve su id. La fila es
    /// propia de la prueba: las cuentas del seed no se tocan.
    /// </summary>
    private async Task<int> CrearAsync(string nombre, string email)
    {
        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre,
            email,
            password = Contrasena,
            rol = "Usuario"
        });
        respuesta.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        return documento.RootElement.GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Lee la cuenta por la ruta de detalle: así una prueba de rechazo compara el estado observable
    /// antes y después sin mirar una columna.
    /// </summary>
    private async Task<JsonElement> CuentaAsync(int id)
    {
        var respuesta = await _administrador.GetAsync($"/api/usuarios/{id}");
        respuesta.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        // Clon: el JsonElement dejaría de valer al salir del using.
        return documento.RootElement.Clone();
    }

    /// <summary>El rechazo no cambió nada: ni la fila ni la cantidad de cuentas.</summary>
    private async Task AfirmarIntactaAsync(int id, JsonElement antes, int filas)
    {
        var despues = await CuentaAsync(id);

        Assert.Equal(antes.GetProperty("nombre").GetString(), despues.GetProperty("nombre").GetString());
        Assert.Equal(antes.GetProperty("email").GetString(), despues.GetProperty("email").GetString());
        Assert.Equal(antes.GetProperty("rol").GetString(), despues.GetProperty("rol").GetString());
        Assert.Equal(antes.GetProperty("activo").GetBoolean(), despues.GetProperty("activo").GetBoolean());
        Assert.Equal(filas, await ContarCuentasAsync());
    }

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        return documento.RootElement.TryGetProperty("codigo", out var codigo)
            ? codigo.GetString()
            : null;
    }

    private async Task<JsonElement> LeerPaginaAsync(string ruta)
    {
        var respuesta = await _administrador.GetAsync(ruta);
        respuesta.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        return documento.RootElement.Clone();
    }

    private static List<JsonElement> Elementos(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().ToList();

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

    /// <summary>
    /// Borra sólo lo que creó esta clase, con un DELETE de verdad y no con la baja lógica de la
    /// aplicación: el listado del seed cuenta filas exactas y la limpieza corre antes y después.
    /// </summary>
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
