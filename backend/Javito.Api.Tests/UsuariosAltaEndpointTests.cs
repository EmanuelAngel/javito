using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// USU-02 (HU-USU-02): el alta de una cuenta desde administración. Una prueba por regla de la
/// historia, y en cada rechazo se mira el <c>COUNT(*)</c> antes y después: la fila no se escribe.
/// La base se recrea una sola vez por corrida y el listado del seed afirma sobre exactamente cuatro
/// cuentas, así que todo lo que crea esta clase lleva el prefijo <see cref="Prefijo"/> en el email y
/// la clase lo borra con un DELETE de verdad antes y después de correr.
/// </summary>
[Collection(ColeccionDeUsuarios.Nombre)]
public class UsuariosAltaEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>
    /// Prefijo de los emails que crea esta clase. La limpieza borra por él y nada más: el resto de
    /// la tabla es del seed y de las otras pruebas de la colección.
    /// </summary>
    private const string Prefijo = "prueba-alta-";

    private const string Contrasena = "Secreta2026";

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

    // --- S5, S7 y S14: el alta devuelve la cuenta con el rol elegido y sin el hash --------------

    [Fact]
    public async Task Crear_devuelve_201_con_la_cuenta_activa_el_rol_elegido_y_sin_rastro_del_hash()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta con rol",
            email = $"{Prefijo}rol@javito.test",
            password = Contrasena,
            rol = "Administrador"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        // L13: no hay GET api/usuarios/{id} en esta parte, así que no se manda un Location que
        // llevaría a un 404.
        Assert.Null(respuesta.Headers.Location);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var creada = documento.RootElement;

        // S14: los únicos campos del cuerpo son los del listado. Ninguno se llama como el hash.
        Assert.Equal(
            ["id", "nombre", "email", "rol", "activo"],
            creada.EnumerateObject().Select(campo => campo.Name));

        Assert.True(creada.GetProperty("id").GetInt32() > 4, "La cuenta es una fila nueva del alta.");
        Assert.Equal("Alta con rol", creada.GetProperty("nombre").GetString());
        Assert.Equal($"{Prefijo}rol@javito.test", creada.GetProperty("email").GetString());
        Assert.Equal("Administrador", creada.GetProperty("rol").GetString());
        Assert.True(creada.GetProperty("activo").GetBoolean(), "La cuenta nace activa (L14).");

        Assert.Equal(antes + 1, await ContarCuentasAsync());
    }

    // --- S6: la contraseña la elige el administrador y se guarda hasheada -----------------------

    [Fact]
    public async Task Crear_guarda_la_contrasena_hasheada_y_la_cuenta_nueva_puede_iniciar_sesion()
    {
        var email = $"{Prefijo}login@javito.test";

        await CrearAsync("Alta para entrar", email, Contrasena, "Usuario");

        // La prueba no lee password_hash: entra con la contraseña que escribió el administrador. Si
        // la columna guardara el texto plano, BCrypt.Verify no podría verificarlo y el login fallaría.
        using var cliente = fabrica.CreateClient();
        var sesion = await cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email, password = Contrasena });

        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);

        var iniciada = await sesion.Content.ReadFromJsonAsync<SesionDto>();
        Assert.NotNull(iniciada);
        Assert.Equal(email, iniciada.Usuario.Email);
        Assert.Equal("Usuario", iniciada.Usuario.Rol);

        // La contraseña guardada es la que se escribió, no cualquier hash: con otra, el login falla.
        var otra = await cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email, password = "OtraDistinta2026" });

        Assert.Equal(HttpStatusCode.Unauthorized, otra.StatusCode);
    }

    // --- S13: el email repetido se avisa antes, sin depender del UNIQUE --------------------------

    [Fact]
    public async Task Crear_con_un_email_que_solo_cambia_en_mayusculas_devuelve_409_email_repetido_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        // La intercalación de la columna es insensible a mayúsculas: "ADMIN@Javito.Local" es la
        // misma cuenta que "admin@javito.local", que ya está en el seed.
        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Copia de la administración",
            email = "ADMIN@Javito.Local",
            password = Contrasena,
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("email_repetido", await CodigoAsync(respuesta));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    // --- S8: las mismas reglas de email y contraseña que el registro ----------------------------

    [Fact]
    public async Task Crear_con_una_contrasena_corta_devuelve_400_nombrando_el_campo_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta con contraseña corta",
            email = $"{Prefijo}corta@javito.test",
            password = "Corta12",
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(
            await ContieneElCampoInvalidoAsync(respuesta, "Password"),
            "El 400 tiene que nombrar el campo Password en errors.");
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Crear_con_una_contrasena_de_73_caracteres_devuelve_400_nombrando_el_campo_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        // El tope de la contraseña son 72 caracteres y no 100: BCrypt hashea como máximo 72 bytes y
        // trunca en silencio, así que lo que pase de ahí no cambia el hash y sólo da la ilusión de
        // seguridad. 73 es el primer largo que ya no verifica distinto.
        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta con contraseña larga",
            email = $"{Prefijo}larga@javito.test",
            password = new string('a', 73),
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(
            await ContieneElCampoInvalidoAsync(respuesta, "Password"),
            "El 400 tiene que nombrar el campo Password en errors.");
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Crear_con_una_contrasena_de_menos_de_72_caracteres_pero_mas_de_72_bytes_devuelve_400_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        // 40 caracteres y 80 bytes en UTF-8: por largo de cadena pasa, por bytes no. Esta es la
        // regla que un StringLength no puede expresar, y sin ella BCrypt truncaría la contraseña
        // sin avisar: dos cuentas con los primeros 72 bytes iguales se autenticarían entre sí.
        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta con contraseña de bytes",
            email = $"{Prefijo}bytes@javito.test",
            password = new string('é', 40),
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(
            await ContieneElCampoInvalidoAsync(respuesta, "Password"),
            "El 400 tiene que nombrar el campo Password en errors.");
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Crear_con_un_email_invalido_devuelve_400_nombrando_el_campo_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta con email inválido",
            email = $"{Prefijo}sin-arroba",
            password = Contrasena,
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(
            await ContieneElCampoInvalidoAsync(respuesta, "Email"),
            "El 400 tiene que nombrar el campo Email en errors.");
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Crear_normaliza_el_email_a_minusculas_y_sin_espacios()
    {
        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta normalizada",
            email = "  Prueba-Alta-Mixta@Javito.Test  ",
            password = Contrasena,
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(
            "prueba-alta-mixta@javito.test",
            documento.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Crear_y_buscar_devuelve_el_email_normalizado_y_no_el_que_se_escribio()
    {
        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta normalizada en la fila",
            email = $"  {Prefijo}MiXeD-Mayusculas@Javito.TEST  ",
            password = Contrasena,
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        // Se verifica por la interfaz pública y no leyendo la columna: el listado tiene que
        // encontrar la cuenta con el email ya normalizado, no con el texto que se escribió.
        const string normalizado = "prueba-alta-mixed-mayusculas@javito.test";
        var creada = Assert.Single(Elementos(await LeerPaginaAsync($"/api/usuarios?q={normalizado}")));

        Assert.Equal(normalizado, creada.GetProperty("email").GetString());
    }

    // --- S7: el rol sale de la lista, no del ENUM de la base ------------------------------------

    [Fact]
    public async Task Crear_con_un_rol_fuera_de_la_lista_devuelve_400_rol_invalido_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta con rol inventado",
            email = $"{Prefijo}rol-invalido@javito.test",
            password = Contrasena,
            rol = "Superusuario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("rol_invalido", await CodigoAsync(respuesta));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Crear_con_el_rol_en_minusculas_devuelve_400_rol_invalido_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        // La lista se compara con Ordinal, no con la intercalación del ENUM de la base: "administrador"
        // en minúsculas no es un rol elegible aunque MySQL lo aceptara, y el rechazo no deja fila.
        var respuesta = await _administrador.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta con rol en minúsculas",
            email = $"{Prefijo}rol-minusculas@javito.test",
            password = Contrasena,
            rol = "administrador"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("rol_invalido", await CodigoAsync(respuesta));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    // --- S12: sólo el Administrador, y un rechazo no toca ninguna fila --------------------------

    [Fact]
    public async Task Crear_sin_sesion_devuelve_401_no_autenticado_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await _anonimo.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta anónima",
            email = $"{Prefijo}anonima@javito.test",
            password = Contrasena,
            rol = "Usuario"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("no_autenticado", await CodigoAsync(respuesta));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    [Fact]
    public async Task Crear_como_usuario_devuelve_403_sin_permiso_y_no_crea_fila()
    {
        var antes = await ContarCuentasAsync();

        var respuesta = await _usuario.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Alta sin permiso",
            email = $"{Prefijo}sin-permiso@javito.test",
            password = Contrasena,
            rol = "Administrador"
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("sin_permiso", await CodigoAsync(respuesta));
        Assert.Equal(antes, await ContarCuentasAsync());
    }

    // --- Ayudas -------------------------------------------------------------------------------

    private async Task CrearAsync(string nombre, string email, string password, string rol)
    {
        var respuesta = await _administrador.PostAsJsonAsync(
            "/api/usuarios",
            new { nombre, email, password, rol });
        respuesta.EnsureSuccessStatusCode();
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

        // Clon: el JsonElement dejaría de valer al salir del using y la prueba lo lee afuera.
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
