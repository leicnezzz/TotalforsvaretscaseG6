using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Handlers
{
    public class ResourceHandler
    {
        private int nextResourceId = 0;

        private readonly List<Category> categories = new()
        {
            new Category { CategoryId = 1, Name = "Food" },
            new Category { CategoryId = 2, Name = "Vehicles" },
            new Category { CategoryId = 3, Name = "Equipment" }
        };

        public Resource CreateResource( string resourceName, int categoryId, bool available, decimal latitude, decimal longitude)
        {
            lock (categories)
            {
                var category = categories.Find(c => c.CategoryId == categoryId);

                if (category == null)
                {
                    throw new ArgumentException("Category does not exist.");
                }

                nextResourceId++;

                var resource = new Resource
                {
                    ResourceId = nextResourceId,
                    Object = resourceName,
                    CategoryId = category.CategoryId,
                    Category = category,
                    Available = available,
                    Latitude = latitude,
                    Longitude = longitude
                };

                category.Resources.Add(resource);

                return resource;
            }
        }
    }
}

// LEGG TIl I PROGRAM CS FOR OG BYGGE KATEGORI IMORGEN NÅR VI SAMMENSVEISER

// builder.Services.AddSingleton<Totalforsvaret.Handlers.ResourceHandler>();