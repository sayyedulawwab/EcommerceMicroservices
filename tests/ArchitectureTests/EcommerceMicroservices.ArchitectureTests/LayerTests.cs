using System.Reflection;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Architecture = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace EcommerceMicroservices.ArchitectureTests;

// Project references already stop Domain and Application from using the outer layers, and stop one
// service from referencing another. These tests catch the cases references allow: a reference added
// later, and controllers using Infrastructure types through the API project's reference to it.
public class LayerTests
{
    private static readonly string[] LayeredServices = ["Cart", "Catalog", "Identity", "Ordering"];

    private static readonly string[] AllServices = [.. LayeredServices, "Payment"];

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies([.. LayeredServices.SelectMany(LayerAssemblies), Load("Payment.Processor"), Load("SharedKernel")])
        .Build();

    public static TheoryData<string> Services => [.. LayeredServices];

    public static TheoryData<string> ServicesWithPayment => [.. AllServices];

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_Should_NotDependOnOtherLayers(string service)
    {
        Types().That().ResideInAssembly(Load($"{service}.Domain"))
            .Should().NotDependOnAny(Types().That().ResideInAssembly(
                Load($"{service}.Application"), Load($"{service}.Infrastructure"), Load($"{service}.API")))
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_Should_NotDependOnInfrastructureOrApi(string service)
    {
        Types().That().ResideInAssembly(Load($"{service}.Application"))
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Load($"{service}.Infrastructure"), Load($"{service}.API")))
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Infrastructure_Should_NotDependOnApi(string service)
    {
        Types().That().ResideInAssembly(Load($"{service}.Infrastructure"))
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Load($"{service}.API")))
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Controllers_Should_NotDependOnInfrastructure(string service)
    {
        Types().That().HaveFullNameStartingWith($"{service}.API.Controllers.")
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Load($"{service}.Infrastructure")))
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ServicesWithPayment))]
    public void Service_Should_NotDependOnOtherServices(string service)
    {
        Assembly[] others = [.. AllServices.Where(other => other != service).SelectMany(ServiceAssemblies)];

        Types().That().ResideInAssembly(ServiceAssemblies(service)[0], ServiceAssemblies(service)[1..])
            .Should().NotDependOnAny(Types().That().ResideInAssembly(others[0], others[1..]))
            .Check(Architecture);
    }

    [Fact]
    public void SharedKernel_Should_NotDependOnServices()
    {
        Assembly[] services = [.. AllServices.SelectMany(ServiceAssemblies)];

        Types().That().ResideInAssembly(Load("SharedKernel"))
            .Should().NotDependOnAny(Types().That().ResideInAssembly(services[0], services[1..]))
            .Check(Architecture);
    }

    private static Assembly[] ServiceAssemblies(string service)
    {
        return service == "Payment" ? [Load("Payment.Processor")] : LayerAssemblies(service);
    }

    private static Assembly[] LayerAssemblies(string service)
    {
        return [Load($"{service}.Domain"), Load($"{service}.Application"), Load($"{service}.Infrastructure"), Load($"{service}.API")];
    }

    private static Assembly Load(string name)
    {
        return Assembly.Load(name);
    }
}
