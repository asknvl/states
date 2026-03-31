using states.Dtos.Actions;
using states.Dtos.Edges;
using states.Dtos.Nodes;
using states.Services.FunnelService.Application;
using states.Services.LeadService;
using Swashbuckle.AspNetCore.Filters;

namespace states.Dtos.Funnels.Examples
{
    public class FlowExample : IMultipleExamplesProvider<object>
    {
        public IEnumerable<SwaggerExample<object>> GetExamples()
        {
            List<Tag> tags = new List<Tag>()
            {
                new Tag(
                    Id: new Guid("0195930e-84a1-7b6e-9c2d-13f7a4b8c901"),
                    Name: "Дурачок"
                ),
                new Tag(
                    Id: new Guid("0195930e-84a2-76d4-a1e9-2b6c9d0f3a57"),
                    Name: "FD"
                ),
                new Tag(
                    Id: new Guid("0195930e-84a3-7f81-b4c2-8e1a7d5f9b23"),
                    Name: "RD"
                )
            };

            var flow = new Flow(
                Id: new Guid("0195930e-84a4-72c9-8f3b-6a1d4e7c0b95"),
                Name: "Флоу 1",
                Nodes: new List<Node>()
                {
                    new Node(
                        Id: new Guid("0195e9f7-6c00-7a31-8b54-1f2c9d7e4a01"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new StartNodeData(
                            Label: "Старт",
                            FinishStatus: LeadFunnelStatus.Nothing
                        )
                    ),
                    new Node(
                        Id: new Guid("0195e9f7-6c01-7f82-9c13-2a7b4d8e5f02"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new ManageTagNodeData(
                            Label: "Тег1",
                            FinishStatus: LeadFunnelStatus.Nothing,
                            Actions: new List<ManageTagAction>()
                            {
                                new ManageTagAction(
                                    Id: new Guid("019d2068-5501-725e-9699-067c51b56937"),
                                    Delay: null,
                                    Operation: TagOperation.Add,
                                    TagId: tags[0].Id,
                                    ReplacementTagId: null
                                )
                            }
                        )
                    ),
                    new Node(
                        Id: new Guid("0195e9f7-6c02-71d4-a6b9-3c8e5f1a7d03"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new SendPresetNodeData(
                            Label: "Сообщение 1",
                            FinishStatus: LeadFunnelStatus.Nothing,
                            Actions: new List<SendPresetAction>()
                            {
                                new SendPresetAction(
                                    Id: new Guid("0195930e-84a6-79f2-b2d4-7e0a1c8f5b36"),
                                    Delay: TimeSpan.FromSeconds(2),
                                    PresetId: new Guid("4e53c6d3-199e-403b-bdbe-3b568d745647"),
                                    NeedPin: false
                                )
                            }
                        )
                    ),
                    new Node(
                        Id: new Guid("0195e9f7-6c03-7b95-8f21-4d9a6c2b8e04"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new SendPresetNodeData(
                            Label: "Сообщение 2",
                            FinishStatus: LeadFunnelStatus.Manual,
                            Actions: new List<SendPresetAction>()
                            {
                                new SendPresetAction(
                                    Id: new Guid("0195930e-84a8-7e5c-a3f1-4d9b6c0e2a78"),
                                    Delay: TimeSpan.FromSeconds(2),
                                    PresetId: new Guid("38d33986-f59e-40fd-aa83-9d220d5b62b3"),
                                    NeedPin: false
                                )
                            }
                        )
                    ),
                },
                Edges: new List<Edge>()
                {
                    new PassEdge(
                        Id: new Guid("0195930e-84a9-71d8-b7a4-9c2e5f1d3b64"),
                        Source: new Guid("0195e9f7-6c00-7a31-8b54-1f2c9d7e4a01"),
                        Target: new Guid("0195e9f7-6c01-7f82-9c13-2a7b4d8e5f02")
                    ),
                    new SplitEdge(
                        Id: new Guid("0195930e-84aa-7c63-8b1f-2d7a4e9c5f20"),
                        Source: new Guid("0195e9f7-6c01-7f82-9c13-2a7b4d8e5f02"),
                        Target: new Guid("0195e9f7-6c02-71d4-a6b9-3c8e5f1a7d03"),
                        Percentage: 50
                    ),
                    new SplitEdge(
                        Id: new Guid("0195930e-9340-7a91-8c4d-2f6b1e7a5032"),
                        Source: new Guid("0195e9f7-6c01-7f82-9c13-2a7b4d8e5f02"),
                        Target: new Guid("0195e9f7-6c03-7b95-8f21-4d9a6c2b8e04"),
                        Percentage: 50
                    )
                }
            );

            yield return new SwaggerExample<object>
            {
                Name = "Flow",
                Summary = "Add flow to funnel",
                Value = flow
            };
        }
    }
}
