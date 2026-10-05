using Crud.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public sealed record AtendimentoCollectionResponse(IReadOnlyList<AtendimentoModel> Data, PaginationMetadata Pagination, IReadOnlyList<HyperLink> Links);

public static class PainelRoute
{
    public static void MapPainelRoutes(this WebApplication app)
    {
        app.MapGet("/atendimento", async Task<IResult> (
            int? page,
            int? pageSize,
            int? guicheAtual,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            CrudContext dbContext,
            HttpContext httpContext) =>
        {
            if (guicheAtual is <= 0)
            {
                return Results.BadRequest(new { mensagem = "O guichê informado é inválido." });
            }

            if (dataInicio.HasValue && dataFim.HasValue && dataInicio.Value > dataFim.Value)
            {
                return Results.BadRequest(new { mensagem = "A data inicial não pode ser posterior à data final." });
            }

            var pageNumber = page is null or < 1 ? 1 : page.Value;
            var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;
            var query = dbContext.Atendimentos.AsQueryable();

            if (guicheAtual.HasValue)
            {
                query = query.Where(a => a.GuicheAtual == guicheAtual.Value);
            }

            if (dataInicio.HasValue)
            {
                var inicio = dataInicio.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.Created >= inicio);
            }

            if (dataFim.HasValue)
            {
                var fimExclusivo = dataFim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.Created < fimExclusivo);
            }

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

            var atendimentos = await query
                .OrderByDescending(a => a.Created)
                .ThenBy(a => a.NumeroAtual)
                .ThenBy(a => a.GuicheAtual)
                .Skip((pageNumber - 1) * requestedPageSize)
                .Take(requestedPageSize)
                .ToListAsync();

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var collectionUrl = $"{baseUrl}/atendimento";
            var filterQuery = new List<string>();
            if (guicheAtual.HasValue)
            {
                filterQuery.Add($"guicheAtual={guicheAtual.Value}");
            }

            if (dataInicio.HasValue)
            {
                filterQuery.Add($"dataInicio={dataInicio.Value:yyyy-MM-dd}");
            }

            if (dataFim.HasValue)
            {
                filterQuery.Add($"dataFim={dataFim.Value:yyyy-MM-dd}");
            }

            string BuildPageUrl(int targetPage) =>
                $"{collectionUrl}?{string.Join("&", filterQuery.Append($"page={targetPage}").Append($"pageSize={requestedPageSize}"))}";

            var links = new List<HyperLink>
            {
                new("self", BuildPageUrl(pageNumber), "GET"),
                new("collection", collectionUrl, "GET"),
                new("create", $"{collectionUrl}/criar", "POST")
            };

            if (pageNumber > 1)
            {
                links.Add(new HyperLink("prev", BuildPageUrl(pageNumber - 1), "GET"));
            }

            if (pageNumber < totalPages)
            {
                links.Add(new HyperLink("next", BuildPageUrl(pageNumber + 1), "GET"));
            }

            if (totalPages > 0)
            {
                links.Add(new HyperLink("first", BuildPageUrl(1), "GET"));
                links.Add(new HyperLink("last", BuildPageUrl(totalPages), "GET"));
            }

            return TypedResults.Ok(new AtendimentoCollectionResponse(
                atendimentos,
                new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                links));
        }).RequireAuthorization("ValidarRequisitosPerfil");

        app.MapGet("/atendimento/guiche/{guicheAtual:int}", async Task<IResult> (
            int guicheAtual,
            int? page,
            int? pageSize,
            CrudContext dbContext,
            HttpContext httpContext) =>
        {
            if (guicheAtual <= 0)
            {
                return Results.BadRequest(new { mensagem = "O guichê informado é inválido." });
            }

            var pageNumber = page is null or < 1 ? 1 : page.Value;
            var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;
            var query = dbContext.Atendimentos.Where(a => a.GuicheAtual == guicheAtual);

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

            var atendimentos = await query
                .OrderBy(a => a.NumeroAtual)
                .ThenBy(a => a.Prefixo)
                .Skip((pageNumber - 1) * requestedPageSize)
                .Take(requestedPageSize)
                .ToListAsync();

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var collectionUrl = $"{baseUrl}/atendimento/guiche/{guicheAtual}";
            var links = new List<HyperLink>
            {
                new("self", $"{collectionUrl}?page={pageNumber}&pageSize={requestedPageSize}", "GET"),
                new("collection", collectionUrl, "GET"),
                new("create", $"{baseUrl}/atendimento/criar", "POST")
            };

            if (pageNumber > 1)
            {
                links.Add(new HyperLink("prev", $"{collectionUrl}?page={pageNumber - 1}&pageSize={requestedPageSize}", "GET"));
            }

            if (pageNumber < totalPages)
            {
                links.Add(new HyperLink("next", $"{collectionUrl}?page={pageNumber + 1}&pageSize={requestedPageSize}", "GET"));
            }

            if (totalPages > 0)
            {
                links.Add(new HyperLink("first", $"{collectionUrl}?page=1&pageSize={requestedPageSize}", "GET"));
                links.Add(new HyperLink("last", $"{collectionUrl}?page={totalPages}&pageSize={requestedPageSize}", "GET"));
            }

            return TypedResults.Ok(new AtendimentoCollectionResponse(
                atendimentos,
                new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                links));
        }).RequireAuthorization("ValidarRequisitosPerfil");

        app.MapPost("/atendimento/criar", async (CriarAtendimentoDto novoAtendimento, CrudContext dbContext, IHubContext<PainelHub> hubContext) =>
        {
            if (string.IsNullOrWhiteSpace(novoAtendimento.Prefixo) || novoAtendimento.NumeroAtual <= 0 || novoAtendimento.GuicheAtual <= 0)
            {
                return Results.BadRequest(new { mensagem = "Dados do chamado inválidos ou incompletos." });
            }

            var atendimentoExistente = await dbContext.Atendimentos
                .FirstOrDefaultAsync(a => a.Prefixo == novoAtendimento.Prefixo
                    && a.NumeroAtual == novoAtendimento.NumeroAtual
                    && a.GuicheAtual == novoAtendimento.GuicheAtual);

            if (atendimentoExistente is not null)
            {
                atendimentoExistente.Prioridade = novoAtendimento.Prioridade;
                atendimentoExistente.Status = string.IsNullOrWhiteSpace(novoAtendimento.Status) ? "pendente" : novoAtendimento.Status;
                atendimentoExistente.NomeCliente = novoAtendimento.NomeCliente;
                atendimentoExistente.Observacao = novoAtendimento.Observacao;
                atendimentoExistente.DataInicioAtendimento ??= DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                await hubContext.Clients.All.SendAsync("NovaChamada", atendimentoExistente);

                return Results.Ok(new { mensagem = "Chamado atualizado com sucesso.", atendimento = atendimentoExistente });
            }

            var atendimento = new AtendimentoModel(
                novoAtendimento.Prefixo,
                novoAtendimento.NumeroAtual,
                novoAtendimento.GuicheAtual,
                novoAtendimento.Prioridade,
                string.IsNullOrWhiteSpace(novoAtendimento.Status) ? "pendente" : novoAtendimento.Status,
                novoAtendimento.NomeCliente,
                DateTime.UtcNow,
                null,
                novoAtendimento.Observacao);

            dbContext.Atendimentos.Add(atendimento);
            await dbContext.SaveChangesAsync();

            // await hubContext.Clients.All.SendAsync("NovaChamada", atendimento);

            return Results.Ok(new { mensagem = "Chamado criado com sucesso.", atendimento });
        }).RequireAuthorization("ValidarRequisitosPerfil");

        app.MapPost("/atendimento/chamar", async (ChamadaSenhaDto novaSenha, IHubContext<PainelHub> hubContext) =>
        {
            if (string.IsNullOrEmpty(novaSenha.Prefixo) || novaSenha.NumeroAtual <= 0 || novaSenha.GuicheAtual <= 0)
            {
                return Results.BadRequest(new { mensagem = "Dados da senha inválidos ou incompletos." });
            }

            await hubContext.Clients.All.SendAsync("NovaChamada", novaSenha);

            // return Results.Ok(new { mensagem = "Senha enviada ao painel com sucesso via SignalR!" });
            return Results.Ok();
        }).RequireAuthorization("ValidarRequisitosPerfil");


        app.MapPost("/atendimento/atender", async (StatusAtendimentoDto novoStatus, IHubContext<PainelHub> hubContext) =>
        {
            if (string.IsNullOrEmpty(novoStatus.Prefixo) || novoStatus.NumeroAtual <= 0 || novoStatus.GuicheAtual <= 0)
            {
                return Results.BadRequest(new { mensagem = "Dados do status de atendimento inválidos ou incompletos." });
            }

            await hubContext.Clients.All.SendAsync("StatusAtendimento", novoStatus);

            return Results.Ok(new { mensagem = "Status do atendimento enviado ao painel com sucesso via SignalR!" });
        }).RequireAuthorization("ValidarRequisitosPerfil");


        app.MapPost("/atendimento/iniciar", async (ChamadaSenhaDto novaSenha, CrudContext dbContext, IHubContext<PainelHub> hubContext) =>
        {
            if (string.IsNullOrEmpty(novaSenha.Prefixo) || novaSenha.NumeroAtual <= 0 || novaSenha.GuicheAtual <= 0)
            {
                return Results.BadRequest(new { mensagem = "Dados do atendimento inválidos ou incompletos." });
            }

            var atendimentoExistente = await dbContext.Atendimentos
                .FirstOrDefaultAsync(a => a.Prefixo == novaSenha.Prefixo
                    && a.NumeroAtual == novaSenha.NumeroAtual
                    && a.GuicheAtual == novaSenha.GuicheAtual);

            if (atendimentoExistente is null)
            {
                return Results.NotFound(new { mensagem = "Chamado não encontrado para iniciar atendimento." });
            }

            atendimentoExistente.Status = string.IsNullOrWhiteSpace(novaSenha.Status) ? "em-atendimento" : novaSenha.Status;
            atendimentoExistente.DataInicioAtendimento = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            var statusAtendimento = new StatusAtendimentoDto
            {
                Prefixo = atendimentoExistente.Prefixo,
                NumeroAtual = atendimentoExistente.NumeroAtual,
                GuicheAtual = atendimentoExistente.GuicheAtual,
                Status = atendimentoExistente.Status
            };

            await hubContext.Clients.All.SendAsync("StatusAtendimento", statusAtendimento);

            return Results.Ok(new { mensagem = "Atendimento atualizado com sucesso.", atendimento = atendimentoExistente });
        }).RequireAuthorization("ValidarRequisitosPerfil");


        app.MapPost("/atendimento/finalizar", async (FinalizarAtendimentoDto finalizacao, CrudContext dbContext, IHubContext<PainelHub> hubContext) =>
        {
            if (string.IsNullOrEmpty(finalizacao.Prefixo) || finalizacao.NumeroAtual <= 0 || finalizacao.GuicheAtual <= 0)
            {
                return Results.BadRequest(new { mensagem = "Dados da finalização inválidos ou incompletos." });
            }

            var atendimento = await dbContext.Atendimentos
                .FirstOrDefaultAsync(a => a.Prefixo == finalizacao.Prefixo
                    && a.NumeroAtual == finalizacao.NumeroAtual
                    && a.GuicheAtual == finalizacao.GuicheAtual);

            if (atendimento is null)
            {
                return Results.NotFound(new { mensagem = "Atendimento não encontrado para finalização." });
            }

            atendimento.Status = string.IsNullOrWhiteSpace(finalizacao.Status) ? "finalizado" : finalizacao.Status;
            atendimento.Observacao = finalizacao.Observacao;
            atendimento.DataFinalizacaoAtendimento = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            await hubContext.Clients.All.SendAsync("StatusAtendimento", atendimento);

            return Results.Ok(new { mensagem = "Atendimento finalizado com sucesso.", atendimento });
        }).RequireAuthorization("ValidarRequisitosPerfil");

        app.MapPost("/atendimento/cancelar", async (CancelarAtendimentoDto cancelamento, CrudContext dbContext, IHubContext<PainelHub> hubContext) =>
        {
            if (string.IsNullOrEmpty(cancelamento.Prefixo) || cancelamento.NumeroAtual <= 0 || cancelamento.GuicheAtual <= 0)
            {
                return Results.BadRequest(new { mensagem = "Dados do cancelamento inválidos ou incompletos." });
            }

            var atendimento = await dbContext.Atendimentos
                .FirstOrDefaultAsync(a => a.Prefixo == cancelamento.Prefixo
                    && a.NumeroAtual == cancelamento.NumeroAtual
                    && a.GuicheAtual == cancelamento.GuicheAtual);

            var agora = DateTime.UtcNow;

            if (atendimento is null)
            {
                var novoAtendimento = new AtendimentoModel(
                    cancelamento.Prefixo,
                    cancelamento.NumeroAtual,
                    cancelamento.GuicheAtual,
                    "Normal",
                    "cancelado",
                    string.Empty,
                    agora,
                    agora,
                    cancelamento.Observacao);

                dbContext.Atendimentos.Add(novoAtendimento);
                await dbContext.SaveChangesAsync();

                await hubContext.Clients.All.SendAsync("StatusAtendimento", novoAtendimento);

                return Results.Ok(new { mensagem = "Chamado cancelado com sucesso.", atendimento = novoAtendimento });
            }

            atendimento.Status = "cancelado";
            atendimento.Observacao = cancelamento.Observacao;
            atendimento.DataInicioAtendimento ??= agora;
            atendimento.DataFinalizacaoAtendimento = agora;

            await dbContext.SaveChangesAsync();

            await hubContext.Clients.All.SendAsync("StatusAtendimento", atendimento);

            return Results.Ok(new { mensagem = "Chamado cancelado com sucesso.", atendimento });
        }).RequireAuthorization("ValidarRequisitosPerfil");
    }
} 