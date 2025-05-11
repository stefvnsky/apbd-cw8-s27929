using WebAPI.Models.DTOs;

namespace WebAPI.Services;

public interface ITripsService
{
    //1. GET
    Task<List<TripDTO>> GetTrips();     //zwracane listy wycieczek
}