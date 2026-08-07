// namespace Crud;

using Crud.Data;
using Microsoft.EntityFrameworkCore;

public static class PersonRoute
{
    public static void MapPersonRoutes(this WebApplication app)
    {
        var route = app.MapGroup("/persons");
  
        route.MapPost("", 
            async Task<IResult> (PersonRequest req, CrudContext db) => {
                var person = new PersonModel(req.name);
                    await db.AddAsync(person);
                    await db.SaveChangesAsync();

                    return Results.Created($"/persons/{person.Id}", person);
            }
        );

        route.MapGet("",
            async Task<IResult>(CrudContext db) => {
                List<PersonModel>people = await db.People.ToListAsync();
                // return people;
                 return people is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(await db.People.ToListAsync());
            }
        );

        route.MapGet("/{id}",
            async Task<IResult> (Guid id, CrudContext db) =>
            {
                var person = await db.People.FirstOrDefaultAsync(p => p.Id == id);

                return person is null
                    ? Results.NotFound()
                    : Results.Ok(person);
            }
        );

        route.MapPut("/{id}",
            async Task<IResult> (Guid id, PersonRequest req, CrudContext db) =>
            {
                var person = await db.People.FirstOrDefaultAsync(p => p.Id == id);
                if (person is null) return Results.NotFound();

                person.UpdatePerson(req.name);
                await db.SaveChangesAsync();

                return Results.Ok(person);
            }
        );

        route.MapDelete("/{id}",
            async Task<IResult> ( Guid id, CrudContext db) =>
            {
                var person = await db.People.FirstOrDefaultAsync(p => p.Id == id);
                if (person is null) return Results.NotFound();

                db.People.Remove(person);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }
        );

        route.MapPatch("/{id}",
            async Task<IResult> (Guid id, PersonPatch req, CrudContext db) => {
                var person = await db.People.FirstAsync(p => p.Id == id);
                if (person is null) return Results.NotFound();

                if (req.Name is not null) person.UpdatePerson(req.Name);

                await db.SaveChangesAsync();

                return Results.Ok(person);
            }
        );

    ;}
}