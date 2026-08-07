using Crud.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public static class LoginRoute
{    
    public static void MapLoginRoutes(this WebApplication app, byte[] chaveEmBytes)
    {
        var route = app.MapGroup("/login");

        route.MapPost("", async Task<IResult> (LoginInputDto input, CrudContext db) =>
        {
            var errosValidacao = ValidateDataAnnotations.Validate(input);
            if (errosValidacao != null)
            {
                return errosValidacao; // Retorna HTTP 400 Bad Request com a lista de erros estruturada
            }

            var usuario = await db.Usuarios
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Email == input.Email);
    
            if (usuario == null || usuario.SenhaHash != input.SenhaHash)
            {
                return Results.Unauthorized();
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())]),
                // Expires = DateTime.UtcNow.AddHours(2), //Duas horas de validade
                Expires = DateTime.UtcNow.AddSeconds(30),  //Trinta segundos de validade para teste 
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(chaveEmBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Results.Ok(new { token = tokenString });
        });
    }

}

