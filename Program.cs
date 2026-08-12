using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using states.Dtos.Funnels.Examples;
using states.Dtos.Edges;
using states.Dtos.LeadEvents;
using states.Dtos.Nodes;
using states.Mongo;
using states.Mongo.Repositories;
using states.Services.Folders.Application;
using states.Services.FunnelService;
using states.Services.LeadEventsService.Application;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Services.LeadService;
using states.Services.LeadService.Routing;
using states.Services.CampaignService;
using states.Services.MigratorService;
using states.Services.TgEngineService;
using Confluent.Kafka;
using states.Services.Events.Producer;
using states.Services.LeadService.Worker;
using states.Swagger;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using MongoDB.Bson.Serialization;
using states.Services.AIServiceClient;
using states.Services.Events.Consumers.LeadPostbackEvents;
using states.Services.Events.Consumers.GlobalEvents;

namespace states
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseUrls("http://0.0.0.0:5002");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAny", policy =>
                {
                    policy.AllowAnyOrigin()   
                          .AllowAnyMethod()   
                          .AllowAnyHeader();  
                });
            });

            var otlpLogs = new Uri(builder.Configuration["OpenTelemetry:Otlp:LogsEndpoint"]);

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
                options.SchemaFilter<DiscriminatorEnumSchemaFilter>();

                options.UseOneOfForPolymorphism();
                options.UseAllOfForInheritance();

                options.SelectSubTypesUsing(baseType =>
                {
                    if (baseType == typeof(NodeData))
                        return [typeof(StartNodeData), typeof(SendPresetNodeData), typeof(ManageTagNodeData), typeof(AiReplyNodeData), typeof(ChangeFlowNodeData), typeof(SendWebhookNodeData)];
                    if (baseType == typeof(Edge))
                        return [typeof(PassEdge), typeof(SplitEdge), typeof(AiRouterEdge)];
                    if (baseType == typeof(LeadEventBaseDto))
                        return [typeof(BotActivationEventDto), typeof(BotDeactivationEventDto), typeof(ChannelSubscriptionEventDto), typeof(ContactEventDto), typeof(RegistrationEventDto), typeof(SaleEventDto), typeof(ResaleEventDto)];
                    return [];
                });

                options.SelectDiscriminatorNameUsing(baseType =>
                {
                    if (baseType == typeof(NodeData)) return "nodeType";
                    if (baseType == typeof(Edge)) return "edgeType";
                    if (baseType == typeof(LeadEventBaseDto)) return "eventType";
                    return null;
                });

                options.SelectDiscriminatorValueUsing(subType =>
                {
                    if (subType == typeof(StartNodeData)) return nameof(NodeType.Start);
                    if (subType == typeof(SendPresetNodeData)) return nameof(NodeType.SendPreset);
                    if (subType == typeof(ManageTagNodeData)) return nameof(NodeType.ManageTag);
                    if (subType == typeof(AiReplyNodeData)) return nameof(NodeType.AiReply);
                    if (subType == typeof(ChangeFlowNodeData)) return nameof(NodeType.ChangeFlow);
                    if (subType == typeof(SendWebhookNodeData)) return nameof(NodeType.SendWebhook);
                    if (subType == typeof(PassEdge)) return nameof(EdgeType.Pass);
                    if (subType == typeof(SplitEdge)) return nameof(EdgeType.Split);
                    if (subType == typeof(AiRouterEdge)) return nameof(EdgeType.AiRouter);
                    if (subType == typeof(BotActivationEventDto)) return "BotActivation";
                    if (subType == typeof(BotDeactivationEventDto)) return "BotDeactivation";
                    if (subType == typeof(ChannelSubscriptionEventDto)) return "ChannelSubscribtion";
                    if (subType == typeof(ContactEventDto)) return "Contact";
                    if (subType == typeof(RegistrationEventDto)) return "Registration";
                    if (subType == typeof(SaleEventDto)) return "Sale";
                    if (subType == typeof(ResaleEventDto)) return "Resale";
                    return null;
                });
            });

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

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
            builder.Services.AddSingleton<ITenantTagsRepository, TenantTagsRepository>();
            builder.Services.AddSingleton<IFoldersRepository, FoldersRepository>();
            builder.Services.AddSingleton<ILeadEventsRepository, LeadEventsRepository>();


            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<FunnelCache>();
            builder.Services.AddSingleton<IFunnelRuntimeCache>(sp => sp.GetRequiredService<FunnelCache>());

            builder.Services.AddSingleton<FunnelRuntimeService>();
            builder.Services.AddSingleton<IFunnelRuntimeSupervisor>(sp => sp.GetRequiredService<FunnelRuntimeService>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<FunnelRuntimeService>());

            builder.Services.AddScoped<IFunnelsApplicationService, FunnelApplicationService>();
            builder.Services.AddScoped<IFoldersApplicationService, FoldersApplicationService>();
            builder.Services.AddScoped<ILeadEventsApplicationService, LeadEventsApplicationService>();

            // Lead service
            builder.Services.AddSingleton<ILeadStateRepository, LeadStateRepository>();
            builder.Services.AddSingleton<IActionTaskRepository, ActionTaskRepository>();
            builder.Services.AddSingleton<IPushTaskRepository, PushTaskRepository>();
            builder.Services.AddSingleton<IEdgeRouter, EdgeRouter>();
            builder.Services.AddSingleton<IActionExecutor, ActionExecutor>();
            builder.Services.AddSingleton<IPushExecutor, PushExecutor>();
            builder.Services.AddSingleton<ILeadProgressionService, LeadProgressionService>();
            builder.Services.AddHostedService<ActionWorkerService>();
            builder.Services.AddHostedService<PushWorkerService>();

            // Campaign service
            builder.Services.AddHttpClient<ICampaignClient, CampaignClient>(client =>
            {
                var baseUrl = builder.Configuration["CampaignClient:EndPoint"]
                    ?? throw new InvalidOperationException("CampaignClient:EndPoint not configured");
                client.BaseAddress = new Uri(baseUrl);
            });

            // Migrator service
            builder.Services.AddHttpClient<IMigratorClient, MigratorClient>(client =>
            {
                var baseUrl = builder.Configuration["MigratorClient:EndPoint"]
                    ?? throw new InvalidOperationException("MigratorClient:EndPoint not configured");
                client.BaseAddress = new Uri(baseUrl);
            });

            // TgEngine service
            builder.Services.AddHttpClient<ITGEngineClient, TGEngineClient>(client =>
            {
                var baseUrl = builder.Configuration["TgEngineClient:EndPoint"]
                    ?? throw new InvalidOperationException("TgEngineClient:EndPoint not configured");
                client.BaseAddress = new Uri(baseUrl);
            });

            // AI service
            builder.Services.AddHttpClient<IAIServiceClient, AIServiceClient>(client =>
            {
                var baseUrl = builder.Configuration["AIServiceClient:EndPoint"]
                    ?? throw new InvalidOperationException("AIServiceClient:EndPoint not configured");
                client.BaseAddress = new Uri(baseUrl);
            });

            // Kafka producer
            builder.Services.AddSingleton<IProducer<string, string>>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = config["Kafka:BootstrapServers"]
                        ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured"),
                    ClientId = config["Kafka:ClientId"],
                    BrokerAddressFamily = BrokerAddressFamily.V4,

                    // Статусы и депозиты лидов: ретрай при потерянном ack не должен
                    // порождать дубли в топике
                    Acks = Acks.All,
                    EnableIdempotence = true,

                    // Недоставленное переотправит outbox-воркер; таймауты нужны, чтобы после
                    // простоя ProduceAsync не висел минуту на мёртвом сокете (дефолт — 60 с)
                    MessageTimeoutMs = 60000,
                    SocketTimeoutMs = 10000,
                    SocketKeepaliveEnable = true,
                    ConnectionsMaxIdleMs = 180000,
                    SocketNagleDisable = true,

                    // Outbox-воркер ждёт каждый ProduceAsync по одному: дефолтный linger 5 мс
                    // добавлялся бы к каждому событию и ограничил бы поток ~200 соб/с
                    LingerMs = 0,
                    CompressionType = CompressionType.Lz4
                };
                return new ProducerBuilder<string, string>(producerConfig).Build();
            });
            builder.Services.AddSingleton<IEventService, KafkaEventService>();
            builder.Services.AddSingleton<IOutboxRepository, OutboxRepository>();
            builder.Services.AddHostedService<OutboxWorkerService>();

            // Telegram Kafka consumer
            builder.Services.AddSingleton<IGlobalEventProcessor, GlobalEventProcessor>();
            builder.Services.AddHostedService<GlobalEventConsumerService>();

            // Postback events Kafka consumer
            builder.Services.AddSingleton<IPostbackEventProcessor, PostbackEventProcessor>();
            builder.Services.AddHostedService<PostbackEventConsumerService>();

            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                var bootStrapper = scope.ServiceProvider
                    .GetRequiredService<MongoBootstrapper>();

                await bootStrapper.Initialize();
            }

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
