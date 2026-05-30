using IMS.Modular.Modules.WarehouseRouting.Domain.Models;

namespace IMS.Modular.Modules.WarehouseRouting.Application;

/// <summary>
/// Builder Pattern: constructs human-readable navigation instructions from a route path.
/// </summary>
public sealed class RouteInstructionBuilder
{
    /// <summary>
    /// Converts a path of nodes into RouteSteps with navigation instructions.
    /// </summary>
    public static RouteResult Build(List<WarehouseNode> path, string strategyName, TimeSpan computationTime)
    {
        if (path.Count == 0)
            return new RouteResult { Strategy = strategyName, ComputationTime = computationTime };

        var steps = new List<RouteStep>();
        double totalDistance = 0;

        for (int i = 0; i < path.Count; i++)
        {
            var node = path[i];
            double distFromPrev = 0;
            string instruction;

            if (i == 0)
            {
                instruction = $"Início: vá até {node.Name} ({node.Code})";
            }
            else
            {
                var prev = path[i - 1];
                distFromPrev = Math.Abs(node.Row - prev.Row) + Math.Abs(node.Col - prev.Col);
                totalDistance += distFromPrev;
                instruction = GetDirectionInstruction(prev, node);
            }

            steps.Add(new RouteStep
            {
                Order = i + 1,
                LocationId = node.LocationId,
                LocationName = node.Name,
                LocationCode = node.Code,
                Row = node.Row,
                Col = node.Col,
                Instruction = instruction,
                DistanceFromPrevious = distFromPrev
            });
        }

        return new RouteResult
        {
            Steps = steps,
            TotalDistance = totalDistance,
            Strategy = strategyName,
            ComputationTime = computationTime
        };
    }

    private static string GetDirectionInstruction(WarehouseNode from, WarehouseNode to)
    {
        var rowDiff = to.Row - from.Row;
        var colDiff = to.Col - from.Col;

        var direction = (rowDiff, colDiff) switch
        {
            ( < 0, 0) => "Siga em frente (norte)",
            ( > 0, 0) => "Siga em frente (sul)",
            (0, > 0) => "Vire à direita",
            (0, < 0) => "Vire à esquerda",
            ( < 0, > 0) => "Diagonal: norte-direita",
            ( < 0, < 0) => "Diagonal: norte-esquerda",
            ( > 0, > 0) => "Diagonal: sul-direita",
            ( > 0, < 0) => "Diagonal: sul-esquerda",
            _ => "Continue"
        };

        return $"{direction} → Colete em {to.Name} ({to.Code})";
    }
}
