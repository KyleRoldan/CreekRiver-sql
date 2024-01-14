using CreekRiver.Models;
using CreekRiver.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

//////////DONT TOUCH THIS CODE//////////////////////////////////////////////////////////////////////////////////////

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// allows our api endpoints to access the database through Entity Framework Core
builder.Services.AddNpgsql<CreekRiverDbContext>(builder.Configuration["CreekRiverDbConnectionString"]);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


///////////////////////////GET ALL CAMPSITES///////////////////////////////////////////////////////////////////////

app.MapGet("/api/campsites", (CreekRiverDbContext db) =>
{
    return db.Campsites
    .Select(c => new CampsiteDTO
    {
        Id = c.Id,
        Nickname = c.Nickname,
        ImageUrl = c.ImageUrl,
        CampsiteTypeId = c.CampsiteTypeId
    }).ToList();
});

///////////////////////////GET ALL CAMPSITES BY ID///////////////////////////////////////////////////////////////////////
///
//////////////TEST THIS THEN TEST WITH ERROR HANDLING CODE//////////////////////////

app.MapGet("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    return db.Campsites
        .Include(c => c.CampsiteType)
        .Select(c => new CampsiteDTO
        {
            Id = c.Id,
            Nickname = c.Nickname,
            CampsiteTypeId = c.CampsiteTypeId,
            CampsiteType = new CampsiteTypeDTO
            {
                Id = c.CampsiteType.Id,
                CampsiteTypeName = c.CampsiteType.CampsiteTypeName,
                FeePerNight = c.CampsiteType.FeePerNight,
                MaxReservationDays = c.CampsiteType.MaxReservationDays
            }
        })
        .Single(c => c.Id == id);


//////////////ENDPOINT WITH ERROR HANDLING....DOES THIS WORK??///////////////////////////////////////////
///
//         app.MapGet("/api/campsites/{id}", async (CreekRiverDbContext db, int id) =>
// {
//     var campsite = await db.Campsites
//         .Include(c => c.CampsiteType)
//         .Where(c => c.Id == id)
//         .Select(c => new CampsiteDTO
//         {
//             Id = c.Id,
//             Nickname = c.Nickname,
//             CampsiteTypeId = c.CampsiteTypeId,
//             CampsiteType = new CampsiteTypeDTO
//             {
//                 Id = c.CampsiteType.Id,
//                 CampsiteTypeName = c.CampsiteType.CampsiteTypeName,
//                 FeePerNight = c.CampsiteType.FeePerNight,
//                 MaxReservationDays = c.CampsiteType.MaxReservationDays
//             }
//         })
//         .FirstOrDefaultAsync();

//     if (campsite == null)
//     {
//         return Results.NotFound("Campsite not found");
//     }

//     return campsite;
// });

});

//////CREATING A CAMPSITE//////////////////////////////////////////////////////////////////////////////////////

app.MapPost("/api/campsites", (CreekRiverDbContext db, Campsite campsite) =>
{
    db.Campsites.Add(campsite);
    db.SaveChanges();
    return Results.Created($"/api/campsites/{campsite.Id}", campsite);
});

/////DELETE A CAMPSITE/////////////////////////////////////////////////////////////////////////////////////////////

app.MapDelete("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    Campsite campsite = db.Campsites.SingleOrDefault(campsite => campsite.Id == id);
    if (campsite == null)
    {
        return Results.NotFound();
    }
    db.Campsites.Remove(campsite);
    db.SaveChanges();
    return Results.NoContent();

});

//////UPDATE A CAMPSITES DETAILS//////////////////////////////////////////////////////////////////////////////////

///TEST ENDPOINT WITH THIS DATA/////////////////////////////////////////////////

// {
//     "id": 1,
//     "nickname": "updated nickname",
//     "campsiteTypeId": 2,
//     "imageUrl": "path.to.my.image/image.jpg"
// }

app.MapPut("/api/campsites/{id}", (CreekRiverDbContext db, int id, Campsite campsite) =>
{
    Campsite campsiteToUpdate = db.Campsites.SingleOrDefault(campsite => campsite.Id == id);
    if (campsiteToUpdate == null)
    {
        return Results.NotFound();
    }
    campsiteToUpdate.Nickname = campsite.Nickname;
    campsiteToUpdate.CampsiteTypeId = campsite.CampsiteTypeId;
    campsiteToUpdate.ImageUrl = campsite.ImageUrl;

    db.SaveChanges();
    return Results.NoContent();
});
///////GETTING RESERVATIONS////////////////////////////////////////////////////////////////////////////////////////

app.MapGet("/api/reservations", (CreekRiverDbContext db) =>
{
    return db.Reservations
        .Include(r => r.UserProfile)
        .Include(r => r.Campsite)
        .ThenInclude(c => c.CampsiteType)
        .OrderBy(res => res.CheckinDate)
        .Select(r => new ReservationDTO
        {
            Id = r.Id,
            CampsiteId = r.CampsiteId,
            UserProfileId = r.UserProfileId,
            CheckinDate = r.CheckinDate,
            CheckoutDate = r.CheckoutDate,
            UserProfile = new UserProfileDTO
            {
                Id = r.UserProfile.Id,
                FirstName = r.UserProfile.FirstName,
                LastName = r.UserProfile.LastName,
                Email = r.UserProfile.Email
            },
            Campsite = new CampsiteDTO
            {
                Id = r.Campsite.Id,
                Nickname = r.Campsite.Nickname,
                ImageUrl = r.Campsite.ImageUrl,
                CampsiteTypeId = r.Campsite.CampsiteTypeId,
                CampsiteType = new CampsiteTypeDTO
                {
                    Id = r.Campsite.CampsiteType.Id,
                    CampsiteTypeName = r.Campsite.CampsiteType.CampsiteTypeName,
                    MaxReservationDays = r.Campsite.CampsiteType.MaxReservationDays,
                    FeePerNight = r.Campsite.CampsiteType.FeePerNight
                }
            }
        })
        .ToList();
});

////////////////////CREATING RESERVATIONS//////////////////////////////////////////////////////////////////////////


///////TEST WITH THIS DATA//////////////////////////////////////////////////////////

// {
//     "userProfileId": 1,
//     "campsiteId": 1,
//     "checkinDate": "2023-07-18",
//     "checkoutDate": "2023-07-20"
// } 

app.MapPost("/api/reservations", (CreekRiverDbContext db, Reservation newRes) =>
{
    db.Reservations.Add(newRes);
    db.SaveChanges();
    return Results.Created($"/api/reservations/{newRes.Id}", newRes);
});

/////////READ INSTRUCTIONS AND THEN TRY THIS CATCH ERROR HANDLING////////////////////

// try
// {
//     db.Reservations.Add(newRes);
//     db.SaveChanges();
//     return Results.Created($"/api/reservations/{newRes.Id}", newRes);
// }
// catch (DbUpdateException)
// {
//     return Results.BadRequest("Invalid data submitted");
// }


//////////DELETING A RESERVATION/////////////////////////////////////////////////////////////////////////////////////////


//////////TEST THIS!!!!///////////////////////////////////////////////////////////////

app.MapDelete("/api/reservations/{id}", (CreekRiverDbContext db, int id) =>
{
    Reservation reservation = db.Reservations.SingleOrDefault(reservation => reservation.Id == id);
    if (reservation == null)
    {
        return Results.NotFound();
    }
    db.Reservations.Remove(reservation);
    db.SaveChanges();
    return Results.NoContent();

});


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
app.Run();