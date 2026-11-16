# API de réservation de restaurant

Petite API REST en ASP.NET Core (.NET 10, minimal API) pour gérer les réservations d'une salle de 40 couverts. Les données sont gardées en mémoire et disparaissent à l'arrêt du serveur.

## Lancer le projet

Il faut le SDK .NET 10. La version est fixée par `global.json`.

```bash
dotnet run --project src/RestaurantApi --urls http://localhost:5000
dotnet test
```

## Routes

| Méthode | Route | Réponses |
|---|---|---|
| `POST` | `/reservations` | 201 avec l'en-tête `Location`, 400 si une règle est violée |
| `GET` | `/reservations?date=2030-06-12` | 200, réservations du jour triées par heure |
| `GET` | `/reservations/{id}` | 200 ou 404 |
| `DELETE` | `/reservations/{id}` | 204 ou 404 |

```bash
curl -i -X POST http://localhost:5000/reservations \
  -H 'Content-Type: application/json' \
  -d '{"name":"Dupont","dateTime":"2030-06-12T19:30:00","covers":4}'
```

Les erreurs suivent le format ProblemDetails (RFC 9457) avec le motif dans `detail`.

## Règles métier

- pas de réservation dans le passé
- de 1 à 8 personnes par réservation
- 40 couverts au plus par créneau, un créneau correspondant à une heure pleine (19h00 à 19h59)

## Organisation du code

- `Reservation.cs` : les records `Reservation` et `CreateReservationRequest`, plus le résultat d'une création
- `ReservationService.cs` : les règles métier, enregistré en singleton par injection de dépendances. Un verrou protège la vérification de capacité contre deux créations simultanées.
- `Program.cs` : configuration des services et déclaration des routes
- `tests/RestaurantApi.Tests` : tests xUnit du service, avec une horloge (`TimeProvider`) figée pour ne pas dépendre de la date du jour
