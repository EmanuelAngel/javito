using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// El alta y la lectura de artículos comparten la tabla del seed (tres filas activas y una retirada):
/// si las clases de ART corrieran en paralelo, las cuentas del catálogo dejarían de ser
/// deterministas. La colección las serializa contra la misma base recreada una vez por corrida.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionDeArticulos : ICollectionFixture<FabricaDeApi>
{
    public const string Nombre = "articulos";
}
