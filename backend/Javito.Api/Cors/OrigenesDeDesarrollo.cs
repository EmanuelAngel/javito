using System.Net;
using System.Net.Sockets;

namespace Javito.Api.Cors;

/// <summary>
/// En desarrollo el SPA también se abre desde el celular con `pnpm dev:web -- --host`, y entonces el
/// origen del navegador es la IP de la PC, no `localhost`. Reconocerla evita editar
/// `Cors:AllowedOrigins` cada vez que el router cambia la IP; en producción la política sigue siendo
/// sólo la lista configurada.
/// </summary>
public static class OrigenesDeDesarrollo
{
    /// <summary>
    /// Cierto para `http://` o `https://` con host de bucle (`localhost`, `127.0.0.1`, `::1`) o con
    /// una IP de red privada, que es la que reparte el router a la PC en la LAN.
    /// </summary>
    public static bool EsDeLaRedLocal(string origen)
    {
        if (!Uri.TryCreate(origen, UriKind.Absolute, out var uri)) return false;

        if (uri.Scheme is not ("http" or "https")) return false;

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;

        return IPAddress.TryParse(uri.Host, out var ip) && (IPAddress.IsLoopback(ip) || EsPrivada(ip));
    }

    private static bool EsPrivada(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // fc00::/7: direcciones locales únicas, el equivalente IPv6 de la red privada.
            return (bytes[0] & 0xFE) == 0xFC;
        }

        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }
}
