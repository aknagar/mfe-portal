using AugmentService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AugmentService.Infrastructure.ProductData
{
    public class ProductDataContext : DbContext
    {
        private readonly IOptions<InfrastructureConfig> _config;

        public ProductDataContext(DbContextOptions<ProductDataContext> options, IOptions<InfrastructureConfig> config)
            : base(options)
        {
            _config = config;
        }

        public DbSet<Product> Product { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseNpgsql(_config.Value.ConnectionString)
                    .EnableSensitiveDataLogging(_config.Value.EnableSensitiveDataLogging);
            }
        }
    }

    public static class Extensions
    {
        public static void CreateProductDbIfNotExists(this IHost host)
        {
            using var scope = host.Services.CreateScope();

            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ProductDataContext>();
            context.Database.EnsureCreated();
            DbInitializer.Initialize(context);
        }
    }


    public static class DbInitializer
    {
        public static void Initialize(ProductDataContext context)
        {
            if (context.Product.Any())
                return;

            var products = new List<Product>
        {
            new Product { Name = "Solar Powered Flashlight", Description = "A fantastic product for outdoor enthusiasts", Price = 19.99m, ImageUrl = "product1.png" },
            new Product { Name = "Hiking Poles", Description = "Ideal for camping and hiking trips", Price = 24.99m, ImageUrl = "product2.png" },
            new Product { Name = "Outdoor Rain Jacket", Description = "This product will keep you warm and dry in all weathers", Price = 49.99m, ImageUrl = "product3.png" },
            new Product { Name = "Survival Kit", Description = "A must-have for any outdoor adventurer", Price = 99.99m, ImageUrl = "product4.png" },
            new Product { Name = "Outdoor Backpack", Description = "This backpack is perfect for carrying all your outdoor essentials", Price = 39.99m, ImageUrl = "product5.png" },
            new Product { Name = "Camping Cookware", Description = "This cookware set is ideal for cooking outdoors", Price = 29.99m, ImageUrl = "product6.png" },
            new Product { Name = "Camping Stove", Description = "This stove is perfect for cooking outdoors", Price = 49.99m, ImageUrl = "product7.png" },
            new Product { Name = "Camping Lantern", Description = "This lantern is perfect for lighting up your campsite", Price = 19.99m, ImageUrl = "product8.png" },
            new Product {  Name = "Camping Tent", Description = "This tent is perfect for camping trips", Price = 99.99m, ImageUrl = "product9.png" },
        };

            context.AddRange(products);

            context.SaveChanges();
        }
    }

}

