using Javito.Api.Contratos;
using Javito.Api.Datos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

const string PoliticaCorsSpa = "SpaJavito";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Todo error sale como ProblemDetails: el 400 de validación, el 401 y el 403 de JwtBearer, el 404 de
// una ruta inexistente y el 500 que nadie atajó. Los errores de negocio los traduce su manejador.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErroresDeNegocio>();

builder.Services.AddScoped<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

// Sin Jwt:Clave no hay API que arranque: el fallo ocurre al construir la aplicación y no en la
// primera request, con un mensaje que dice cómo cargar la clave.
var jwt = OpcionesDeJwt.Desde(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<ServicioDeTokens>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        // Los claims viajan tal como se firmaron (sub, rol, nombre, email). Sin esto ASP.NET Core
        // los remapea a los nombres de ClaimTypes y el SPA recibiría otra cosa que la documentada.
        opciones.MapInboundClaims = false;

        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.ClaveDeFirma,
            ValidateLifetime = true,
            // 30 s y no los 5 min por defecto: la sesión dura 8 h y el reloj no tiene por qué mentir.
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ReclamosDeSesion.Rol,
            NameClaimType = ReclamosDeSesion.Nombre
        };

        opciones.Events = new JwtBearerEvents
        {
            OnChallenge = contexto =>
            {
                contexto.HandleResponse();
                return RespuestasDeError.EscribirAsync(contexto.HttpContext, new ErrorDeNegocio(
                    "no_autenticado",
                    "Sesión requerida",
                    "Falta el token o ya venció. Volvé a iniciar sesión.",
                    StatusCodes.Status401Unauthorized));
            },
            OnForbidden = contexto => RespuestasDeError.EscribirAsync(contexto.HttpContext, new ErrorDeNegocio(
                "sin_permiso",
                "Acceso denegado",
                "Tu rol no permite esta operación.",
                StatusCodes.Status403Forbidden))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi(opciones => opciones.AddDocumentTransformer<EsquemaDeSeguridadEnOpenApi>());

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

// El orden importa: sin UseAuthentication, UseAuthorization no tiene principal que autorizar y
// [Authorize] rechazaría toda request con token válido.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// La clase Program que generan las instrucciones de nivel superior es interna.
// Esta declaración la expone para que el proyecto de pruebas pueda alojar la API.
public partial class Program { }
