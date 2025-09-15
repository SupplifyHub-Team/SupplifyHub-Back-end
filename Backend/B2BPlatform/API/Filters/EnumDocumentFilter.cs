using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Enum;
namespace API.Filters
{
    public class EnumDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var modelsAssembly = typeof(CategoryStatus).Assembly;

            // Get distinct enum types
            var enumTypes = modelsAssembly.GetTypes()
                .Where(t => t.IsEnum && t.IsPublic)
                .Distinct()
                .ToList();

            foreach (var type in enumTypes)
            {
                // Use full type name as unique key
                var schemaKey = type.FullName;

                // Skip if already exists in any form
                if (context.SchemaRepository.Schemas.ContainsKey(schemaKey) ||
                    context.SchemaRepository.Schemas.ContainsKey(type.Name))
                {
                    continue;
                }

                try
                {
                    var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                    context.SchemaRepository.AddDefinition(schemaKey, schema);
                }
                catch (Exception ex)
                {
                    // Log error if needed
                    Console.WriteLine($"Error generating schema for {type.Name}: {ex.Message}");
                }
            }
        }
    }
}