using System.Diagnostics;
using AugmentService.Core.Entities;
using AugmentService.Infrastructure.ProductData;
using Microsoft.EntityFrameworkCore;

namespace AugmentService.Api.Endpoints
{
    public static class ProductEndpoints
    {
        public const string ActivitySourceName = "AugmentService.Api.Products";
        private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

        public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/Product");

            // var secret = secretClient.GetSecret("AspireTestSecret");

            group.MapGet("/", async (ProductDataContext db) =>
            {
                using var activity = s_activitySource.StartActivity("GetAllProducts");
                var products = await db.Product.ToListAsync();
                activity?.SetTag("product.count", products.Count);
                return products;
            })
            .WithName("GetAllProducts")
            .Produces<List<Product>>(StatusCodes.Status200OK);

            group.MapGet("/{id}", async (int id, ProductDataContext db) =>
            {
                using var activity = s_activitySource.StartActivity("GetProductById");
                activity?.SetTag("product.id", id);

                var model = await db.Product.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (model is null)
                {
                    activity?.SetTag("product.found", false);
                    return Results.NotFound();
                }

                activity?.SetTag("product.found", true);
                return Results.Ok(model);
            })
            .WithName("GetProductById")
            .Produces<Product>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            group.MapPut("/{id}", async (int id, Product product, ProductDataContext db) =>
            {
                using var activity = s_activitySource.StartActivity("UpdateProduct");
                activity?.SetTag("product.id", id);

                var affected = await db.Product
                    .Where(model => model.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                      .SetProperty(m => m.Id, product.Id)
                      .SetProperty(m => m.Name, product.Name)
                      .SetProperty(m => m.Description, product.Description)
                      .SetProperty(m => m.Price, product.Price)
                      .SetProperty(m => m.ImageUrl, product.ImageUrl)
                    );

                activity?.SetTag("product.updated", affected == 1);
                return affected == 1 ? Results.Ok() : Results.NotFound();
            })
            .WithName("UpdateProduct")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent);

            group.MapPost("/", async (Product product, ProductDataContext db) =>
            {
                using var activity = s_activitySource.StartActivity("CreateProduct");
                db.Product.Add(product);
                await db.SaveChangesAsync();
                activity?.SetTag("product.id", product.Id);
                return Results.Created($"/api/Product/{product.Id}", product);
            })
            .WithName("CreateProduct")
            .Produces<Product>(StatusCodes.Status201Created);

            group.MapDelete("/{id}", async (int id, ProductDataContext db) =>
            {
                using var activity = s_activitySource.StartActivity("DeleteProduct");
                activity?.SetTag("product.id", id);

                var affected = await db.Product
                    .Where(model => model.Id == id)
                    .ExecuteDeleteAsync();

                activity?.SetTag("product.deleted", affected == 1);
                return affected == 1 ? Results.Ok() : Results.NotFound();
            })
            .WithName("DeleteProduct")
            .Produces<Product>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
        }
    }

}

