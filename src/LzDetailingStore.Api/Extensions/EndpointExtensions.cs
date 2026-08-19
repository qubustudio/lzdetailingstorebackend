using LzDetailingStore.Api.Endpoints;

namespace LzDetailingStore.Api.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroups = typeof(IEndpointGroup).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpointGroup)))
            .Select(Activator.CreateInstance)
            .Cast<IEndpointGroup>();

        foreach (var group in endpointGroups)
            group.MapEndpoints(app);

        return app;
    }
}
