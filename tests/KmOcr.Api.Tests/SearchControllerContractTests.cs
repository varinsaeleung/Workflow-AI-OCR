using FluentAssertions;
using KmOcr.Api.Controllers;
using KmOcr.Application.Search;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KmOcr.Api.Tests;

/// <summary>
/// Verifies Search API response contract metadata.
/// </summary>
public sealed class SearchControllerContractTests
{
    /// <summary>
    /// Ensures the Search API advertises the highlighted OpenSearch response contract.
    /// </summary>
    [Fact]
    public void SearchAsync_should_produce_document_search_response()
    {
        var attribute = typeof(SearchController)
            .GetMethod(nameof(SearchController.SearchAsync))!
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
            .Cast<ProducesResponseTypeAttribute>()
            .Single(item => item.StatusCode == 200);

        attribute.Type.Should().Be(typeof(DocumentSearchResponseDto));
    }
}
