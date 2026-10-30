using MySqlConnector;

namespace Javito.Api.Datos;

public sealed class MySqlConnectionFactory(IConfiguration configuracion) : IDbConnectionFactory
{
    private readonly string _cadenaDeConexion =
        configuracion.GetConnectionString("Javito")
        ?? throw new InvalidOperationException(
            "Falta ConnectionStrings:Javito. Configurala con dotnet user-secrets.");

    public async Task<MySqlConnection> AbrirAsync(CancellationToken cancellationToken = default)
    {
        var conexion = new MySqlConnection(_cadenaDeConexion);
        await conexion.OpenAsync(cancellationToken);
        return conexion;
    }
}
