using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// CAT-01…CAT-04: alta, edición y baja lógica del catálogo. Una prueba por regla de las historias.
/// La base se recrea una sola vez por corrida, así que lo que crea cada prueba lleva el prefijo
/// <see cref="Prefijo"/> y la clase limpia lo suyo antes y después de correr: el listado del seed
/// afirma sobre exactamente cinco filas.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class CategoriasEscrituraEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "Prueba ";

    private readonly HttpClient _admin = fabrica.CreateClient();
    private readonly HttpClient _usuario = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAdministradorAsync(_admin);
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _admin.Dispose();
        _usuario.Dispose();
    }

    // --- CAT-02: alta -------------------------------------------------------------------------

    [Fact]
    public async Task Crear_devuelve_201_con_la_categoria_activa_y_su_location()
    {
        var respuesta = await _admin.PostAsJsonAsync(
            "/api/categorias",
            new { nombre = $"{Prefijo}Cuerpo", descripcion = "Alta desde el ABM" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var creada = await respuesta.Content.ReadFromJsonAsync<CategoriaDto>();
        Assert.NotNull(creada);
        Assert.True(creada.Id > 5, "La categoría creada tiene que ser una fila nueva, no del seed.");
        Assert.Equal($"{Prefijo}Cuerpo", creada.Nombre);
        Assert.Equal("Alta desde el ABM", creada.Descripcion);
        Assert.True(creada.Activo);
        Assert.Equal($"/api/categorias/{creada.Id}", respuesta.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Crear_con_un_nombre_que_ya_existe_en_otra_caja_devuelve_409_nombre_repetido()
    {
        var respuesta = await _admin.PostAsJsonAsync("/api/categorias", new { nombre = "cUeRpO" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problema.GetProperty("status").GetInt32());
        Assert.Equal("nombre_repetido", problema.GetProperty("codigo").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890123456789012345678901234567890123456789012345678901")]
    public async Task Crear_con_un_nombre_invalido_devuelve_400_con_el_error_del_campo(string nombre)
    {
        var respuesta = await _admin.PostAsJsonAsync("/api/categorias", new { nombre });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(
            problema.GetProperty("errors").TryGetProperty("Nombre", out var mensajes),
            "El 400 tiene que nombrar el campo Nombre en errors.");
        Assert.NotEmpty(mensajes.EnumerateArray());
    }

    [Fact]
    public async Task Crear_sin_descripcion_la_guarda_en_null()
    {
        var sinDescripcion = await _admin.PostAsJsonAsync(
            "/api/categorias",
            new { nombre = $"{Prefijo}Sin descripción" });
        var descripcionVacia = await _admin.PostAsJsonAsync(
            "/api/categorias",
            new { nombre = $"{Prefijo}Descripción vacía", descripcion = "" });

        Assert.Equal(HttpStatusCode.Created, sinDescripcion.StatusCode);
        Assert.Equal(HttpStatusCode.Created, descripcionVacia.StatusCode);

        var primera = await sinDescripcion.Content.ReadFromJsonAsync<CategoriaDto>();
        var segunda = await descripcionVacia.Content.ReadFromJsonAsync<CategoriaDto>();
        Assert.Null(primera?.Descripcion);
        Assert.Null(segunda?.Descripcion);
    }

    // --- CAT-03: edición ---------------------------------------------------------------------

    [Fact]
    public async Task Editar_guarda_el_nombre_y_la_descripcion()
    {
        var creada = await CrearAsync($"{Prefijo}Para editar");

        var respuesta = await _admin.PutAsJsonAsync(
            $"/api/categorias/{creada.Id}",
            new { nombre = $"{Prefijo}Editada", descripcion = "Descripción nueva" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var editada = await respuesta.Content.ReadFromJsonAsync<CategoriaDto>();
        Assert.NotNull(editada);
        Assert.Equal($"{Prefijo}Editada", editada.Nombre);
        Assert.Equal("Descripción nueva", editada.Descripcion);

        var listado = await _admin.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias");
        Assert.Contains(
            listado!,
            categoria => categoria.Id == creada.Id && categoria.Nombre == $"{Prefijo}Editada");
    }

    [Fact]
    public async Task Editar_con_el_nombre_de_otra_categoria_devuelve_409_nombre_repetido()
    {
        var creada = await CrearAsync($"{Prefijo}Choca");

        var respuesta = await _admin.PutAsJsonAsync(
            $"/api/categorias/{creada.Id}",
            new { nombre = "Mente" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("nombre_repetido", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Editar_un_id_inexistente_devuelve_404_no_encontrado()
    {
        var respuesta = await _admin.PutAsJsonAsync(
            "/api/categorias/999999",
            new { nombre = $"{Prefijo}Fantasma" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_encontrado", problema.GetProperty("codigo").GetString());
    }

    /// <summary>
    /// HU-CAT-03: cambiar el nombre no afecta a los hábitos que la usan. Se comprueba sobre la
    /// categoría del seed que tiene hábitos y contra la base, porque todavía no hay endpoint de
    /// hábitos: la fila tiene que seguir siendo la misma (sin borrar y volver a insertar).
    /// </summary>
    [Fact]
    public async Task Editar_el_nombre_no_toca_los_habitos_que_usan_la_categoria()
    {
        const int cuerpo = 1;

        var habitosAntes = await ContarHabitosAsync(cuerpo);
        Assert.True(
            habitosAntes > 0,
            "El seed tiene que tener hábitos con categoría: si no, la prueba no prueba nada.");

        try
        {
            var respuesta = await _admin.PutAsJsonAsync(
                $"/api/categorias/{cuerpo}",
                new { nombre = "Cuerpo físico", descripcion = "Movilidad, postura y actividad física" });

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal(habitosAntes, await ContarHabitosAsync(cuerpo));
        }
        finally
        {
            await _admin.PutAsJsonAsync(
                $"/api/categorias/{cuerpo}",
                new { nombre = "Cuerpo", descripcion = "Movilidad, postura y actividad física" });
        }
    }

    // --- CAT-04: baja lógica ------------------------------------------------------------------

    [Fact]
    public async Task Desactivar_conserva_la_fila_y_la_saca_de_las_activas()
    {
        var creada = await CrearAsync($"{Prefijo}Para desactivar");

        var respuesta = await _admin.PutAsJsonAsync(
            $"/api/categorias/{creada.Id}/activo",
            new { activo = false });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var desactivada = await respuesta.Content.ReadFromJsonAsync<CategoriaDto>();
        Assert.NotNull(desactivada);
        Assert.False(desactivada.Activo);

        // La fila sigue existiendo: la baja es lógica (HU-CAT-04).
        var todas = await _admin.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias");
        Assert.Contains(todas!, categoria => categoria.Id == creada.Id && !categoria.Activo);

        // Se va del selector, que pide sólo las activas, y sigue en el listado de inactivas.
        var activas = await _admin.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias?activo=true");
        Assert.DoesNotContain(activas!, categoria => categoria.Id == creada.Id);

        var inactivas = await _admin.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias?activo=false");
        Assert.Contains(inactivas!, categoria => categoria.Id == creada.Id);
    }

    [Fact]
    public async Task Reactivar_la_devuelve_a_las_activas()
    {
        var creada = await CrearAsync($"{Prefijo}Para reactivar");
        await _admin.PutAsJsonAsync($"/api/categorias/{creada.Id}/activo", new { activo = false });

        var respuesta = await _admin.PutAsJsonAsync(
            $"/api/categorias/{creada.Id}/activo",
            new { activo = true });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var activas = await _admin.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias?activo=true");
        Assert.Contains(activas!, categoria => categoria.Id == creada.Id);
    }

    [Fact]
    public async Task Cambiar_activo_al_estado_que_ya_tiene_devuelve_200()
    {
        var creada = await CrearAsync($"{Prefijo}Idempotente");

        var primera = await _admin.PutAsJsonAsync(
            $"/api/categorias/{creada.Id}/activo",
            new { activo = false });
        var segunda = await _admin.PutAsJsonAsync(
            $"/api/categorias/{creada.Id}/activo",
            new { activo = false });

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
    }

    [Fact]
    public async Task Cambiar_activo_sin_declarar_el_estado_devuelve_400()
    {
        var creada = await CrearAsync($"{Prefijo}Sin estado");

        var respuesta = await _admin.PutAsJsonAsync(
            $"/api/categorias/{creada.Id}/activo",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty("Activo", out _));
    }

    [Fact]
    public async Task Cambiar_activo_de_un_id_inexistente_devuelve_404_no_encontrado()
    {
        var respuesta = await _admin.PutAsJsonAsync(
            "/api/categorias/999999/activo",
            new { activo = false });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_encontrado", problema.GetProperty("codigo").GetString());
    }

    // --- Roles y sesión -----------------------------------------------------------------------

    [Fact]
    public async Task Escribir_con_rol_usuario_devuelve_403_sin_permiso()
    {
        var alta = await _usuario.PostAsJsonAsync(
            "/api/categorias",
            new { nombre = $"{Prefijo}Sin permiso" });
        var baja = await _usuario.PutAsJsonAsync(
            "/api/categorias/1/activo",
            new { activo = false });

        Assert.Equal(HttpStatusCode.Forbidden, alta.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, baja.StatusCode);

        var problema = await alta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("sin_permiso", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Listar_con_rol_usuario_devuelve_200()
    {
        var respuesta = await _usuario.GetAsync("/api/categorias");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Escribir_sin_token_devuelve_401_no_autenticado()
    {
        using var anonimo = fabrica.CreateClient();

        var respuesta = await anonimo.PostAsJsonAsync(
            "/api/categorias",
            new { nombre = $"{Prefijo}Anónima" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());
    }

    // --- Ayudas -------------------------------------------------------------------------------

    private async Task<CategoriaDto> CrearAsync(string nombre)
    {
        var respuesta = await _admin.PostAsJsonAsync("/api/categorias", new { nombre });
        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<CategoriaDto>()
            ?? throw new InvalidOperationException("El alta no devolvió la categoría.");
    }

    private static async Task<int> ContarHabitosAsync(int categoriaId)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(
            "SELECT COUNT(*) FROM habito WHERE categoria_id = @id",
            conexion);
        comando.Parameters.AddWithValue("@id", categoriaId);

        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    /// <summary>
    /// Borra sólo lo que creó esta clase. Es un DELETE de verdad y no la baja lógica de la aplicación:
    /// la base de pruebas se recrea en cada corrida y el listado del seed cuenta filas exactas.
    /// </summary>
    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(
            "DELETE FROM categoria WHERE nombre LIKE 'Prueba %'",
            conexion);
        await comando.ExecuteNonQueryAsync();
    }
}
