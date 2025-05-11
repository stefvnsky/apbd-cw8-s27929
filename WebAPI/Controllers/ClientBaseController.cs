using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers;

// Dodawanie nowego klienta (osobno, bez wycieczki)

[ApiController]
[Route("api/clients")]
public class ClientsBaseController : ControllerBase
{
    private readonly IClientsService _clientsService;

    public ClientsBaseController(IClientsService clientsService)
    {
        _clientsService = clientsService;
    }

    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] ClientDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // 400 i opis bledow walidacji
        }

        try
        {
            var newId = await _clientsService.AddClientAsync(dto);
            return Created($"/api/clients/{newId}", new { IdClient = newId });
        }
        catch (Exception e)
        {
            if (e.Message.Contains("PESEL"))
                return Conflict(new { error = e.Message }); // 409 conflict jesli PESEL juz istnieje

            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
}