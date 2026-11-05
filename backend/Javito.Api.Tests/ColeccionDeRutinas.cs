using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// RUT-01 y RUT-02 comparten una base que se recrea una sola vez por corrida. Van en su propia
/// colección porque el resto de las pruebas no cuenta rutinas: lo único que podría ensuciarse es lo
/// que crea esta clase, y ella misma lo limpia.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionDeRutinas : ICollectionFixture<FabricaDeApi>
{
    public const string Nombre = "rutinas";
}
