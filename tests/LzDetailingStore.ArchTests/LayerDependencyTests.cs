using FluentAssertions;
using NetArchTest.Rules;

namespace LzDetailingStore.ArchTests;

/// <summary>
/// Verifica la regla de dependencias de Clean Architecture:
/// Api → Application → Domain ← Infrastructure, con SharedKernel transversal.
/// El Domain no puede conocer capas externas.
/// </summary>
public class LayerDependencyTests
{
    [Fact]
    public void Domain_NoDebeDependerDe_Application()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Domain)
            .ShouldNot()
            .HaveDependencyOn(ArchTestAssemblies.ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Domain no debe depender de Application.", result));
    }

    [Fact]
    public void Domain_NoDebeDependerDe_Infrastructure()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Domain)
            .ShouldNot()
            .HaveDependencyOn(ArchTestAssemblies.InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Domain no debe depender de Infrastructure.", result));
    }

    [Fact]
    public void Domain_NoDebeDependerDe_Api()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Domain)
            .ShouldNot()
            .HaveDependencyOn(ArchTestAssemblies.ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Domain no debe depender de Api.", result));
    }

    [Fact]
    public void Domain_NoDebeDependerDe_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Domain)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Domain debe permanecer libre de EF Core (persistencia).", result));
    }

    [Fact]
    public void Application_NoDebeDependerDe_Infrastructure()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Application)
            .ShouldNot()
            .HaveDependencyOn(ArchTestAssemblies.InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Application no debe depender de Infrastructure (depende de abstracciones).", result));
    }

    [Fact]
    public void Application_NoDebeDependerDe_Api()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Application)
            .ShouldNot()
            .HaveDependencyOn(ArchTestAssemblies.ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Application no debe depender de Api.", result));
    }

    [Fact]
    public void SharedKernel_NoDebeDependerDe_NingunaCapaSuperior()
    {
        var result = Types.InAssembly(ArchTestAssemblies.SharedKernel)
            .ShouldNot()
            .HaveDependencyOnAny(
                ArchTestAssemblies.DomainNamespace,
                ArchTestAssemblies.ApplicationNamespace,
                ArchTestAssemblies.InfrastructureNamespace,
                ArchTestAssemblies.ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("SharedKernel debe ser transversal y no depender de otras capas.", result));
    }

    private static string BuildMessage(string rule, TestResult result)
    {
        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);
        return $"{rule} Tipos que violan la regla: {offenders}";
    }
}
