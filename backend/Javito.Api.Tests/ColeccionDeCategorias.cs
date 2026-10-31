using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// CAT-01…CAT-04 comparten una base que se recrea una sola vez por corrida: el listado afirma sobre
/// las cinco categorías del seed y la escritura crea, edita y desactiva filas. Si las dos clases
/// corrieran en paralelo, las cuentas sobre el seed dejarían de ser deterministas.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionDeCategorias : ICollectionFixture<FabricaDeApi>
{
    public const string Nombre = "categorias";
}
