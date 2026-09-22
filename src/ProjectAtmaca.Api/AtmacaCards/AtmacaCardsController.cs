using Microsoft.AspNetCore.Mvc;
using ProjectAtmaca.Application.AtmacaCards;

namespace ProjectAtmaca.Api.AtmacaCards;

[ApiController]
[Route("api/atmaca-cards")]
public sealed class AtmacaCardsController(
    IAtmacaCardReader reader) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AtmacaCardSummary>>> Search(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 2)
            return Ok(Array.Empty<AtmacaCardSummary>());

        var results = await reader.SearchAsync(
            search,
            cancellationToken: cancellationToken);

        return Ok(results);
    }
}
