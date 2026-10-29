using MySqlConnector;

namespace Javito.Api.Datos;

/// <summary>Abre conexiones a MySQL. Sin ORM: el SQL vive en los repositorios.</summary>
public interface IDbConnectionFactory
{
    Task<MySqlConnection> AbrirAsync(CancellationToken cancellationToken = default);
}
