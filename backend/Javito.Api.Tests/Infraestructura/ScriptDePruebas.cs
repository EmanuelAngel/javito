using System.Text.RegularExpressions;

namespace Javito.Api.Tests.Infraestructura;

/// <summary>
/// Adapta database.sql al schema de pruebas. Todo lo que nombra una base pasa por acá y después se
/// verifica: si el script suma otra sentencia de schema, esto falla antes de tocar MySQL en vez de
/// borrar la base de desarrollo.
/// </summary>
internal static partial class ScriptDePruebas
{
    public static string LeerDatabaseSql() => File.ReadAllText(RutaDeDatabaseSql());

    public static string Adaptar(string script)
    {
        // El DROP del script apuntaba a la base de desarrollo y la borraba en cada corrida.
        var adaptado = PatronDrop().Replace(
            script,
            $"DROP DATABASE IF EXISTS {BaseDeDatosDePrueba.Nombre}");
        adaptado = PatronCreate().Replace(
            adaptado,
            $"CREATE DATABASE {BaseDeDatosDePrueba.Nombre} ");
        adaptado = PatronUse().Replace(adaptado, $"USE {BaseDeDatosDePrueba.Nombre};");

        foreach (Match sentencia in PatronSentenciaDeBase().Matches(adaptado))
        {
            var baseNombrada = sentencia.Groups["base"].Value;
            if (!baseNombrada.Equals(BaseDeDatosDePrueba.Nombre, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"El script de pruebas nombra la base '{baseNombrada}'. Solo se admite " +
                    $"'{BaseDeDatosDePrueba.Nombre}': revisá las sustituciones de ScriptDePruebas.Adaptar.");
            }
        }

        return adaptado;
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

    [GeneratedRegex(@"DROP\s+DATABASE\s+IF\s+EXISTS\s+javito\b")]
    private static partial Regex PatronDrop();

    [GeneratedRegex(@"CREATE\s+DATABASE\s+javito\b")]
    private static partial Regex PatronCreate();

    [GeneratedRegex(@"USE\s+javito\s*;")]
    private static partial Regex PatronUse();

    /// <summary>Red de seguridad: toda sentencia que nombre una base tiene que apuntar al schema de pruebas.</summary>
    [GeneratedRegex(@"(?im)^\s*(?:DROP\s+DATABASE(?:\s+IF\s+EXISTS)?|CREATE\s+DATABASE|USE)\s+(?<base>[A-Za-z0-9_]+)")]
    private static partial Regex PatronSentenciaDeBase();
}
