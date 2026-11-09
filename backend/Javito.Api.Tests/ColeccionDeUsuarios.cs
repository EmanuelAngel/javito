using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// USU-01…USU-03 comparten el schema de pruebas, que se recrea una sola vez por corrida. El listado
/// afirma sobre las cuatro cuentas del seed —una Administrador, dos Usuario activas y una
/// desactivada—, así que las clases del módulo comparten el fixture en vez de correr en paralelo y
/// pisarse esas cuentas.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionDeUsuarios : ICollectionFixture<FabricaDeApi>
{
    public const string Nombre = "usuarios";
}
