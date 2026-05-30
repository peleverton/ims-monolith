using FluentAssertions;
using IMS.Modular.Modules.WarehouseRouting.Application;
using IMS.Modular.Modules.WarehouseRouting.Application.Strategies;
using IMS.Modular.Modules.WarehouseRouting.Domain.Models;

namespace IMS.Modular.Tests.Modules.WarehouseRouting;

public class WarehouseRoutingTests
{
    /// <summary>
    /// Creates a 4x4 grid warehouse for testing:
    ///   [0,0] [0,1] [0,2] [0,3]
    ///   [1,0] [1,1] [1,2] [1,3]
    ///   [2,0] [2,1] [2,2] [2,3]
    ///   [3,0] [3,1] [3,2] [3,3]
    /// </summary>
    private static (WarehouseGraph Graph, List<WarehouseNodeInput> Inputs) CreateTestGrid()
    {
        var inputs = new List<WarehouseNodeInput>();
        for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
        {
            inputs.Add(new WarehouseNodeInput
            {
                LocationId = Guid.NewGuid(),
                Name = $"Loc-{r}{c}",
                Code = $"L{r}{c}",
                Row = r,
                Col = c,
                IsWalkable = true
            });
        }

        var graph = WarehouseGraph.BuildFromGrid(inputs, 4, 4);
        return (graph, inputs);
    }

    /// <summary>
    /// Creates a grid with an obstacle (non-walkable cell at [1,1]).
    /// </summary>
    private static (WarehouseGraph Graph, List<WarehouseNodeInput> Inputs) CreateGridWithObstacle()
    {
        var inputs = new List<WarehouseNodeInput>();
        for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
        {
            inputs.Add(new WarehouseNodeInput
            {
                LocationId = Guid.NewGuid(),
                Name = $"Loc-{r}{c}",
                Code = $"L{r}{c}",
                Row = r,
                Col = c,
                IsWalkable = !(r == 1 && c == 1) // obstacle at center
            });
        }

        var graph = WarehouseGraph.BuildFromGrid(inputs, 3, 3);
        return (graph, inputs);
    }

    // ─── Graph Construction ───────────────────────────────────────────────────

    [Fact]
    public void BuildFromGrid_CreatesCorrectNodeCount()
    {
        var (graph, _) = CreateTestGrid();

        graph.Nodes.Should().HaveCount(16);
        graph.Rows.Should().Be(4);
        graph.Cols.Should().Be(4);
    }

    [Fact]
    public void BuildFromGrid_CornerNodesHave2Edges()
    {
        var (graph, inputs) = CreateTestGrid();

        var corner = graph.Nodes[inputs[0].LocationId]; // [0,0]
        corner.Edges.Should().HaveCount(2); // right and down
    }

    [Fact]
    public void BuildFromGrid_CenterNodesHave4Edges()
    {
        var (graph, inputs) = CreateTestGrid();

        // [1,1] is at index 5 (row 1, col 1)
        var center = graph.Nodes[inputs[5].LocationId];
        center.Edges.Should().HaveCount(4);
    }

    [Fact]
    public void BuildFromGrid_ObstacleNodeHasNoEdges()
    {
        var (graph, inputs) = CreateGridWithObstacle();

        // [1,1] is the obstacle — index 4
        var obstacle = graph.Nodes[inputs[4].LocationId];
        obstacle.Edges.Should().HaveCount(0);
    }

    [Fact]
    public void BuildFromGrid_EntryPointIsTopLeft()
    {
        var (graph, inputs) = CreateTestGrid();

        graph.EntryPoint.Should().NotBeNull();
        graph.EntryPoint!.Row.Should().Be(0);
        graph.EntryPoint.Col.Should().Be(0);
    }

    // ─── Dijkstra Strategy ────────────────────────────────────────────────────

    [Fact]
    public void Dijkstra_SingleTarget_FindsShortestPath()
    {
        var (graph, inputs) = CreateTestGrid();
        var strategy = new DijkstraRoutingStrategy();

        var start = graph.Nodes[inputs[0].LocationId];  // [0,0]
        var target = graph.Nodes[inputs[3].LocationId]; // [0,3]

        var path = strategy.CalculateRoute(graph, start, [target]);

        path.Should().HaveCount(4); // [0,0] → [0,1] → [0,2] → [0,3]
        path.First().Should().Be(start);
        path.Last().Should().Be(target);
    }

    [Fact]
    public void Dijkstra_MultipleTargets_VisitsAll()
    {
        var (graph, inputs) = CreateTestGrid();
        var strategy = new DijkstraRoutingStrategy();

        var start = graph.Nodes[inputs[0].LocationId];   // [0,0]
        var target1 = graph.Nodes[inputs[3].LocationId]; // [0,3]
        var target2 = graph.Nodes[inputs[12].LocationId]; // [3,0]

        var path = strategy.CalculateRoute(graph, start, [target1, target2]);

        path.Should().Contain(target1);
        path.Should().Contain(target2);
        path.First().Should().Be(start);
    }

