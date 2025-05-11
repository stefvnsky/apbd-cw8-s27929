using WebAPI.Models.DTOs;

namespace WebAPI.Services;

public interface IClientsService
{
    //metoda do zapisu klienta na wycieczke (dodatkowa)
    Task AddClientToTripAsync(int tripId, ClientTripDTO dto);
    
    //2. GET 
    Task<IEnumerable<ClientTripDTO>> GetClientTripsAsync(int clientId);
    
    //3. POST
    Task<int> AddClientAsync(ClientDTO dto);
    
    //4. PUT
    Task RegisterClientToTripAsync(int clientId, int tripId, int paymentDate);
    
    //5. DELETE
    Task RemoveClientFromTripAsync(int clientId, int tripId);

}