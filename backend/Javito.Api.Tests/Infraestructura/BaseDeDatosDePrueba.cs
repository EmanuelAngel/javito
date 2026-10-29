using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Javito.Api.Tests.Infraestructura;

/// <summary>
/// Prepara el schema de pruebas una vez por corrida: recrea javito_test ejecutando database.sql
/// con el nombre de base sustituido. Nunca toca la base de desarrollo.
/// </summary>
internal static partial class BaseDeDatosDePrueba
{
    public const string Nombre = "javito_test";

    private const string CadenaPorDefecto = "Server=localhost;Port=3306;User ID=root;Database=javito";

    private static readonly Lazy<Task> Preparacion =
        new(PrepararAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string CadenaDeConexion =>
        new MySqlConnectionStringBuilder(CadenaConfigurada()) { Database = Nombre }.ConnectionString;

    public static Task AsegurarAsync() => Preparacion.Value;

    private static string CadenaConfigurada()
    {
        var delEntorno = Environment.GetEnvironmentVariable("ConnectionStrings__Javito");
        if (!string.IsNullOrWhiteSpace(delEntorno))
        {
            return delEntorno;
        }

        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        return configuracion.GetConnectionString("Javito") ?? CadenaPorDefecto;
    }

    private static async Task PrepararAsync()
    {
        if (!Nombre.EndsWith("_test", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("El fixture solo recrea bases cuyo nombre termina en _test.");
        }

        var script = await File.ReadAllTextAsync(RutaDeDatabaseSql());
        script = PatronCreateDatabase().Replace(script, $"CREATE DATABASE {Nombre} ");
        script = PatronUse().Replace(script, $"USE {Nombre};");

        // database.sql usa variables de sesión (SET @lunes = ...) para cargar fechas relativas.
        var sinBase = new MySqlConnectionStringBuilder(CadenaDeConexion)
        {
            Database = string.Empty,
            AllowUserVariables = true
        };

        await using var conexion = new MySqlConnection(sinBase.ConnectionString);
        try
        {
            await conexion.OpenAsync();
        }
        catch (MySqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo conectar a MySQL en localhost:3306. Corré net start MySQL80 y configurá la " +
                "contraseña con: dotnet user-secrets set \"ConnectionStrings:Javito\" \"Server=localhost;" +
                "Port=3306;Database=javito;User ID=root;Password=...\" --project backend/Javito.Api. " +
                $"Detalle: {excepcion.Message}",
                excepcion);
        }

        await using (var borrar = new MySqlCommand($"DROP DATABASE IF EXISTS {Nombre}", conexion))
        {
            await borrar.ExecuteNonQueryAsync();
        }

        using var lote = new MySqlBatch(conexion);
        foreach (var sentencia in Sentencias(script))
        {
            lote.BatchCommands.Add(new MySqlBatchCommand(sentencia));
        }

        await lote.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Divide el script por el punto y coma de fin de línea. database.sql no tiene puntos y coma
    /// dentro de literales, así que la regla alcanza y no hace falta un parser de SQL.
    /// </summary>
    private static IEnumerable<string> Sentencias(string script)
    {
        var sentencia = new StringBuilder();
        foreach (var linea in script.Split('\n'))
        {
            var limpia = linea.TrimEnd('\r');
            if (limpia.TrimStart().StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            sentencia.AppendLine(limpia);
            if (!limpia.TrimEnd().EndsWith(';'))
            {
                continue;
            }

            yield return sentencia.ToString().Trim();
            sentencia.Clear();
        }
    }

    private static string RutaDeDatabaseSql()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "database.sql")))
        {
            directorio = directorio.Parent;
        }

        return directorio is null
            ? throw new FileNotFoundException("No se encontró database.sql en el árbol del repositorio.")
            : Path.Combine(directorio.FullName, "database.sql");
    }

    [GeneratedRegex(@"CREATE DATABASE\s+javito\b")]
    private static partial Regex PatronCreateDatabase();

    [GeneratedRegex(@"USE\s+javito\s*;")]
    private static partial Regex PatronUse();
}
