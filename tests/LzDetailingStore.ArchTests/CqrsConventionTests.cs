using FluentAssertions;
using MediatR;
using NetArchTest.Rules;

namespace LzDetailingStore.ArchTests;

/// <summary>
/// Verifica las convenciones CQRS reales del proyecto: los casos de uso viven en
/// Application.Features, los handlers son sealed y los commands/queries implementan
/// el contrato de MediatR.
/// </summary>
public class CqrsConventionTests
{
    private const string FeaturesNamespace = "LzDetailingStore.Application.Features";

    [Fact]
    public void Handlers_DebenSer_Sealed()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Application)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Todo Handler debe ser sealed (patrón del proyecto).", result));
    }

    [Fact]
    public void Handlers_DebenResidirEn_Features()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Application)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .ResideInNamespaceStartingWith(FeaturesNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Los Handlers deben vivir dentro de Application.Features (vertical slices).", result));
    }

    [Fact]
    public void Commands_DebenImplementar_IRequest()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Application)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(IBaseRequest))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Todo Command debe implementar IRequest<T> (contrato MediatR).", result));
    }

    [Fact]
    public void Queries_DebenImplementar_IRequest()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Application)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(IBaseRequest))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Toda Query debe implementar IRequest<T> (contrato MediatR).", result));
    }

    [Fact]
    public void Validators_DebenResidirEn_Features()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Application)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .ResideInNamespaceStartingWith(FeaturesNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Los Validators deben vivir junto a su caso de uso en Application.Features.", result));
    }

    private static string BuildMessage(string rule, TestResult result)
    {
        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);
        return $"{rule} Tipos que violan la regla: {offenders}";
    }
}
