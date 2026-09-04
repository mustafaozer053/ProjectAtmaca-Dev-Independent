using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(
    options =>
    {
        options.IncludeScopes =
            true;
    });

// Add services to the container.

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.Authority =
                builder.Configuration[
                    "Authentication:Authority"];

            options.Audience =
                builder.Configuration[
                    "Authentication:Audience"];

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

builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddHealthChecks();

// Learn more about configuring Swagger/OpenAPI at
// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
