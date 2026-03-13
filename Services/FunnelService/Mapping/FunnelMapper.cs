using DTO = states.Dtos;
using DOC = states.Mongo.Documents;

namespace states.Mongo.Mappers;

public static class FunnelDocumentMapper
{
    public static DOC.Funnel ToDocument(this DTO.Funnels.Funnel dto)
    {
        return new DOC.Funnel
        {
            Id = dto.Id,
            TenantId = dto.TenantId,
            SpaceId = dto.SpaceId,
            BotId = dto.BotId,
            Name = dto.Name,
            Description = dto.Description,
            Tags = dto.Tags.Select(ToDocument).ToList(),
            Flows = dto.Flows.Select(ToDocument).ToList()
        };
    }

    public static DTO.Funnels.Funnel ToDto(this DOC.Funnel document)
    {
        return new DTO.Funnels.Funnel(
            Id: document.Id,
            TenantId: document.TenantId,
            SpaceId: document.SpaceId,
            BotId: document.BotId,
            Name: document.Name,
            Description: document.Description,
            Tags: document.Tags.Select(ToDto).ToList(),
            Flows: document.Flows.Select(ToDto).ToList()
        );
    }

    private static DOC.Tag ToDocument(DTO.Funnels.Tag dto)
    {
        return new DOC.Tag
        {
            Id = dto.Id,
            Name = dto.Name
        };
    }

    private static DTO.Funnels.Tag ToDto(DOC.Tag document)
    {
        return new DTO.Funnels.Tag(
            Id: document.Id,
            Name: document.Name
        );
    }

    private static DOC.Flow ToDocument(DTO.Funnels.Flow dto)
    {
        return new DOC.Flow
        {
            Id = dto.Id,
            Name = dto.Name,
            Nodes = dto.Nodes.Select(ToDocument).ToList(),
            Edges = dto.Edges.Select(ToDocument).ToList()
        };
    }

    private static DTO.Funnels.Flow ToDto(DOC.Flow document)
    {
        return new DTO.Funnels.Flow(
            Id: document.Id,
            Name: document.Name,
            Nodes: document.Nodes.Select(ToDto).ToList(),
            Edges: document.Edges.Select(ToDto).ToList()
        );
    }

    private static DOC.Nodes.Node ToDocument(DTO.Nodes.Node dto)
    {
        return new DOC.Nodes.Node
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
                Operation = x.Operation,
                TagId = x.TagId,
                ReplacementTagId = x.ReplacementTagId
            },

            _ => throw new NotSupportedException($"Unsupported node data type: {dto.GetType().Name}")
        };
    }

    private static NodeData ToDto(NodeDataDocument document)
    {
        return document switch
        {
            StartNodeDataDocument x => new StartNodeData(
                Label: x.Label
            ),

            SendPresetNodeDataDocument x => new SendPresetNodeData(
                Label: x.Label,
                Actions: x.Actions
                    .OfType<SendPresetActionDocument>()
                    .Select(ToDto)
                    .ToList()
            ),

            ManageTagNodeDataDocument x => new ManageTagNodeData(
                Label: x.Label,
                Operation: x.Operation,
                TagId: x.TagId,
                ReplacementTagId: x.ReplacementTagId
            ),

            _ => throw new NotSupportedException($"Unsupported node data document type: {document.GetType().Name}")
        };
    }

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

            _ => throw new NotSupportedException($"Unsupported edge type: {dto.GetType().Name}")
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

            _ => throw new NotSupportedException($"Unsupported edge document type: {document.GetType().Name}")
        };
    }

    private static NodeActionDocument ToDocument(SendPresetAction dto)
    {
        return new SendPresetActionDocument
        {
            Id = dto.Id,
            Type = dto.Type,
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
}