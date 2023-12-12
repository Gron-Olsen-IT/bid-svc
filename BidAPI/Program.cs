using BidAPI.Services;
using NLog;
using NLog.Web;
using sidecar_lib;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;
using System.Text;


var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    AuthSidecar sidecar = new(logger);

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSingleton<IVaultClient>(sidecar.vaultClient);
    builder.Services.AddScoped<IBidService, BidService>();
    builder.Services.AddScoped<IBidRepo, BidRepoMongo>();
    builder.Services.AddScoped<IRabbitController, RabbitController>();
    builder.Services.AddScoped<IInfraRepo, InfraRepo>();

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = sidecar.GetTokenValidationParameters();
    });

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.ConfigureSwagger("BidAPI");

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("./v1/swagger.json", "Your Microservice API V1");
    });


    app.UseHttpsRedirection();

    app.UseAuthorization();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception e)
{
    logger.Error(e, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}
