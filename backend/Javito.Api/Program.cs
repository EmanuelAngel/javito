using Javito.Api.Contratos;
using Javito.Api.Datos;
using Javito.Api.Repositorios;
using Scalar.AspNetCore;

const string PoliticaCorsSpa = "SpaJavito";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Todo error sale como ProblemDetails: el 400 de validación, el 404 de una ruta inexistente
// y el 500 que nadie atajó. Los errores de negocio los traduce su propio manejador.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErroresDeNegocio>();

builder.Services.AddScoped<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();

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
    app.MapScalarApiReference();
}
else
{
    // En desarrollo el SPA consume la API por http; redirigir rompería el consumo local.
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors(PoliticaCorsSpa);

app.UseAuthorization();

app.MapControllers();

app.Run();

// La clase Program que generan las instrucciones de nivel superior es interna.
// Esta declaración la expone para que el proyecto de pruebas pueda alojar la API.
public partial class Program { }
