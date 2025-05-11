using Microsoft.Data.SqlClient;
using WebAPI.Models.DTOs;

namespace WebAPI.Services;

public class ClientsService : IClientsService
{
     private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=apbd;Integrated Security=True;";
     
     /*dodatkowa pomocniacza koncowka
        - umozliwia zapis klienta na wycieczke
        - jestli klient nie jestnieje, jest tworzony
        - jesli istnieje, uzywa istniejacego wpisu
        - dodaje wpis do tabeli Client_Trip*/
    public async Task AddClientToTripAsync(int tripId, ClientTripDTO dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            //Sprawdzanie czy klient już istnieje (po PESEL)
            var checkClientCmd = new SqlCommand("SELECT IdClient FROM Client WHERE Pesel = @Pesel", connection, transaction);
            checkClientCmd.Parameters.AddWithValue("@Pesel", dto.Pesel);
            var clientIdObj = await checkClientCmd.ExecuteScalarAsync();

            int clientId;

            if (clientIdObj == null)
            {
                //Dodanie klienta
                var insertClientCmd = new SqlCommand(@"
                    INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                    OUTPUT INSERTED.IdClient
                    VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)
                ", connection, transaction);

                insertClientCmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
                insertClientCmd.Parameters.AddWithValue("@LastName", dto.LastName);
                insertClientCmd.Parameters.AddWithValue("@Email", dto.Email);
                insertClientCmd.Parameters.AddWithValue("@Telephone", dto.Telephone);
                insertClientCmd.Parameters.AddWithValue("@Pesel", dto.Pesel);

                clientId = (int)(await insertClientCmd.ExecuteScalarAsync())!;
            }
            else
            {
                clientId = (int)clientIdObj;
            }

            //Sprawdzanie czy wycieczka istnieje
            var checkTripCmd = new SqlCommand("SELECT COUNT(1) FROM Trip WHERE IdTrip = @TripId", connection, transaction);
            checkTripCmd.Parameters.AddWithValue("@TripId", tripId);
            var tripExists = (int)(await checkTripCmd.ExecuteScalarAsync()) > 0;

            if (!tripExists)
                throw new Exception("Wycieczka nie istnieje.");

            //Sprawdzanie czy klient juz zapisany
            var checkIfAlreadyCmd = new SqlCommand("SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", connection, transaction);
            checkIfAlreadyCmd.Parameters.AddWithValue("@ClientId", clientId);
            checkIfAlreadyCmd.Parameters.AddWithValue("@TripId", tripId);
            var alreadyRegistered = (int)(await checkIfAlreadyCmd.ExecuteScalarAsync()) > 0;

            if (alreadyRegistered)
                throw new Exception("Klient już zapisany na tę wycieczkę.");

            //Wstawianie zapis
            var insertCmd = new SqlCommand(@"
                INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                VALUES (@ClientId, @TripId, @RegisteredAt, @PaymentDate)
            ", connection, transaction);

            int registereAt = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));
            
            insertCmd.Parameters.AddWithValue("@ClientId", clientId);
            insertCmd.Parameters.AddWithValue("@TripId", tripId);
            insertCmd.Parameters.AddWithValue("@RegisteredAt", registereAt);
            insertCmd.Parameters.AddWithValue("@PaymentDate", dto.PaymentDate);

            await insertCmd.ExecuteNonQueryAsync();

            //Zatwierdzanie transakcji
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // GET /api/clients/{id}/trips
    public async Task<IEnumerable<ClientTripDTO>> GetClientTripsAsync(int clientId)
    {
        var result = new List<ClientTripDTO>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        //Sprawdzanie, czy klient istnieje
        var checkClientCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE IdClient = @Id", connection);
        checkClientCmd.Parameters.AddWithValue("@Id", clientId);

        var exists = (int)(await checkClientCmd.ExecuteScalarAsync()) > 0;
        if (!exists)
        {
            throw new Exception("Klient o podanym ID nie istnieje.");
        }

        //Pobieranie danych o wycieczkach klienta
        var cmd = new SqlCommand(@"
        SELECT 
            t.Name AS TripName,
            t.Description,
            t.DateFrom,
            t.DateTo,
            ct.PaymentDate
        FROM Trip t
        INNER JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
        WHERE ct.IdClient = @Id
    ", connection);

        cmd.Parameters.AddWithValue("@Id", clientId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ClientTripDTO
            {
                TripName = reader.GetString(reader.GetOrdinal("TripName")),
                TripDescription = reader.GetString(reader.GetOrdinal("Description")),
                TripDateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                TripDateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                PaymentDate = reader.GetInt32(reader.GetOrdinal("PaymentDate"))
            });
        }
        return result; //zwrocenie listy zdanymi o wycieczkach i platnosciach
    }
    
