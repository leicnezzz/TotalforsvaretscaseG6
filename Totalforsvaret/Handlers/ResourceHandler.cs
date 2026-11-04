using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Handlers
{
    public class ResourceHandler
    {
        private int nextResourceId = 0;

        private readonly List<Category> categories = new()
        {
            new Category { CategoryId = 1, Name = "Vehicles" },
            new Category { CategoryId = 2, Name = "Equipment" }
        };

        // Returnerer kopier av kategoriene til skjemaet.
        public IReadOnlyList<Category> GetCategories()
        {
            lock (categories)
            {
                return categories.Select(c => new Category
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name
                }).ToList();
            }
        }

        // Oppretter en ressurs knyttet til brukeren som registrerer den.
        public Resource CreateResource(
            string resourceName, int categoryId, bool available, decimal latitude,
            decimal longitude, int brukerId, string navn = "", string kontaktpunkt = "", DateTime? tilgjengeligFra = null)
        {
            if (brukerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(brukerId));
            }

            lock (categories)
            {
                var category = categories.Find(
                    c => c.CategoryId == categoryId);

                if (category == null)
                {
                    throw new ArgumentException(
                        "Category does not exist.",
                        nameof(categoryId));
                }

                nextResourceId++;

                var resource = new Resource
                {
                    ResourceId = nextResourceId,
                    BrukerId = brukerId,
                    Object = resourceName,
                    CategoryId = category.CategoryId,
                    Category = category,
                    Available = available,
                    Latitude = latitude,
                    Longitude = longitude,
                    Navn = navn,
                    Kontaktpunkt = kontaktpunkt,
                    TilgjengeligFra = tilgjengeligFra
                };

                category.Resources.Add(resource);

                return resource;
            }
        }

        // Returnerer kun kartpunktene som tilhører denne brukeren.
        public IReadOnlyList<ResourceMapPoint> GetResourcePointsForUser(
            int brukerId)
        {
            if (brukerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(brukerId));
            }

            lock (categories)
            {
                return categories
                    .SelectMany(c => c.Resources)
                    .Where(r => r.BrukerId == brukerId)
                    .Select(r => new ResourceMapPoint(
                        r.ResourceId,
                        r.Object,
                        r.Latitude,
                        r.Longitude))
                    .ToList();
            }
        }
    }
}