using KmOcr.Application.Auth;
using KmOcr.Application.Common;
using KmOcr.Application.Ocr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// OCR APIs for direct file extraction.
/// </summary>
[ApiController]
[Route("api/v1/ocr")]
[Produces("application/json")]
public sealed class OcrController : ControllerBase
{
    private readonly IOcrModule _ocr;

    /// <summary>
    /// Creates the OCR controller with the OCR application module.
    /// </summary>
    public OcrController(IOcrModule ocr)
    {
        _ocr = ocr;
    }

    /// <summary>
    /// Extracts Thai and English OCR text from a PDF, JPG, or PNG file and returns structured JSON.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsAi)]
    [HttpPost("extract")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(OcrResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OcrResultDto>> ExtractAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("Uploaded file is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _ocr.ExtractAsync(new OcrExtractCommand(file.FileName, file.ContentType, stream), cancellationToken);
        return Ok(result);
    }
}
