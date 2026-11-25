using Chapter.WebApi.Contexts;
using Chapter.WebApi.Models;
using System.Linq;

namespace Chapter.WebApi.Repositories
{
    public class UsuarioRepository
    {
        private readonly ChapterContext _context;

        public UsuarioRepository(ChapterContext context)
        {
            _context = context;
        }

        /// <summary>Valida o usuário pelo e-mail e senha.</summary>
        public Usuario Login(string email, string senha)
        {
            return _context.Usuarios.FirstOrDefault(u => u.Email == email && u.Senha == senha);
        }
    }
}
