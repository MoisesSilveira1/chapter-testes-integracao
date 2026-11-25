using Chapter.WebApi.Contexts;
using Chapter.WebApi.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;

namespace Chapter.WebApi
{
    public class Startup
    {
        // Chave com no minimo 32 bytes (256 bits), exigida pelo HS256 nas versoes atuais do IdentityModel.
        // (A chave original "chapter-chave-autenticacao" tem apenas 26 bytes e gerava erro 500 no login.)
        public const string ChaveJwt = "chapter-chave-autenticacao-com-no-minimo-32-bytes";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<ChapterContext, ChapterContext>();
            services.AddTransient<LivroRepository, LivroRepository>();
            services.AddTransient<UsuarioRepository, UsuarioRepository>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChapterApi", Version = "v1" });
            });

            services
               .AddAuthentication(options =>
               {
                   options.DefaultAuthenticateScheme = "JwtBearer";
                   options.DefaultChallengeScheme = "JwtBearer";
               })
               .AddJwtBearer("JwtBearer", options =>
               {
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidateLifetime = true,
                       IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(ChaveJwt)),
                       ClockSkew = TimeSpan.FromMinutes(30),
                       ValidIssuer = "chapter.webapi",
                       ValidAudience = "chapter.webapi"
                   };
               });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChapterApi v1"));
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
