namespace FauxData.Tests;

public class FauxModelTests
{
    private readonly Faux _sut = new();

    [Fact]
    public void ModelSimpleObject_ShouldFillAllProperties()
    {
        var obj = _sut.Model<SimpleObject>();
        obj.Should().NotBeNull();
        obj.Id.Should().BeGreaterThan(0);
        obj.Name.Should().NotBeNullOrWhiteSpace();
        obj.Value.Should().BeDefined();
        obj.Ids.Should().NotBeEmpty();
    }

    [Fact]
    public void ModelSimpleObject_IgnoreId()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Ignore(obj => obj.Id));
        obj.Should().NotBeNull();
        obj.Id.Should().Be(0);
        obj.Name.Should().NotBeNullOrWhiteSpace();
        obj.Value.Should().BeDefined();
        obj.Ids.Should().NotBeEmpty();
    }

    [Fact]
    public void ModelSimpleObject_IgnoreName()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Ignore(obj => obj.Name));
        obj.Should().NotBeNull();
        obj.Id.Should().BeGreaterThan(0);
        obj.Name.Should().BeNull();
        obj.Value.Should().BeDefined();
        obj.Ids.Should().NotBeEmpty();
    }

    [Fact]
    public void ModelSimpleObject_IgnoreValue()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Ignore(obj => obj.Value));
        obj.Should().NotBeNull();
        obj.Id.Should().BeGreaterThan(0);
        obj.Name.Should().NotBeNullOrWhiteSpace();
        obj.Value.Should().Be(0);
        obj.Ids.Should().NotBeEmpty();
    }

    [Fact]
    public void ModelSimpleObject_IgnoreIds()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Ignore(obj => obj.Ids));
        obj.Should().NotBeNull();
        obj.Id.Should().BeGreaterThan(0);
        obj.Name.Should().NotBeNullOrWhiteSpace();
        obj.Value.Should().BeDefined();
        obj.Ids.Should().BeNull();
    }

    [Fact]
    public void ModelSimpleObject_SetId()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Set(obj => obj.Id, -5));
        obj.Id.Should().Be(-5);
    }

    [Fact]
    public void ModelSimpleObject_SetName()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Set(obj => obj.Name, "My name"));
        obj.Name.Should().Be("My name");
    }

    [Fact]
    public void ModelSimpleObject_SetValue()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Set(obj => obj.Value, SomeEnum.Three));
        obj.Value.Should().Be(SomeEnum.Three);
    }

    [Fact]
    public void ModelSimpleObject_SetIds()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Set(obj => obj.Ids, [1, 2, 3]));
        obj.Ids.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void ModelSimpleObject_CountIds_Zero()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Count(obj => obj.Ids, 0));
        obj.Ids.Should().BeEmpty();
    }

    [Fact]
    public void ModelSimpleObject_CountIds_One()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Count(obj => obj.Ids, 1));
        obj.Ids.Should().HaveCount(1);
    }

    [Fact]
    public void ModelSimpleObject_AssignName()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Assign(obj => obj.Name, obj => obj.Id.ToString()));
        obj.Name.Should().NotBeEmpty().And.Be(obj.Id.ToString());
    }

    [Fact]
    public void ModelNestedObject_ShouldFillNestedProperties()
    {
        var obj = _sut.Model<NestedObject>();
        obj.Child1.Should().NotBeNull();
        obj.Child1.Name.Should().NotBeNullOrEmpty();
        obj.Child2.Should().NotBeNull();
        obj.Child2.Name.Should().NotBeNullOrEmpty();
        obj.Children.Should().NotBeEmpty();
        obj.Children.Should().AllSatisfy(child => child.Name.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void ModelNestedObject_SetChild1Name()
    {
        var obj = _sut.Model<NestedObject>(conf => conf.Set(x => x.Child1.Name, "Some name"));
        obj.Child1.Name.Should().Be("Some name");
        obj.Child2.Name.Should().NotBe("Some name");
    }

    [Fact]
    public void ModelNestedObject_ForChild1_SetName()
    {
        var obj = _sut.Model<NestedObject>(conf => conf.For(obj => obj.Child1).Set(x => x.Name, "Some name"));
        obj.Child1.Name.Should().Be("Some name");
        obj.Child2.Name.Should().NotBe("Some name");
    }

    [Fact]
    public void ModelNestedObject_ForTypeSimpleObject_SetName()
    {
        var obj = _sut.Model<NestedObject>(conf => conf.ForType<SimpleObject>().Set(x => x.Name, "Some name"));
        obj.Child1.Name.Should().Be("Some name");
        obj.Child2.Name.Should().Be("Some name");
    }

    [Fact]
    public void ModelNestedObject_ForChild1_AssignName()
    {
        var obj = _sut.Model<NestedObject>(conf =>
            conf.For(obj => obj.Child1).Assign(child => child.Name, obj => obj.Child2.Name)
        );
        obj.Child1.Name.Should().Be(obj.Child2.Name);
    }

    [Fact]
    public void ModelNestedObject_AssignNestedName()
    {
        var obj = _sut.Model<NestedObject>(conf => conf.Assign(x => x.Child1.Name, x => x.Code));
        obj.Child1.Name.Should().Be(obj.Code);
        obj.Child2.Name.Should().NotBe(obj.Code);
    }

    [Fact]
    public void ModelNestedObject_ForTypeAssignNestedName()
    {
        _sut.ForType<NestedObject>().Assign(x => x.Child1.Name, x => x.Code);
        var obj = _sut.Model<NestedObject>();
        obj.Child1.Name.Should().Be(obj.Code);
    }

    [Fact]
    public void ModelNestedObject_ForAllChildren_SetName()
    {
        var obj = _sut.Model<NestedObject>(conf =>
            conf.ForAll(obj => obj.Children).Set(child => child.Name, "Some name")
        );
        obj.Children.Should().AllSatisfy(child => child.Name.Should().Be("Some name"));
        obj.Child1.Name.Should().NotBe("Some name");
        obj.Child2.Name.Should().NotBe("Some name");
    }

    [Fact]
    public void ModelInfiniteObject_NestedFor_SetId()
    {
        var obj = _sut.Model<InfiniteObject>(conf =>
            conf.Set(obj => obj.Id, -1)
                .For(obj => obj.Child)
                .Set(obj => obj.Id, -2)
                .For(obj => obj.Child)
                .Set(obj => obj.Id, -3)
        );
        obj.Id.Should().Be(-1);
        obj.Child.Id.Should().Be(-2);
        obj.Child.Child.Id.Should().Be(-3);
        obj.Child.Child.Child.Id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// A local config (i.e. specified in .Model) should always override the same config for a property that was
    /// configured on the instance.
    /// </summary>
    [Fact]
    public void ModelSimpleObject_LocalConfigOverridesInstanceConfig()
    {
        _sut.ForType<SimpleObject>().Set(obj => obj.Name, "First name");
        var obj = _sut.Model<SimpleObject>(conf => conf.Ignore(obj => obj.Name));

        // Ignore wins here because it's specified last
        obj.Name.Should().BeNull();
    }

    /// <summary>
    /// When two local configs conflict, the last one specified should win.
    /// </summary>
    [Fact]
    public void ModelSimpleObject_LastLocalConfigOverridesPreviousLocalConfig()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Set(obj => obj.Name, "First name").Ignore(obj => obj.Name));

        // Ignore wins here because it's specified last
        obj.Name.Should().BeNull();
    }

    [Fact]
    public void ModelSimpleObject_MultipleConfigs()
    {
        var obj = _sut.Model<SimpleObject>(conf =>
            conf.Set(obj => obj.Id, 4).Set(obj => obj.Name, "Some name").Set(obj => obj.Value, SomeEnum.Three)
        );

        obj.Id.Should().Be(4);
        obj.Name.Should().Be("Some name");
        obj.Value.Should().Be(SomeEnum.Three);
    }

    /// <summary>
    /// Local configs on one model should nog bleed over to another
    /// </summary>
    [Fact]
    public void ModelSimpleObject_LocalConfigStaysLocal()
    {
        var obj1 = _sut.Model<SimpleObject>(conf => conf.Set(obj => obj.Id, -2).Set(obj => obj.Name, "First name"));
        var obj2 = _sut.Model<SimpleObject>(conf => conf.Set(obj => obj.Name, "Second name"));

        obj1.Id.Should().Be(-2);
        obj1.Name.Should().Be("First name");
        obj2.Id.Should().NotBe(-2);
        obj2.Name.Should().Be("Second name");
    }

    [Fact]
    public void ModelOrganisation_ShouldSucceed()
    {
        _sut.ForType<Organisation>()
            .ForAll(org => org.Departments)
            .Assign(dep => dep.CollectionPoint, org => org.CollectionPoints[0]);

        var organisation = _sut.Model<Organisation>();

        var firstCollectionPoint = organisation.CollectionPoints[0];
        organisation.Departments.Should().AllSatisfy(dep => dep.CollectionPoint.Should().Be(firstCollectionPoint));
    }
}
