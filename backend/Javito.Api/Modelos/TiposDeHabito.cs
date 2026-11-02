namespace Javito.Api.Modelos;

/// <summary>
/// Valores de los ENUM de <c>habito</c>. Los dicta database.sql y viajan al JSON tal cual: el SPA
/// decide por ellos, así que se escriben una sola vez acá en lugar de como literales sueltos.
/// </summary>
public static class TiposDeHabito
{
    public const string SiNo = "si_no";

    public const string Contador = "contador";

    public const string Abstinencia = "abstinencia";

    public const string Diaria = "diaria";

    public const string Semanal = "semanal";
}
