
using DataContextContainer.Models;

using Microsoft.EntityFrameworkCore;

namespace DataContextContainer
{
    public class UserContext : DbContext, IHaveSchemaName
    {
        private readonly ISchemaNameProvider schemaNameProvider;

        public string SchemaName { get; set; } = "public";

        public UserContext()
        {
        }

        public UserContext(DbContextOptions<UserContext> options, ISchemaNameProvider schemaNameProvider) : base(options)
        {
            this.schemaNameProvider = schemaNameProvider;
            this.SchemaName = this.schemaNameProvider.SchemaName;
            System.Console.WriteLine("Running on schema name" + this.SchemaName);
        }

        public virtual DbSet<Product> Products { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<Category> Categories { get; set; }

        public DbSet<Filter> Filters { get; set; }

        // public virtual DbSet<UserConfig> UserConfigs { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            optionsBuilder.UseNpgsql("Host=localhost;DataBase=shared;Username=postgres;Password=pass123");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(this.SchemaName);
            modelBuilder.Entity<Product>().ToTable(nameof(Products), r => r.ExcludeFromMigrations());
            //modelBuilder.Entity<User>().ToTable("Users", "public");
            //modelBuilder.Entity<UserConfig>().ToTable("UserConfigs", "public");

            modelBuilder.Entity<UserProduct>().HasKey(r => new { r.UserId, r.ProductId });
            modelBuilder.Entity<UserProduct>().HasOne(r => r.Product).WithOne().HasForeignKey<UserProduct>(r => r.UserId);
            modelBuilder.Entity<UserProduct>().HasOne(r => r.User).WithOne().HasForeignKey<UserProduct>(r => r.UserId);


            base.OnModelCreating(modelBuilder);
        }
    }


    public interface IHaveSchemaName
    {
        string SchemaName { get; set; }
    }
}
