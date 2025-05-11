using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Controllers;

//pobieranie

[ApiController]
[Route("api/clients/{id}/trips")]
public class ClientsTripsController : ControllerBase
{
    private readonly IClientsService _clientsService;

    public ClientsTripsController(IClientsService clientsService)
    {
        _clientsService = clientsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        try
        {
            var trips = await _clientsService.GetClientTripsAsync(id);
            return Ok(trips);
        }
        catch (Exception e)
        {
            return NotFound(new { error = e.Message });
        }
    }
    
    [HttpPut]
    public async Task<IActionResult> RegisterClientToTrip(int id, [FromQuery] int tripId, [FromQuery] int paymentDate)
    {
        try
        {
            await _clientsService.RegisterClientToTripAsync(id, tripId, paymentDate);
            return Ok("Klient zapisany na wycieczkę.");
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
    
    [HttpDelete("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> RemoveClientFromTrip(int clientId, int tripId)
    {
        try
        {
            await _clientsService.RemoveClientFromTripAsync(clientId, tripId);
            return Ok("Zapis klienta na wycieczkę został usunięty.");
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}