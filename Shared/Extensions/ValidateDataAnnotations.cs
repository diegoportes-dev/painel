using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http; // Necessário para o TypedResults
// namespace Crud.Common.Helpers; // Adicione o namespace da sua pasta compartilhada


public static class ValidateDataAnnotations
{
    public static IResult? Validate(object request)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults
                .SelectMany(result =>
                {
                    var members = result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty };
                    return members.Select(member => new { Member = member, Error = result.ErrorMessage ?? "Valor inválido." });
                })
                .GroupBy(item => item.Member)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Error).Distinct().ToArray());

            return TypedResults.ValidationProblem(errors);
        }

        return null;
    }  
}
