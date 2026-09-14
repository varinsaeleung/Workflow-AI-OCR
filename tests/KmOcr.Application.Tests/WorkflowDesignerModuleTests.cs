using FluentAssertions;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.WorkflowEngine;
using KmOcr.Domain.WorkflowEngine;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Tests workflow designer application use cases.
/// </summary>
public sealed class WorkflowDesignerModuleTests
{
    /// <summary>
    /// Ensures creating a definition persists it through the unit of work.
    /// </summary>
    [Fact]
    public async Task CreateAsync_should_persist_workflow_definition()
    {
        var repository = new FakeWorkflowDefinitionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var module = new WorkflowDesignerModule(repository, unitOfWork, NullLogger<WorkflowDesignerModule>.Instance);

        var result = await module.CreateAsync(new CreateWorkflowDefinitionCommand("invoice-review", "Invoice Review", "admin@km.local"), CancellationToken.None);

        result.Code.Should().Be("invoice-review");
        repository.AddedDefinition.Should().NotBeNull();
        unitOfWork.SaveCount.Should().Be(1);
    }

    /// <summary>
    /// Ensures updating graph nodes and edges stores graph state.
    /// </summary>
    [Fact]
    public async Task UpdateGraphAsync_should_replace_nodes_and_edges()
    {
        var repository = new FakeWorkflowDefinitionRepository();
        var definition = WorkflowDefinition.Create("invoice-review", "Invoice Review", "admin@km.local");
        repository.Definition = definition;
        var module = new WorkflowDesignerModule(repository, new FakeUnitOfWork(), NullLogger<WorkflowDesignerModule>.Instance);
        var graph = new WorkflowGraphDto(
            [
                new WorkflowNodeDto("start", "Start", 0, 0, "{}"),
                new WorkflowNodeDto("end", "End", 200, 0, "{}")
            ],
            [
                new WorkflowEdgeDto("start", "end", null)
            ]);

        var result = await module.UpdateGraphAsync(definition.Id, graph, CancellationToken.None);

        result.Graph.Nodes.Should().HaveCount(2);
        result.Graph.Edges.Should().ContainSingle();
    }

    /// <summary>
    /// Ensures publishing a valid graph returns the published version.
    /// </summary>
    [Fact]
    public async Task PublishAsync_should_publish_valid_definition()
    {
        var repository = new FakeWorkflowDefinitionRepository();
        var definition = WorkflowDefinition.Create("invoice-review", "Invoice Review", "admin@km.local");
        definition.AddNode("start", WorkflowNodeType.Start, 0, 0, "{}");
        definition.AddNode("end", WorkflowNodeType.End, 200, 0, "{}");
        definition.AddEdge("start", "end", null);
        repository.Definition = definition;
        var module = new WorkflowDesignerModule(repository, new FakeUnitOfWork(), NullLogger<WorkflowDesignerModule>.Instance);

        var result = await module.PublishAsync(definition.Id, "admin@km.local", CancellationToken.None);

        result.PublishedVersionNumber.Should().Be(1);
        result.IsPublished.Should().BeTrue();
    }

    private sealed class FakeWorkflowDefinitionRepository : IWorkflowDefinitionRepository
    {
        /// <summary>
        /// Gets or sets the stored workflow definition.
        /// </summary>
        public WorkflowDefinition? Definition { get; set; }

        /// <summary>
        /// Gets the added definition.
        /// </summary>
        public WorkflowDefinition? AddedDefinition { get; private set; }

        /// <summary>
        /// Adds a workflow definition to memory.
        /// </summary>
        public Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
        {
            AddedDefinition = definition;
            Definition = definition;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Loads a workflow definition by id.
        /// </summary>
        public Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Definition?.Id == id ? Definition : null);
        }

        /// <summary>
        /// Lists stored workflow definitions.
        /// </summary>
        public Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<WorkflowDefinition>>(Definition is null ? [] : [Definition]);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Gets the number of save calls.
        /// </summary>
        public int SaveCount { get; private set; }

        /// <summary>
        /// Counts a unit-of-work commit.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
