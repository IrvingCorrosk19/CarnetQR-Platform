using CarnetQRPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class CardsController : Controller
{
    private readonly ICardService _cardService;
    private readonly ILogger<CardsController> _logger;

    public CardsController(ICardService cardService, ILogger<CardsController> logger)
    {
        _cardService = cardService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var cards = await _cardService.GetAllAsync();
        return View(cards);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var card = await _cardService.GetByIdAsync(id);
        if (card == null)
        {
            return NotFound();
        }

        return View(card);
    }
}

