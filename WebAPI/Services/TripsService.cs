using Microsoft.Data.SqlClient;
using WebAPI.Models.DTOs;

namespace WebAPI.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=apbd;Integrated Security=True;";

    //1. GET /api/trips
    
    //zwraca liste wszystkich wycieczek wraz z ich szczegolami i krajami
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        
        //zapytanie zwracajace dane o wycieczkach i przypisanych do nich krajach
        var cmd = new SqlCommand(@"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               c.Name AS CountryName
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
        ORDER BY t.IdTrip
    ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        TripDTO? currentTrip = null;
        int? lastTripId = null;

        while (await reader.ReadAsync())
        {
            int tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

            //jeslo nowy trip otworz nowy obiekt DTO
            if (tripId != lastTripId)
            {
                currentTrip = new TripDTO
                {
                    IdTrip = tripId,
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                    Countries = new List<CountryDTO>()
                };

                trips.Add(currentTrip);
                lastTripId = tripId;
            }

            //jesli przypisany kraj istnieje, dodaj go do listy 
            if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
            {
                currentTrip!.Countries.Add(new CountryDTO
                {
                    Name = reader.GetString(reader.GetOrdinal("CountryName"))
                });
            }
        }
        return trips;
    }
}