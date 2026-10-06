using System.Reflection;
using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Behaviors;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Infrastructure.Persistence;
using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.ArchTests;

/// <summary>
/// Anclas de assembly y namespaces raíz de cada capa, usados por todas las reglas
/// de arquitectura. Centraliza los tipos de referencia para que un cambio de nombre
/// de assembly se refleje en un solo lugar.
/// </summary>
public static class ArchTestAssemblies
{
    public static Assembly Domain => typeof(Product).Assembly;
    public static Assembly Application => typeof(ValidationBehavior<,>).Assembly;
    public static Assembly Infrastructure => typeof(AppDbContext).Assembly;
    public static Assembly Api => typeof(EndpointExtensions).Assembly;
    public static Assembly SharedKernel => typeof(Result).Assembly;

    public const string DomainNamespace = "LzDetailingStore.Domain";
    public const string ApplicationNamespace = "LzDetailingStore.Application";
    public const string InfrastructureNamespace = "LzDetailingStore.Infrastructure";
    public const string ApiNamespace = "LzDetailingStore.Api";
    public const string SharedKernelNamespace = "LzDetailingStore.SharedKernel";
}
