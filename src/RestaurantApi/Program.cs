using RestaurantApi;

var builder = WebApplication.CreateBuilder(args);

// Injection de dépendances : on enregistre les services dans le conteneur.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IReservationService, ReservationService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Middleware : toute exception non gérée devient une réponse 500 au format ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

var reservations = app.MapGroup("/reservations");

reservations.MapPost("/", async (CreateReservationRequest request, IReservationService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/reservations/{result.Reservation!.Id}", result.Reservation)
        : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
});

reservations.MapGet("/", async (DateOnly date, IReservationService service) =>
    Results.Ok(await service.GetByDateAsync(date)));

reservations.MapGet("/{id:guid}", async (Guid id, IReservationService service) =>
    await service.GetByIdAsync(id) is { } reservation
        ? Results.Ok(reservation)
        : Results.NotFound());

reservations.MapDelete("/{id:guid}", async (Guid id, IReservationService service) =>
    await service.CancelAsync(id)
        ? Results.NoContent()
        : Results.NotFound());

app.Run();
