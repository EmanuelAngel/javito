const string PoliticaCorsSpa = "SpaJavito";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var origenesPermitidos = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(PoliticaCorsSpa, policy =>
    {
        // Fail-closed: sin orígenes configurados la política no habilita ninguno.
        if (origenesPermitidos.Length > 0)
        {
            policy.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // En desarrollo el SPA consume la API por http; redirigir rompería el consumo local.
    app.UseHttpsRedirection();
}

app.UseCors(PoliticaCorsSpa);

app.UseAuthorization();

app.MapControllers();

app.Run();

// La clase Program que generan las instrucciones de nivel superior es interna.
// Esta declaración la expone para que el proyecto de pruebas pueda alojar la API.
public partial class Program { }
