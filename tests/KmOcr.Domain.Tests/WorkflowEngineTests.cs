using FluentAssertions;
using KmOcr.Domain.WorkflowEngine;
using Xunit;

namespace KmOcr.Domain.Tests;

/// <summary>
/// Verifies node-based workflow graph behavior.
/// </summary>
public sealed class WorkflowEngineTests
{
    /// <summary>
    /// Ensures a definition can publish a valid start-to-end graph.
    /// </summary>
    [Fact]
    public void Publish_should_create_immutable_version_for_valid_graph()
    {
        var definition = WorkflowDefinition.Create("invoice-review", "Invoice Review", "admin@km.local");
        var start = definition.AddNode("start", WorkflowNodeType.Start, 10, 20, "{}");
        var ocr = definition.AddNode("ocr", WorkflowNodeType.Ocr, 140, 20, "{\"language\":\"tha+eng\"}");
        var end = definition.AddNode("end", WorkflowNodeType.End, 280, 20, "{}");
        definition.AddEdge(start.NodeKey, ocr.NodeKey, null);
        definition.AddEdge(ocr.NodeKey, end.NodeKey, null);

        var version = definition.Publish("admin@km.local");

        definition.IsPublished.Should().BeTrue();
        definition.PublishedVersionNumber.Should().Be(1);
        version.VersionNumber.Should().Be(1);
        version.GraphJson.Should().Contain("\"ocr\"");
    }

    /// <summary>
    /// Ensures publishing requires exactly one start node.
    /// </summary>
    [Fact]
    public void Publish_should_reject_graph_without_start_node()
    {
        var definition = WorkflowDefinition.Create("broken", "Broken", "admin@km.local");
        definition.AddNode("end", WorkflowNodeType.End, 0, 0, "{}");

        var act = () => definition.Publish("admin@km.local");

        act.Should().Throw<InvalidOperationException>().WithMessage("*start node*");
    }

    /// <summary>
    /// Ensures normal nodes cannot branch to multiple outgoing paths without condition semantics.
    /// </summary>
    [Fact]
    public void Validate_should_reject_multiple_outgoing_edges_from_non_condition_node()
    {
        var definition = WorkflowDefinition.Create("broken", "Broken", "admin@km.local");
        var start = definition.AddNode("start", WorkflowNodeType.Start, 0, 0, "{}");
        var email = definition.AddNode("email", WorkflowNodeType.Email, 100, 0, "{}");
        var endA = definition.AddNode("end-a", WorkflowNodeType.End, 200, 0, "{}");
        var endB = definition.AddNode("end-b", WorkflowNodeType.End, 200, 100, "{}");
        definition.AddEdge(start.NodeKey, email.NodeKey, null);
        definition.AddEdge(email.NodeKey, endA.NodeKey, null);
        definition.AddEdge(email.NodeKey, endB.NodeKey, null);

        var errors = definition.Validate();

        errors.Should().Contain(error => error.Contains("Condition", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ensures all requested enterprise node types are available.
    /// </summary>
    [Fact]
    public void WorkflowNodeType_should_include_enterprise_nodes()
    {
        Enum.GetNames<WorkflowNodeType>().Should().Contain(["Ocr", "Ai", "Email", "Folder", "Webhook", "Condition", "Loop", "Approval"]);
    }
}
