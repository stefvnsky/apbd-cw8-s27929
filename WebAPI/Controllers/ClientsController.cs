using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers;

// Endpoint zapisuje klienta na konkretną wycieczkę
// Jeśli klient nie istnieje (sprawdzany po PESEL), zostaje utworzony
// Jeśli klient istnieje, zostaje ponownie wykorzystany

[ApiController]
[Route("api/trips/{tripId}/clients")]
public class ClientsController : ControllerBase
{
    private readonly IClientsService _clientsService;

    public ClientsController(IClientsService clientsService)
    {
        _clientsService = clientsService;
    }

    [HttpPost]
    public async Task<IActionResult> AddClientToTrip(int tripId, [FromBody] ClientTripDTO dto)
    {
        try
        {
            await _clientsService.AddClientToTripAsync(tripId, dto);
            return StatusCode(201); // Created
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}