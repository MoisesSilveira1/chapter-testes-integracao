using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chapter.WebApi;
using Chapter.WebApi.Contexts;
using Chapter.WebApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chapter.IntegrationTests;

/// <summary>
/// Sobe a Web API completa (Startup, rotas, controllers, repositórios, autenticação JWT e EF Core)
/// em memória com WebApplicationFactory. Somente o banco SQL Server é substituído por um banco EF InMemory.
/// </summary>
public class ChapterApiFactory : WebApplicationFactory<Program>
{
    private readonly string _banco = "chapter-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ChapterContext>();
            services.RemoveAll<DbContextOptions<ChapterContext>>();
            services.AddDbContext<ChapterContext>(o => o.UseInMemoryDatabase(_banco));
        });
    }

    public void Semear(Action<ChapterContext> acao)
    {
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ChapterContext>();
        acao(ctx);
        ctx.SaveChanges();
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var d in services.Where(s => s.ServiceType == typeof(T)).ToList())
            services.Remove(d);
    }
}

public class ChapterApiTests : IClassFixture<ChapterApiFactory>
{
    private readonly ChapterApiFactory _factory;
    private readonly HttpClient _client;

    public ChapterApiTests(ChapterApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> ObterTokenAsync()
    {
        _factory.Semear(ctx =>
        {
            if (!ctx.Usuarios.Any(u => u.Email == "teste@chapter.com"))
                ctx.Usuarios.Add(new Usuario { Email = "teste@chapter.com", Senha = "123456" });
        });
        var resp = await _client.PostAsJsonAsync("/api/login", new Usuario { Email = "teste@chapter.com", Senha = "123456" });
        resp.EnsureSuccessStatusCode();
        var corpo = await resp.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(corpo);
        return json.RootElement.GetProperty("token").GetString();
    }

    // IT01 - Cadastro e listagem (POST + GET)
    [Fact]
    public async Task IT01_PostLivro_Retorna201_EGetListaOLivroCadastrado()
    {
        var post = await _client.PostAsJsonAsync("/api/livros",
            new Livro { Titulo = "Dom Casmurro", QuantidadePaginas = 256, Disponivel = true });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var get = await _client.GetAsync("/api/livros");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var livros = await get.Content.ReadFromJsonAsync<List<Livro>>();
        Assert.Contains(livros, l => l.Titulo == "Dom Casmurro" && l.QuantidadePaginas == 256 && l.Disponivel);
    }

    // IT02 - Busca por id existente
    [Fact]
    public async Task IT02_GetLivroPorId_Existente_Retorna200ComOLivro()
    {
        await _client.PostAsJsonAsync("/api/livros", new Livro { Titulo = "O Cortiço", QuantidadePaginas = 300, Disponivel = false });
        var todos = await _client.GetFromJsonAsync<List<Livro>>("/api/livros");
        var criado = todos.First(l => l.Titulo == "O Cortiço");

        var resp = await _client.GetAsync($"/api/livros/{criado.Id}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var livro = await resp.Content.ReadFromJsonAsync<Livro>();
        Assert.Equal(criado.Id, livro.Id);
        Assert.Equal(300, livro.QuantidadePaginas);
    }

    // IT03 - Busca por id inexistente
    [Fact]
    public async Task IT03_GetLivroPorId_Inexistente_Retorna404()
    {
        var resp = await _client.GetAsync("/api/livros/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // IT04 - Atualização (PUT é anônimo)
    [Fact]
    public async Task IT04_PutLivro_Retorna204_EAlteracaoFicaPersistida()
    {
        await _client.PostAsJsonAsync("/api/livros", new Livro { Titulo = "Iracema", QuantidadePaginas = 100, Disponivel = true });
        var criado = (await _client.GetFromJsonAsync<List<Livro>>("/api/livros")).First(l => l.Titulo == "Iracema");

        var put = await _client.PutAsJsonAsync($"/api/livros/{criado.Id}",
            new Livro { Titulo = "Iracema (2ª ed.)", QuantidadePaginas = 120, Disponivel = false });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var atualizado = await _client.GetFromJsonAsync<Livro>($"/api/livros/{criado.Id}");
        Assert.Equal("Iracema (2ª ed.)", atualizado.Titulo);
        Assert.Equal(120, atualizado.QuantidadePaginas);
        Assert.False(atualizado.Disponivel);
    }

    // IT05 - DELETE exige autenticação
    [Fact]
    public async Task IT05_DeleteLivro_SemToken_Retorna401()
    {
        var resp = await _client.DeleteAsync("/api/livros/1");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // IT06 - Login inválido
    [Fact]
    public async Task IT06_Login_CredenciaisInvalidas_Retorna404()
    {
        var resp = await _client.PostAsJsonAsync("/api/login", new Usuario { Email = "naoexiste@chapter.com", Senha = "errada" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // IT07 - Login válido devolve um JWT
    [Fact]
    public async Task IT07_Login_CredenciaisValidas_Retorna200ComTokenJwt()
    {
        var token = await ObterTokenAsync();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }

    // IT08 - Fluxo completo: login -> cadastro -> exclusão autenticada -> consulta
    [Fact]
    public async Task IT08_FluxoCompleto_LoginCadastroExclusaoComToken_LivroDeixaDeExistir()
    {
        var token = await ObterTokenAsync();
        await _client.PostAsJsonAsync("/api/livros", new Livro { Titulo = "Memórias Póstumas", QuantidadePaginas = 200, Disponivel = true });
        var criado = (await _client.GetFromJsonAsync<List<Livro>>("/api/livros")).First(l => l.Titulo == "Memórias Póstumas");

        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/livros/{criado.Id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var del = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var depois = await _client.GetAsync($"/api/livros/{criado.Id}");
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
    }

    // IT09 - Token inválido é rejeitado
    [Fact]
    public async Task IT09_DeleteLivro_ComTokenInvalido_Retorna401()
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/livros/1");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token.invalido.aqui");
        var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
