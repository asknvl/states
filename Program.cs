using MongoDB.Driver;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using states.Dtos.Funnels.Examples;
using states.Mongo;
using states.Mongo.Repositories;
using states.Services.FunnelService;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Swagger;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;

namespace states
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseUrls("http://0.0.0.0:5002");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAny", policy =>
                {
                    policy.AllowAnyOrigin()   // ��������� ����� ��������
                          .AllowAnyMethod()   // ��������� ����� HTTP-������
                          .AllowAnyHeader();  // ��������� ����� ���������
                });
            });

            var otlpLogs = new Uri(builder.Configuration["OpenTelemetry:Otlp:LogsEndpoint"]);

            builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

            builder.Logging.AddOpenTelemetry(o =>
            {
                o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Configuration["ServiceInstanceId"]));

                o.IncludeFormattedMessage = true;
                o.ParseStateValues = true;
                o.IncludeScopes = true;

                o.AddConsoleExporter();
                o.AddOtlpExporter(exp =>
                {
                    exp.Endpoint = otlpLogs;
                    exp.Protocol = OtlpExportProtocol.HttpProtobuf;
                });
            });

            builder.Services.AddSwaggerExamplesFromAssemblyOf<FunnelExample>();

            builder.Services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                options.ExampleFilters();
                options.OperationFilter<RemoveErrorBodiesOperationFilter>();
            });

            builder.Services.AddSingleton<IMongoClient>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();

                var connectionString = config["Mongo:ConnectionString"];
                var cs = config["Mongo:ConnectionString"]
                    ?? throw new InvalidOperationException("Mongo:ConnectionString not configured");

                return new MongoClient(cs);
            });

            builder.Services.AddSingleton<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                var config = sp.GetRequiredService<IConfiguration>();
                return client.GetDatabase(config["Mongo:Database"]);
            });

            builder.Services.AddSingleton<MongoContext>();
            builder.Services.AddSingleton<MongoBootstrapper>();
            builder.Services.AddSingleton<IFunnelsRepository, FunnelsRepository>();


            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<FunnelCache>();
            builder.Services.AddSingleton<IFunnelRuntimeCache>(sp => sp.GetRequiredService<FunnelCache>());

            builder.Services.AddSingleton<FunnelRuntimeService>();
            builder.Services.AddSingleton<IFunnelRuntimeSupervisor>(sp => sp.GetRequiredService<FunnelRuntimeService>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<FunnelRuntimeService>());

            builder.Services.AddScoped<IFunnelsApplicationService, FunnelApplicationService>();

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseCors("AllowAny");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.MapControllers();

            app.Run();
        }
    }
}
