// See https://aka.ms/new-console-template for more information

// See https://aka.ms/new-console-template for more information

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Internal;

namespace MigrationError
{
    // Entity class
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public DateOnly DateOnly { get; set; }
    }

    // DbContext class
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Person> People { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Start SQL Server container
            var msSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Database@1") // SQL Server requires a strong password
                .Build();

            await msSqlContainer.StartAsync();

            msSqlContainer.GetConnectionString();

            DbContextOptionsBuilder<AppDbContext> optionBuilder = new();
            optionBuilder
                .UseSqlServer(msSqlContainer.GetConnectionString());


            // Initialize DbContext and perform migrations
            using (var dbContext = new AppDbContext(optionBuilder.Options))
            {
                Console.WriteLine("Migrating database...");
                await dbContext.Database.MigrateAsync(); // This will apply any migrations to the database

                // Insert a record
                var person = new Person { Name = "John Doe", DateOnly = DateOnly.Parse(DateTime.Now.ToShortDateString())};
                dbContext.People.Add(person);
                await dbContext.SaveChangesAsync();

                // Retrieve the record
                var retrievedPerson = dbContext.People.FirstOrDefault(p => p.Id == person.Id);
                Console.WriteLine($"Retrieved Person: {retrievedPerson.Name} - {retrievedPerson.DateOnly}");
            }

            // Stop the container once done
            await msSqlContainer.StopAsync();
        }
    }

    // Migration configuration
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer("");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}