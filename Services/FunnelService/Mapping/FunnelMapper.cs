using states.Dtos.Actions;
using states.Dtos.Edges;
using states.Dtos.Funnels;
using states.Dtos.Nodes;
using states.Mongo.Documents;
using states.Mongo.Documents.Actions;
using states.Mongo.Documents.Edges;
using states.Mongo.Documents.Nodes;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace states.Mongo.Mappers;

public static class FunnelDocumentMapper
{
    #region funnel
    public static FunnelDocument ToDocument(this FunnelCreateDto dto)
    {
        return new FunnelDocument
        {
            Id = Guid.CreateVersion7(),
            TenantId = dto.TenantId,
            SpaceId = dto.SpaceId,
            BotId = dto.BotId,
            Name = dto.Name,
            Description = dto.Description
        };
    }

    public static FunnelDto ToFunnelDto(this FunnelDocument document)
    {
        return new FunnelDto(
            Id: document.Id,
            TenantId: document.TenantId,
            SpaceId: document.SpaceId,
            BotId: document.BotId,
            Name: document.Name,
            Description: document.Description,
            Tags: document.Tags.Select(t => new Tag(t.Id, t.Name)).ToList(),
            Flows: document.Flows.Select(f => new FlowShortDto(f.Id, f.Name)).ToList(),
            IsActive: document.IsActive
        );
    }

    public static Funnel ToDto(this FunnelDocument document)
    {
        return new Funnel(
            Id: document.Id,
            TenantId: document.TenantId,
            SpaceId: document.SpaceId,
            BotId: document.BotId,
            Name: document.Name,
            Description: document.Description,
            Tags: document.Tags.Select(ToDto).ToList(),
            Flows: document.Flows.Select(ToDto).ToList(),
            IsActive: document.IsActive
        );
    }
    #endregion

    #region tag
    private static TagDocument ToDocument(Tag dto)
    {
        return new TagDocument
        {
            Id = dto.Id,
            Name = dto.Name
        };
    }

    private static Tag ToDto(TagDocument document)
    {
        return new Tag(
            Id: document.Id,
            Name: document.Name
        );
    }
    #endregion

    #region flow
    public static FlowDocument ToDocument(this Flow dto)
    {
        return new FlowDocument
        {
            Id = dto.Id,
            Name = dto.Name,
            Nodes = dto.Nodes.Select(ToDocument).ToList(),
            Edges = dto.Edges.Select(ToDocument).ToList()
        };
    }

    public static Flow ToDto(this FlowDocument document)
    {
        return new Flow(
            Id: document.Id,
            Name: document.Name,
            Nodes: document.Nodes.Select(ToDto).ToList(),
            Edges: document.Edges.Select(ToDto).ToList()
        );
    }
    #endregion flow

    #region node
    private static NodeDocument ToDocument(Node dto)
    {
        return new NodeDocument
        {
            Id = dto.Id,
            Type = dto.Type,
            Position = ToDocument(dto.Position),
            Data = ToDocument(dto.Data)
        };
    }

    private static Node ToDto(NodeDocument document)
    {
        return new Node(
            Id: document.Id,
            Type: document.Type,
            Position: ToDto(document.Position),
            Data: ToDto(document.Data)
        );
    }
    #endregion

    #region position
    private static PositionDocument ToDocument(Position dto)
    {
        return new PositionDocument
        {
            X = dto.X,
            Y = dto.Y
        };
    }

    private static Position ToDto(PositionDocument document)
    {
        return new Position(
            X: document.X,
            Y: document.Y
        );
    }
    #endregion

    #region nodedata
    private static NodeDataDocument ToDocument(NodeData dto)
    {
        return dto switch
        {
            StartNodeData x => new StartNodeDataDocument
            {
                Label = x.Label
            },

            SendPresetNodeData x => new SendPresetNodeDataDocument
            {
                Label = x.Label,
                Actions = x.Actions.Select(ToDocument).ToList()
            },

            ManageTagNodeData x => new ManageTagNodeDataDocument
            {
                Label = x.Label,
                Actions = x.Actions.Select(ToDocument).ToList()
            },

            _ => throw new NotSupportedException($"Unsupported node data dto type: {dto.GetType().Name}")
        };
    }

    private static NodeData ToDto(NodeDataDocument document)
    {
        return document switch
        {
            StartNodeDataDocument x => new StartNodeData(
                Label: x.Label,
                FinishStatus: x.FinishStatus
            ),

            SendPresetNodeDataDocument x => new SendPresetNodeData(
                Label: x.Label,
                FinishStatus: x.FinishStatus,
                Actions: x.Actions
                    .OfType<SendPresetActionDocument>()
                    .Select(ToDto)
                    .ToList()
            ),

            ManageTagNodeDataDocument x => new ManageTagNodeData(
                Label: x.Label,
                FinishStatus: x.FinishStatus,
                Actions: x.Actions
                    .OfType<ManageTagActionDocument>()
                    .Select(ToDto)
                    .ToList()
            ),

            _ => throw new NotSupportedException($"Unsupported node data document type: {document.GetType().Name}")
        };
    }
    #endregion

    #region edge
    private static EdgeDocument ToDocument(Edge dto)
    {
        return dto switch
        {
            PassEdge x => new PassEdgeDocument
            {
                Id = x.Id,
                Source = x.Source,
                Target = x.Target
            },

            SplitEdge x => new SplitEdgeDocument
            {
                Id = x.Id,
                Source = x.Source,
                Target = x.Target,
                Percentage = x.Percentage
            },

            AiRouterEdge x => new AiRouterEdgeDocument
            {
                Id = x.Id,
                Source = x.Source,
                Target = x.Target,
                Thesis = x.Thesis
            },

            _ => throw new NotSupportedException($"Unsupported edge dto type: {dto.GetType().Name}")
        };
    }

    private static Edge ToDto(EdgeDocument document)
    {
        return document switch
        {
            PassEdgeDocument x => new PassEdge(
                Id: x.Id,
                Source: x.Source,
                Target: x.Target
            ),

            SplitEdgeDocument x => new SplitEdge(
                Id: x.Id,
                Source: x.Source,
                Target: x.Target,
                Percentage: x.Percentage
            ),

            AiRouterEdgeDocument x => new AiRouterEdge(
                Id: x.Id,
                Source: x.Source,
                Target: x.Target,
                Thesis: x.Thesis
            ),

            _ => throw new NotSupportedException($"Unsupported edge document type: {document.GetType().Name}")
        };
    }
    #endregion

    #region nodeaction
    private static SendPresetActionDocument ToDocument(SendPresetAction dto)
    {
        return new SendPresetActionDocument
        {
            Id = dto.Id,
            PresetId = dto.PresetId,
            Delay = dto.Delay,
            NeedPin = dto.NeedPin
        };
    }

    private static SendPresetAction ToDto(SendPresetActionDocument document)
    {
        return new SendPresetAction(
            Id: document.Id,
            PresetId: document.PresetId,
            Delay: document.Delay,
            NeedPin: document.NeedPin
        );
    }

    private static ManageTagActionDocument ToDocument(ManageTagAction dto)
    {
        return new ManageTagActionDocument
        {
            Id = dto.Id,            
            Operation = dto.Operation,
            TagId = dto.TagId,
            ReplacementTagId = dto.ReplacementTagId            
        };
    }
    public static ManageTagAction ToDto(ManageTagActionDocument document) {
        return new ManageTagAction(
            Id: document.Id,
            Operation: document.Operation,
            TagId: document.TagId,
            ReplacementTagId: document.ReplacementTagId,
            Delay: null
        );
    }
    #endregion
}