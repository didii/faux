using FluentAssertions.Execution;

namespace FauxData.Tests;

public class FauxPasTests
{
    private readonly Faux _sut = new();

    [Fact]
    public void PasSimpleObject_ReturnsEmptyValues()
    {
        var result = _sut.Pas<SimpleObject>();

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Id.Should().Be(default);
        result.Value.Should().Be(SomeEnum.One);
        result.Ids.Should().BeEmpty();
        result.Name.Should().Be(default);
    }

    [Fact]
    public void PasNestedObject_ReturnsNestedEmptyObjects()
    {
        var result = _sut.Pas<NestedObject>();

        result.Should().NotBeNull();
        using var scope = new AssertionScope();
        result.Id.Should().Be(default);
        result.Child1.Should().NotBeNull();
        result.Child1.Id.Should().Be(default);
        result.Child1.Value.Should().Be(SomeEnum.One);
        result.Child1.Ids.Should().BeEmpty();
        result.Child1.Name.Should().Be(default);
        result.Child2.Should().NotBeNull();
        result.Child2.Id.Should().Be(default);
        result.Child2.Value.Should().Be(SomeEnum.One);
        result.Child2.Ids.Should().BeEmpty();
        result.Child2.Name.Should().Be(default);
        result.Code.Should().Be(default);
    }

    [Fact]
    public void PasNoEmptyConstructorObject_ReturnsEmptyObject()
    {
        var result = _sut.Pas<NoEmptyConstructorObject>();
        result.Should().NotBeNull();

        using var scope = new AssertionScope();
        result.Id.Should().Be(default);
        result.Name.Should().Be(default);
        result.Child.Should().NotBeNull();
        result.Child.Id.Should().Be(default);
    }
}