    [Fact]
    public void Dijkstra_WithObstacle_FindsAlternativePath()
    {
        var (graph, inputs) = CreateGridWithObstacle();
        var strategy = new DijkstraRoutingStrategy();

        var start = graph.Nodes[inputs[0].LocationId]; // [0,0]
        var target = graph.Nodes[inputs[8].LocationId]; // [2,2]

        var path = strategy.CalculateRoute(graph, start, [target]);

        path.Should().NotBeEmpty();
        path.First().Should().Be(start);
        path.Last().Should().Be(target);
        // Should NOT pass through [1,1] (obstacle)
        path.Should().NotContain(graph.Nodes[inputs[4].LocationId]);
    }

    // ─── A* Strategy ──────────────────────────────────────────────────────────

    [Fact]
    public void AStar_SingleTarget_FindsShortestPath()
    {
        var (graph, inputs) = CreateTestGrid();
        var strategy = new AStarRoutingStrategy();

        var start = graph.Nodes[inputs[0].LocationId];  // [0,0]
        var target = graph.Nodes[inputs[15].LocationId]; // [3,3]

        var path = strategy.CalculateRoute(graph, start, [target]);

        path.Should().HaveCountGreaterThanOrEqualTo(7); // Manhattan distance = 6 edges = 7 nodes
        path.First().Should().Be(start);
        path.Last().Should().Be(target);
    }

    [Fact]
    public void AStar_MultipleTargets_VisitsAll()
    {
        var (graph, inputs) = CreateTestGrid();
        var strategy = new AStarRoutingStrategy();

        var start = graph.Nodes[inputs[0].LocationId];
        var targets = new List<WarehouseNode>
        {
            graph.Nodes[inputs[3].LocationId],  // [0,3]
            graph.Nodes[inputs[12].LocationId], // [3,0]
            graph.Nodes[inputs[15].LocationId]  // [3,3]
        };

        var path = strategy.CalculateRoute(graph, start, targets);

        foreach (var t in targets)
            path.Should().Contain(t);
    }

    [Fact]
    public void AStar_WithObstacle_AvoidsBlockedCell()
    {
        var (graph, inputs) = CreateGridWithObstacle();
        var strategy = new AStarRoutingStrategy();

        var start = graph.Nodes[inputs[0].LocationId]; // [0,0]
        var target = graph.Nodes[inputs[8].LocationId]; // [2,2]

        var path = strategy.CalculateRoute(graph, start, [target]);

        path.Should().NotContain(graph.Nodes[inputs[4].LocationId]);
        path.Last().Should().Be(target);
    }

    // ─── Route Instruction Builder ────────────────────────────────────────────

    [Fact]
    public void RouteInstructionBuilder_GeneratesCorrectSteps()
    {
        var (graph, inputs) = CreateTestGrid();
        var path = new List<WarehouseNode>
        {
            graph.Nodes[inputs[0].LocationId],  // [0,0]
            graph.Nodes[inputs[1].LocationId],  // [0,1]
            graph.Nodes[inputs[2].LocationId],  // [0,2]
        };

        var result = RouteInstructionBuilder.Build(path, "Dijkstra", TimeSpan.FromMilliseconds(5));

        result.Steps.Should().HaveCount(3);
        result.TotalDistance.Should().Be(2);
        result.Strategy.Should().Be("Dijkstra");
        result.Steps[0].Instruction.Should().Contain("Início");
        result.Steps[1].Instruction.Should().Contain("direita");
    }

    [Fact]
    public void RouteInstructionBuilder_EmptyPath_ReturnsEmptyResult()
    {
        var result = RouteInstructionBuilder.Build([], "Dijkstra", TimeSpan.Zero);

        result.Steps.Should().BeEmpty();
        result.TotalDistance.Should().Be(0);
    }

    // ─── Strategy Selection ───────────────────────────────────────────────────

    [Fact]
    public void DijkstraAndAStar_ProduceSameVisitSet()
    {
        var (graph, inputs) = CreateTestGrid();
        var dijkstra = new DijkstraRoutingStrategy();
        var astar = new AStarRoutingStrategy();

        var start = graph.Nodes[inputs[0].LocationId];
        var targets = new List<WarehouseNode>
        {
            graph.Nodes[inputs[3].LocationId],
            graph.Nodes[inputs[12].LocationId]
        };

        var pathD = dijkstra.CalculateRoute(graph, start, targets);
        var pathA = astar.CalculateRoute(graph, start, targets);

        // Both must visit all targets
        pathD.Should().Contain(targets[0]).And.Contain(targets[1]);
        pathA.Should().Contain(targets[0]).And.Contain(targets[1]);
    }
}
