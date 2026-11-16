using RestaurantApi;

namespace RestaurantApi.Tests;

public class ReservationServiceTests
{
    // Horloge figée : les tests ne dépendent pas de la date du jour.
    private static readonly DateTime Now = new(2030, 6, 12, 12, 0, 0);
    private readonly ReservationService _service = new(new FixedTimeProvider(Now));

    [Fact]
    public async Task Refuse_une_reservation_dans_le_passe()
    {
        var result = await _service.CreateAsync(new("Dupont", Now.AddHours(-1), 2));

        Assert.False(result.IsSuccess);
        Assert.Contains("passé", result.Error);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(8, true)]
    [InlineData(9, false)]
    public async Task Limite_a_8_couverts_par_reservation(int covers, bool expectedSuccess)
    {
        var result = await _service.CreateAsync(new("Dupont", Now.AddDays(1), covers));

        Assert.Equal(expectedSuccess, result.IsSuccess);
    }

    [Fact]
    public async Task Refuse_si_le_creneau_depasse_40_couverts()
    {
        var evening = Now.Date.AddDays(1).AddHours(19);
        for (var i = 0; i < 5; i++)
            Assert.True((await _service.CreateAsync(new($"Groupe {i}", evening, 8))).IsSuccess);

        // 40 couverts pris à 19h : refusé à 19h30 (même créneau), accepté à 20h.
        var sameSlot = await _service.CreateAsync(new("Martin", evening.AddMinutes(30), 1));
        var nextSlot = await _service.CreateAsync(new("Martin", evening.AddHours(1), 1));

        Assert.False(sameSlot.IsSuccess);
        Assert.True(nextSlot.IsSuccess);
    }

    [Fact]
    public async Task Annuler_libere_la_place_et_un_second_appel_echoue()
    {
        var created = await _service.CreateAsync(new("Dupont", Now.AddDays(1), 4));
        var id = created.Reservation!.Id;

        Assert.True(await _service.CancelAsync(id));
        Assert.False(await _service.CancelAsync(id));
        Assert.Null(await _service.GetByIdAsync(id));
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }
}
