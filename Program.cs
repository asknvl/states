using MongoDB.Driver;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using states.Dtos.Funnels.Examples;
using states.Dtos.Edges;
using states.Dtos.Nodes;
using states.Mongo;
using states.Mongo.Repositories;
using states.Services.FunnelService;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Services.LeadService;
using states.Services.LeadService.Routing;
using states.Services.CampaignService;
using states.Services.Events.Consumer;
using states.Services.LeadService.Worker;
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

            builder.Services.AddSwaggerExamplesFromAssemblyOf<FlowExample>();

            builder.Services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.UseInlineDefinitionsForEnums();
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                options.ExampleFilters();
                options.OperationFilter<RemoveErrorBodiesOperationFilter>();

                options.UseOneOfForPolymorphism();
                options.UseAllOfForInheritance();

                options.SelectSubTypesUsing(baseType =>
                {
                    if (baseType == typeof(NodeData))
                        return [typeof(StartNodeData), typeof(SendPresetNodeData), typeof(ManageTagNodeData)];
                    if (baseType == typeof(Edge))
                        return [typeof(PassEdge), typeof(SplitEdge), typeof(AiRouterEdge)];
                    return [];
                });

                options.SelectDiscriminatorNameUsing(baseType =>
                {
                    if (baseType == typeof(NodeData)) return "nodeType";
                    if (baseType == typeof(Edge)) return "edgeType";
                    return null;
                });

                options.SelectDiscriminatorValueUsing(subType =>
                {
                    if (subType == typeof(StartNodeData)) return nameof(NodeType.Start);
                    if (subType == typeof(SendPresetNodeData)) return nameof(NodeType.SendPreset);
                    if (subType == typeof(ManageTagNodeData)) return nameof(NodeType.ManageTag);
                    if (subType == typeof(PassEdge)) return nameof(EdgeType.Pass);
                    if (subType == typeof(SplitEdge)) return nameof(EdgeType.Split);
                    if (subType == typeof(AiRouterEdge)) return nameof(EdgeType.AiRouter);
                    return null;
                });
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

            // Lead service
            builder.Services.AddSingleton<ILeadStateRepository, LeadStateRepository>();
            builder.Services.AddSingleton<IActionTaskRepository, ActionTaskRepository>();
            builder.Services.AddSingleton<IAiRouterClient, AiRouterClientStub>();
            builder.Services.AddSingleton<IEdgeRouter, EdgeRouter>();
            builder.Services.AddSingleton<IActionExecutor, ActionExecutor>();
            builder.Services.AddSingleton<ILeadProgressionService, LeadProgressionService>();
            builder.Services.AddHostedService<ActionWorkerService>();

            // Campaign service
            builder.Services.AddSingleton<ICampaignClient, CampaignClientStub>();

            // Telegram Kafka consumer
            builder.Services.AddSingleton<IGlobalEventProcessor, GlobalEventProcessor>();
            builder.Services.AddHostedService<GlobalEventConsumerService>();

            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

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
