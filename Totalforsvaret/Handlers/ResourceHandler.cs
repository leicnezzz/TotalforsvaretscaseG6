using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Handlers
{
    public class ResourceHandler
    {
        public Resource CreateResource(int resourceId, string resourceName, int categoryId, string categoryName, bool available, decimal latitude, decimal longitude)
        {
            var category = new Category
            {
                CategoryId = categoryId,
                Name = categoryName,
                Resources = new List<Resource>()
            };

            var resource = new Resource
            {
                ResourceId = resourceId,
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