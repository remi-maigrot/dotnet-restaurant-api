namespace RestaurantApi;

public interface IReservationService
{
    Task<CreateResult> CreateAsync(CreateReservationRequest request);
    Task<IReadOnlyList<Reservation>> GetByDateAsync(DateOnly date);
    Task<Reservation?> GetByIdAsync(Guid id);
    Task<bool> CancelAsync(Guid id);
}

// Porte toutes les règles métier. Enregistré en Singleton : une seule instance
// pour toute l'appli, sinon le stockage en mémoire serait perdu à chaque requête.
public class ReservationService(TimeProvider timeProvider) : IReservationService
{
    public const int MaxCoversPerReservation = 8;
    public const int RoomCapacity = 40;

    private readonly Dictionary<Guid, Reservation> _reservations = new();

    // Kestrel traite les requêtes en parallèle : sans verrou, deux POST simultanés
    // pourraient tous deux voir 36 couverts et passer à 44 (check-then-act).
    private readonly Lock _lock = new();

    public Task<CreateResult> CreateAsync(CreateReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Task.FromResult(CreateResult.Fail("Le nom est obligatoire."));

        if (request.Covers < 1)
            return Task.FromResult(CreateResult.Fail("Il faut au moins 1 couvert."));

        if (request.Covers > MaxCoversPerReservation)
            return Task.FromResult(CreateResult.Fail(
                $"{MaxCoversPerReservation} personnes maximum par réservation."));

        var now = timeProvider.GetLocalNow().DateTime;
        if (request.DateTime <= now)
            return Task.FromResult(CreateResult.Fail("Impossible de réserver dans le passé."));

        lock (_lock)
        {
            var slot = SlotOf(request.DateTime);
            var alreadyBooked = _reservations.Values
                .Where(r => SlotOf(r.DateTime) == slot)
                .Sum(r => r.Covers);

            if (alreadyBooked + request.Covers > RoomCapacity)
                return Task.FromResult(CreateResult.Fail(
                    $"Créneau complet : {RoomCapacity - alreadyBooked} couvert(s) restant(s)."));

            var reservation = new Reservation(
                Guid.NewGuid(), request.Name.Trim(), request.DateTime, request.Covers);
            _reservations[reservation.Id] = reservation;
            return Task.FromResult(CreateResult.Ok(reservation));
        }
    }

    public Task<IReadOnlyList<Reservation>> GetByDateAsync(DateOnly date)
    {
        lock (_lock)
        {
            IReadOnlyList<Reservation> result = _reservations.Values
                .Where(r => DateOnly.FromDateTime(r.DateTime) == date)
                .OrderBy(r => r.DateTime)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Reservation?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_reservations.GetValueOrDefault(id));
        }
    }

    public Task<bool> CancelAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_reservations.Remove(id));
        }
    }

    // Un créneau = une heure pleine (19:00 à 19:59).
    private static DateTime SlotOf(DateTime dateTime) =>
        new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
}