    // POST /api/clients
    public async Task<int> AddClientAsync(ClientDTO dto)
    {
        //przyjmuje ClientDTO z wymaganymi polami
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        //Sprawdzenie czy klient o podanym PESEL już istnieje
        var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE Pesel = @Pesel", connection);
        checkCmd.Parameters.AddWithValue("@Pesel", dto.Pesel);
        var exists = (int)(await checkCmd.ExecuteScalarAsync()) > 0;

        if (exists)
            throw new Exception("Klient o podanym numerze PESEL już istnieje.");

        //Wstawienie nowego klienta(nowy rekord)
        var insertCmd = new SqlCommand(@"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)
    ", connection);

        insertCmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
        insertCmd.Parameters.AddWithValue("@LastName", dto.LastName);
        insertCmd.Parameters.AddWithValue("@Email", dto.Email);
        insertCmd.Parameters.AddWithValue("@Telephone", dto.Telephone);
        insertCmd.Parameters.AddWithValue("@Pesel", dto.Pesel);

        var newId = (int)(await insertCmd.ExecuteScalarAsync())!;
        return newId; //zwrocenie nowego IdClient
    }
    
    // PUT /api/clients/{id}/trips/{tripId}
    public async Task RegisterClientToTripAsync(int clientId, int tripId, int paymentDate)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            //Sprawdza czy klient istnieje
            var checkClientCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE IdClient = @ClientId", connection, transaction);
            checkClientCmd.Parameters.AddWithValue("@ClientId", clientId);
            var clientExists = (int)(await checkClientCmd.ExecuteScalarAsync()) > 0;
            if (!clientExists)
                throw new Exception("Klient nie istnieje.");

            //Sprawdza czy wycieczka istnieje
            var checkTripCmd = new SqlCommand("SELECT COUNT(1) FROM Trip WHERE IdTrip = @TripId", connection, transaction);
            checkTripCmd.Parameters.AddWithValue("@TripId", tripId);
            var tripExists = (int)(await checkTripCmd.ExecuteScalarAsync()) > 0;
            if (!tripExists)
                throw new Exception("Wycieczka nie istnieje.");

            //Sprawdza czy klient zostal juz zapisany
            var checkDuplicateCmd = new SqlCommand("SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", connection, transaction);
            checkDuplicateCmd.Parameters.AddWithValue("@ClientId", clientId);
            checkDuplicateCmd.Parameters.AddWithValue("@TripId", tripId);
            var alreadyRegistered = (int)(await checkDuplicateCmd.ExecuteScalarAsync()) > 0;
            if (alreadyRegistered)
                throw new Exception("Klient już zapisany na tę wycieczkę.");

            //Sprawdza liczbe zapisanych osob do maksymalnej
            var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId", connection, transaction);
            countCmd.Parameters.AddWithValue("@TripId", tripId);
            int currentCount = (int)(await countCmd.ExecuteScalarAsync());

            var maxCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId", connection, transaction);
            maxCmd.Parameters.AddWithValue("@TripId", tripId);
            int maxPeople = (int)(await maxCmd.ExecuteScalarAsync());

            if (currentCount >= maxPeople)
                throw new Exception("Limit uczestników wycieczki został osiągnięty.");

            //Dodaj wpis do Client_Trip
            var insertCmd = new SqlCommand(@"
                INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                VALUES (@ClientId, @TripId, @RegisteredAt, @PaymentDate)
            ", connection, transaction);

            int registeredAt = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));
            
            insertCmd.Parameters.AddWithValue("@ClientId", clientId);
            insertCmd.Parameters.AddWithValue("@TripId", tripId);
            insertCmd.Parameters.AddWithValue("@RegisteredAt", registeredAt);
            insertCmd.Parameters.AddWithValue("@PaymentDate", paymentDate);

            await insertCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // DELETE /api/clients/{id}/trips/{tripId}
    public async Task RemoveClientFromTripAsync(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            //Sprawdzenie, czy klient istnieje
            var checkClientCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE IdClient = @ClientId", connection, transaction);
            checkClientCmd.Parameters.AddWithValue("@ClientId", clientId);
            var clientExists = (int)(await checkClientCmd.ExecuteScalarAsync()) > 0;
            if (!clientExists)
                throw new Exception("Klient nie istnieje.");

            //Sprawdzenie, czy przypisanie istnieje
            var checkAssignmentCmd = new SqlCommand("SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", connection, transaction);
            checkAssignmentCmd.Parameters.AddWithValue("@ClientId", clientId);
            checkAssignmentCmd.Parameters.AddWithValue("@TripId", tripId);
            var assigned = (int)(await checkAssignmentCmd.ExecuteScalarAsync()) > 0;
            //jak nie istnieje
            if (!assigned)
                throw new Exception("Klient nie jest zapisany na tę wycieczkę.");

            //Usun przypisanie
            var deleteCmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", connection, transaction);
            deleteCmd.Parameters.AddWithValue("@ClientId", clientId);
            deleteCmd.Parameters.AddWithValue("@TripId", tripId);
            await deleteCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}