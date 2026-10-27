using FluentAssertions;
using LzDetailingStore.SharedKernel;
using NetArchTest.Rules;

namespace LzDetailingStore.ArchTests;

/// <summary>
/// Verifica convenciones del dominio: las entidades viven bajo Domain.Entities.{Modulo},
/// son sealed, y la mayoría hereda de BaseEntity. Las entidades de unión con clave
/// compuesta (UserRole, RolePermission) están excluidas de la regla de BaseEntity
/// porque por diseño no la heredan.
/// </summary>
public class DomainConventionTests
{
    private const string EntitiesNamespace = "LzDetailingStore.Domain.Entities";

    [Fact]
    public void Entidades_DebenSer_Sealed()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Domain)
            .That()
            .ResideInNamespaceStartingWith(EntitiesNamespace)
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Las entidades de dominio deben ser sealed.", result));
    }

    [Fact]
    public void Entidades_ConHerencia_DebenExtenderBaseEntity()
    {
        // Excluye las entidades de unión (join tables) que por diseño no heredan BaseEntity.
        var result = Types.InAssembly(ArchTestAssemblies.Domain)
            .That()
            .ResideInNamespaceStartingWith(EntitiesNamespace)
            .And()
            .AreClasses()
            .And()
            .DoNotHaveName("UserRole")
            .And()
            .DoNotHaveName("RolePermission")
            .Should()
            .Inherit(typeof(BaseEntity))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("Las entidades (salvo las de unión con clave compuesta) deben heredar de BaseEntity.", result));
    }

    [Fact]
    public void Domain_NoDebeDependerDe_FluentValidation()
    {
        var result = Types.InAssembly(ArchTestAssemblies.Domain)
            .ShouldNot()
            .HaveDependencyOn("FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            BuildMessage("El dominio no debe conocer FluentValidation (validación es responsabilidad de Application).", result));
    }

    private static string BuildMessage(string rule, TestResult result)
    {
        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);
        return $"{rule} Tipos que violan la regla: {offenders}";
    }
}
