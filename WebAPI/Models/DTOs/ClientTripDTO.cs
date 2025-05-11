namespace WebAPI.Models.DTOs;

public class ClientTripDTO
{
    //przyjmowanie danych klienta przy zapisie na wycieczke
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Telephone { get; set; }
    public string Pesel { get; set; }
    
    public int PaymentDate { get; set; }
    
    public string? TripName { get; set; }
    public string? TripDescription { get; set; }
    public DateTime? TripDateFrom { get; set; }
    public DateTime? TripDateTo { get; set; }

}