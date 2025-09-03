using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Demo.Context;

public class InMemoryContext(DbContextOptions<InMemoryContext> options) : DbContext(options)
{
   public DbSet<OutboxMessage> OutboxMessages { get; set; }

   protected override void OnModelCreating(ModelBuilder b) =>
      b.Entity<OutboxMessage>()
       .ToTable("outbox_messages")
       .HasKey(x => x.Id);
}

public class OutboxMessage
{
   public long Id { get; set; }
}

public static class SqlLiteInMemoryConfigurationHelper
{
   public static WebApplicationBuilder UseSqlLiteInMemory(this WebApplicationBuilder builder)
   {
      builder.Services.AddSingleton(_ =>
      {
         // Keep the in-memory DB alive for the app lifetime
         var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
         conn.Open();
         return conn;
      });

      builder.Services.AddDbContext<InMemoryContext>((sp, opt) =>
      {
         var conn = sp.GetRequiredService<SqliteConnection>();
         opt.UseSqlite(conn); // <- in-memory SQLite
         opt.EnableSensitiveDataLogging(); // for parameters in logs (dev)
         opt.EnableDetailedErrors();
      });

      return builder;
   }

   public static WebApplication CreateInMemoryDb(this WebApplication app)
   {
      using var scope = app.Services.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<InMemoryContext>();
      context.Database.EnsureCreated();
      return app;
   }
}