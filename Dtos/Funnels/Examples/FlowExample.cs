using states.Dtos.Actions;
using states.Dtos.Edges;
using states.Dtos.Nodes;
using states.Services.FunnelService.Application;
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
                        Id: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new StartNodeData(
                            Label: "Старт"
                        )
                    ),
                    new Node(
                        Id: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new ManageTagNodeData(
                            Label: "Тег1",
                            Operation: TagOperation.Add,
                            TagId: tags[0].Id,
                            ReplacementTagId: null
                        )
                    ),
                    new Node(
                        Id: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new SendPresetNodeData(
                            Label: "Сообщение 1",
                            Actions: new List<SendPresetAction>()
                            {
                                new SendPresetAction(
                                    Id: new Guid("0195930e-84a6-79f2-b2d4-7e0a1c8f5b36"),
                                    Delay: TimeSpan.FromSeconds(2),
                                    PresetId: new Guid("6950dd26-6ea6-4616-869b-8ae0466301cc"),
                                    NeedPin: false
                                )
                            }
                        )
                    ),
                    new Node(
                        Id: new Guid("0195930e-84a7-74ab-9d6f-3b1c7e2a8f49"),
                        Type: "custom",
                        Position: new Position( 0, 0 ),
                        Data: new SendPresetNodeData(
                            Label: "Сообщение 2",
                            Actions: new List<SendPresetAction>()
                            {
                                new SendPresetAction(
                                    Id: new Guid("0195930e-84a8-7e5c-a3f1-4d9b6c0e2a78"),
                                    Delay: TimeSpan.FromSeconds(2),
                                    PresetId: new Guid("6950dd26-6ea6-4616-869b-8ae0466301cc"),
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
                        Source: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"),
                        Target: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10")
                    ),
                    new SplitEdge(
                        Id: new Guid("0195930e-84aa-7c63-8b1f-2d7a4e9c5f20"),
                        Source: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"),
                        Target: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"),
                        Percentage: 10
                    ),
                    new SplitEdge(
                        Id: new Guid("0195930e-9340-7a91-8c4d-2f6b1e7a5032"),
                        Source: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"),
                        Target: new Guid("0195930e-84a7-74ab-9d6f-3b1c7e2a8f49"),
                        Percentage: 10
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
