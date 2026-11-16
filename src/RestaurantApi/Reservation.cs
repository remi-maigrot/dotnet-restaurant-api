namespace RestaurantApi;

// Ce qui est stocké : l'Id est généré par le serveur, jamais fourni par le client.
public record Reservation(Guid Id, string Name, DateTime DateTime, int Covers);

// Corps JSON attendu par POST /reservations.
public record CreateReservationRequest(string Name, DateTime DateTime, int Covers);

// Résultat d'une création : soit une réservation, soit un message d'erreur métier.
public record CreateResult(Reservation? Reservation, string? Error)
{
    public bool IsSuccess => Error is null;

    public static CreateResult Ok(Reservation reservation) => new(reservation, null);
    public static CreateResult Fail(string error) => new(null, error);
}
