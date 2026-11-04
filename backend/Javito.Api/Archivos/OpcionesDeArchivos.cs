namespace Javito.Api.Archivos;

/// <summary>
/// Raíz física del almacén de archivos. Sale de <c>Archivos:Ruta</c> para que las pruebas apunten a
/// un temporal y no al <c>wwwroot</c> del proyecto; sin configurar, es la carpeta de siempre.
/// </summary>
public sealed record OpcionesDeArchivos(string Ruta);
