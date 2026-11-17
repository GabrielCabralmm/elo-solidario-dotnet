using System.Text.Json.Serialization;
using EloSolidario.Api.Data;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Endpoints;
using EloSolidario.Api.Infra.Erros;
using EloSolidario.Api.Infra.Idempotencia;
using EloSolidario.Api.Infra.LimiteRequisicoes;
using EloSolidario.Api.Infra.Saude;
using EloSolidario.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados
builder.Services.AddDbContext<EloDbContext>(opcoes =>
    opcoes.UseOracle(builder.Configuration.GetConnectionString("Oracle")));
builder.Services.AddScoped<IImpactoRepository, ImpactoRepository>();

// Serviços
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<IdempotenciaFilter>();
builder.Services.ConfigureHttpJsonOptions(opcoes =>
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Autenticação e autorização
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TokenService.Emissor,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = TokenService.CriarChave(builder.Configuration["Jwt:Secret"] ?? string.Empty),
            NameClaimType = "sub",
            RoleClaimType = TokenService.ClaimPerfil,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Admin, politica => politica.RequireRole(Perfil.Admin));

// Limite de requisições
builder.Services.AddLimiteRequisicoes(builder.Configuration);

// Health checks
builder.Services.AddHttpClient(ApiJavaHealthCheck.NomeCliente, cliente =>
{
    var url = builder.Configuration["ApiJava:Url"];
    if (!string.IsNullOrWhiteSpace(url))
    {
        cliente.BaseAddress = new Uri(url.TrimEnd('/') + "/");
    }
    cliente.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHealthChecks()
    .AddCheck("api", () => HealthCheckResult.Healthy("API .NET no ar"), tags: new[] { "live" })
    .AddCheck<OracleHealthCheck>("oracle", tags: new[] { "ready" })
    .AddCheck<ApiJavaHealthCheck>("api-java", tags: new[] { "ready" });

// Documentação
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EloSolidário API .NET",
        Version = "v1",
        Description = "Catálogo de alimentos, painel de impacto e vitrine anti-desperdício - Global Solution 2026/2 FIAP"
    });
    opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe o token obtido em /auth/login"
    });
    opcoes.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorExcecoes>();
builder.Services.AddCors(opcoes => opcoes.AddDefaultPolicy(politica =>
    politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After", IdempotenciaFilter.CabecalhoReplay)));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI(opcoes => opcoes.DocumentTitle = "EloSolidário API .NET");
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = RespostaSaude.EscreverAsync })
    .DisableRateLimiting();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = verificacao => verificacao.Tags.Contains("live"),
    ResponseWriter = RespostaSaude.EscreverAsync
}).DisableRateLimiting();

app.MapAuthEndpoints();
app.MapCatalogoEndpoints();
app.MapImpactoEndpoints();

app.Run();

public partial class Program
{
}
