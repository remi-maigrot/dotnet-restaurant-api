# dotnet-restaurant-api

Une petite API de réservation pour un restaurant, écrite pour me mettre à C# et ASP.NET Core. Elle tourne sur .NET 10 en minimal API et garde tout en mémoire : pas de base de données, les réservations disparaissent quand on coupe le serveur.

On peut créer une réservation, lister celles d'un jour et en annuler une. La salle fait 40 couverts, une réservation va de 1 à 8 personnes et on ne peut pas réserver dans le passé. J'ai découpé la soirée en créneaux d'une heure : si 19h est complet, une réservation à 19h30 est refusée mais 20h passe.

## Lancer

```bash
dotnet run --project src/RestaurantApi --urls http://localhost:5000
dotnet test
```

## Essayer

```bash
# créer (201)
curl -i -X POST http://localhost:5000/reservations \
  -H 'Content-Type: application/json' \
  -d '{"name":"Dupont","dateTime":"2030-06-12T19:30:00","covers":4}'

# lister un jour
curl "http://localhost:5000/reservations?date=2030-06-12"

# annuler (204, ou 404 si l'id n'existe pas)
curl -i -X DELETE http://localhost:5000/reservations/<id>
```

Quand une règle n'est pas respectée, l'API répond 400 avec la raison dans le champ `detail`.

## Dans le code

Les règles sont dans `ReservationService`, injecté en singleton pour que les données survivent d'une requête à l'autre. La vérification de capacité est protégée par un verrou : sans lui, deux réservations envoyées en même temps pouvaient passer toutes les deux et dépasser les 40 couverts. L'heure courante passe par un `TimeProvider`, ce qui permet aux tests xUnit de figer la date.
