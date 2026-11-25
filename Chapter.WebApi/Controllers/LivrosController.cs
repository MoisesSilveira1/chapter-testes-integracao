using Chapter.WebApi.Models;
using Chapter.WebApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chapter.WebApi.Controllers
{
    /// <summary>Endpoints referentes aos livros (api/livros).</summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LivrosController : ControllerBase
    {
        private readonly LivroRepository _livroRepository;

        public LivrosController(LivroRepository livroRepository)
        {
            _livroRepository = livroRepository;
        }

        // GET /api/livros
        [HttpGet]
        public IActionResult Listar()
        {
            return Ok(_livroRepository.Listar());
        }

        // POST /api/livros
        [HttpPost]
        public IActionResult Cadastrar(Livro livro)
        {
            _livroRepository.Cadastrar(livro);
            return StatusCode(201);
        }

        // GET /api/livros/{id}
        [HttpGet("{id}")]
        public IActionResult BuscarPorId(int id)
        {
            Livro livro = _livroRepository.BuscarPorId(id);
            if (livro == null)
            {
                return NotFound();
            }
            return Ok(livro);
        }

        // PUT /api/livros/{id}
        [AllowAnonymous]
        [HttpPut("{id}")]
        public IActionResult Atualizar(int id, Livro livro)
        {
            _livroRepository.Atualizar(id, livro);
            return StatusCode(204);
        }

        // DELETE /api/livros/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult Deletar(int id)
        {
            _livroRepository.Deletar(id);
            return StatusCode(204);
        }
    }
}
