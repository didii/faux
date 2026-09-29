namespace FauxData.Tests;

public class FauxRuleBuilderTests
{
    private readonly FauxRuleBuilder _fauxRuleBuilder = new([]);

    [Fact]
    public void SimplePropertyIgnorer()
    {
        _fauxRuleBuilder.ForType<SimpleObject>().Ignore(obj => obj.Id);

        var rule = _fauxRuleBuilder.Rules[0] as FauxRuleIgnoreConfig;
        rule.Should().NotBeNull();
        rule.Selector(typeof(int), [SimpleObject.IdProp]).Should().BeTrue();
    }

    [Fact]
    public void SimplePropertySetter()
    {
        _fauxRuleBuilder.ForType<SimpleObject>().Set(obj => obj.Id, -5);

        var rule = _fauxRuleBuilder.Rules[0] as FauxRuleFixedValueConfig;
        rule.Should().NotBeNull();
        rule.Value.Should().Be(-5);
        rule.Selector(typeof(int), [SimpleObject.IdProp]).Should().BeTrue();
    }

    [Fact]
    public void NestedPropertySetter()
    {
        _fauxRuleBuilder.ForType<NestedObject>().Set(obj => obj.Child1.Id, -5);

        var rule = _fauxRuleBuilder.Rules[0] as FauxRuleFixedValueConfig;
        rule.Should().NotBeNull();
        rule.Value.Should().Be(-5);

        // Should not match any simple ID prop
        rule.Selector(typeof(int), [SimpleObject.IdProp]).Should().BeFalse();
        // Should match nested ID prop withing the first child
        rule.Selector(typeof(int), [SimpleObject.IdProp, NestedObject.Child1Prop]).Should().BeTrue();
    }

    [Fact]
    public void DoubleNestedPropertySetter()
    {
        _fauxRuleBuilder.ForType<InfiniteObject>().Set(obj => obj.Child.Child.Id, -5);

        var rule = _fauxRuleBuilder.Rules[0] as FauxRuleFixedValueConfig;
        rule.Should().NotBeNull();
        rule.Value.Should().Be(-5);

        rule.Selector(typeof(int), [InfiniteObject.IdProp]).Should().BeFalse();
        rule.Selector(typeof(int), [InfiniteObject.IdProp, InfiniteObject.ChildProp]).Should().BeFalse();
        rule.Selector(typeof(int), [InfiniteObject.IdProp, InfiniteObject.ChildProp, InfiniteObject.ChildProp])
            .Should()
            .BeTrue();
    }

    [Fact]
    public void MaxDepthSetter()
    {
        _fauxRuleBuilder.MaxDepth(55);

        var rule = _fauxRuleBuilder.Rules[0] as FauxRuleMaxDepthConfig;
        rule.Should().NotBeNull();
        rule.Depth.Should().Be(55);
    }
}
