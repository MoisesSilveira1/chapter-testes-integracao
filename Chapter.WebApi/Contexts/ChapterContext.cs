using Chapter.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Chapter.WebApi.Contexts
{
    // DbContext: ponte entre o modelo de classes e o banco de dados
    public class ChapterContext : DbContext
    {
        public ChapterContext()
        {
        }

        public ChapterContext(DbContextOptions<ChapterContext> options)
          : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=DESKTOP-KHGJH1M\\SQLEXPRESS; initial catalog=Chapter; Integrated Security=true; TrustServerCertificate=true");
            }
        }

        public virtual DbSet<Livro> Livros { get; set; }
        public virtual DbSet<Usuario> Usuarios { get; set; }
    }
}
