using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using ProjectAtmaca.Api.Security;
using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(
    options =>
    {
        options.IncludeScopes =
            true;
    });

// Add services to the container.

builder.Services
    .AddOptions<ApiAuthenticationOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ApiAuthenticationOptions.SectionName))
    .Validate(
        static options =>
            Uri.TryCreate(
                options.Authority,
                UriKind.Absolute,
                out Uri? authority) &&
            authority is not null &&
            string.Equals(
                authority.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase),
        "Authentication:Authority must be configured " +
        "as an absolute HTTPS URI.")
    .Validate(
        static options =>
            !string.IsNullOrWhiteSpace(
                options.Audience),
        "Authentication:Audience is required.")
    .ValidateOnStart();

bool isDevelopment =
    builder.Environment.IsDevelopment();

// Requires an explicit opt-in (never set by the test host's
// WebApplicationFactory) in addition to Development, so security/
// production-composition tests keep exercising real authentication.
bool developmentActorBypassEnabled =
    isDevelopment &&
    string.Equals(
        Environment.GetEnvironmentVariable(
            "PROJECTATMACA_ENABLE_DEV_BYPASS"),
        "true",
        StringComparison.OrdinalIgnoreCase);

var authenticationBuilder = builder.Services
    .AddAuthentication(
        developmentActorBypassEnabled
            ? DevelopmentActorAuthenticationHandler
                .SchemeName
            : JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.MapInboundClaims =
                false;

            options.RequireHttpsMetadata =
                true;

            options.IncludeErrorDetails =
                false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };
        });

// Development-only: authenticate every request as a fixed local actor
// and grant every permission, so a local UI client can call the API
// without standing up a full identity provider. Never active outside
// Development (see DevelopmentActorAuthenticationHandler/AllowAllPermissionEvaluator).
if (developmentActorBypassEnabled)
{
    authenticationBuilder.AddScheme<
        AuthenticationSchemeOptions,
        DevelopmentActorAuthenticationHandler>(
        DevelopmentActorAuthenticationHandler
            .SchemeName,
        _ => { });
}

builder.Services.AddHttpContextAccessor();

builder.Services.Replace(
    ServiceDescriptor.Scoped<
        IClaimsTransformation,
        ActorClaimsTransformation>());

builder.Services.AddScoped<
    ICurrentActor,
    HttpContextCurrentActor>();

builder.Services
    .AddOptions<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<ApiAuthenticationOptions>>(
        (jwtBearerOptions, authenticationOptions) =>
        {
            jwtBearerOptions.Authority =
                authenticationOptions.Value.Authority;

            jwtBearerOptions.Audience =
                authenticationOptions.Value.Audience;
        });

builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(
                static context =>
                    ResolvedActorPrincipal.TryGetActorId(
                        context.User,
                        out _))
            .Build());

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

// Development-only: bypass the persisted permission grant table so
// prototyping does not require seeding ActorPermissionGrant rows.
if (developmentActorBypassEnabled)
{
    builder.Services.Replace(
        ServiceDescriptor.Scoped<
            IActorPermissionEvaluator,
            AllowAllPermissionEvaluator>());
}

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory =
            ProjectAtmaca.Api.Errors.InvalidRequestProblem.Create;
    });

builder.Services.AddHealthChecks();

// Learn more about configuring Swagger/OpenAPI at
// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(ProjectAtmaca.Api.Errors.UnexpectedRequestProblem.WriteAsync));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags.Contains(
                    "ready")
    })
    .AllowAnonymous();

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate =
            _ => false
    })
    .AllowAnonymous();

app.MapFallback(
        () =>
            Results.NotFound())
    .AllowAnonymous();

app.Run();

public partial class Program
{
}
