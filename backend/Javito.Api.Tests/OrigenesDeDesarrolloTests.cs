using Javito.Api.Cors;

namespace Javito.Api.Tests;

/// <summary>El SPA se prueba desde el celular: su origen es la IP de la PC, no `localhost`.</summary>
public class OrigenesDeDesarrolloTests
{
    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://127.0.0.1:5173")]
    [InlineData("http://192.168.0.10:5173")]
    [InlineData("http://10.1.2.3:5173")]
    [InlineData("http://172.20.0.5:5173")]
    public void Un_origen_de_la_red_local_se_acepta(string origen)
    {
        Assert.True(OrigenesDeDesarrollo.EsDeLaRedLocal(origen));
    }

    [Theory]
    [InlineData("http://8.8.8.8:5173")]
    [InlineData("http://172.32.0.5:5173")]
    [InlineData("https://javito.example.com")]
    [InlineData("ftp://192.168.0.10")]
    [InlineData("no-es-un-origen")]
    public void Un_origen_publico_o_mal_formado_se_rechaza(string origen)
    {
        Assert.False(OrigenesDeDesarrollo.EsDeLaRedLocal(origen));
    }
}
