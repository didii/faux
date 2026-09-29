namespace FauxData.Tests;

public class FauxSimpleTests
{
    private const int RepeatCount = 100;
    private Faux _sut = new();

    [Fact]
    public void RandomBoolean()
    {
        Repeat(() => _sut.Model<bool>());
        // Nothing to test except that it doesn't throw
    }

    [Fact]
    public void RandomInt()
    {
        RepeatWithoutCollision(() =>
        {
            var data = _sut.Model<int>();
            data.Should().NotBe(0);
            return data;
        });
    }

    [Fact]
    public void RandomFloat()
    {
        RepeatWithoutCollision(() =>
        {
            var data = _sut.Model<float>();
            data.Should().NotBe(0f);
            return data;
        });
    }

    [Fact]
    public void RandomDouble()
    {
        RepeatWithoutCollision(() =>
        {
            var data = _sut.Model<double>();
            data.Should().NotBe(0d);
            return data;
        });
    }

    [Fact]
    public void RandomGuid()
    {
        RepeatWithoutCollision(() =>
        {
            var data = _sut.Model<Guid>();
            data.Should().NotBe(Guid.Empty);
            return data;
        });
    }

    [Fact]
    public void RandomDateTime()
    {
        RepeatWithoutCollision(() =>
        {
            var data = _sut.Model<DateTime>();
            data.Should().NotBe(DateTime.MinValue);
            return data;
        });
    }

    [Fact]
    public void RandomDateOnly_WithinTwoYearsOfToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Repeat(() =>
        {
            var data = _sut.Model<DateOnly>();
            data.Should().BeOnOrAfter(today.AddYears(-2)).And.BeOnOrBefore(today.AddYears(2));
        });
    }

    [Fact]
    public void RandomString()
    {
        RepeatWithoutCollision(() =>
        {
            var data = _sut.Model<string>();
            data.Should().NotBeNullOrEmpty();
            return data;
        });
    }

    [Fact]
    public void RandomEnum()
    {
        Repeat(() =>
        {
            var randomEnum = _sut.Model<SomeEnum>();
            randomEnum.Should().BeOneOf(SomeEnum.One, SomeEnum.Two, SomeEnum.Three);
        });
    }

    [Fact]
    public void RandomArray()
    {
        Repeat(() =>
        {
            var randomArray = _sut.Model<int[]>();
            randomArray.Should().NotBeNull();
            randomArray.Should().NotBeEmpty();
            randomArray.Should().AllSatisfy(item => item.Should().NotBe(0));
        });
    }

    [Fact]
    public void RandomList()
    {
        Repeat(() =>
        {
            var randomList = _sut.Model<List<int>>();
            randomList.Should().NotBeNull();
            randomList.Should().NotBeEmpty();
            randomList.Should().AllSatisfy(item => item.Should().NotBe(0));
        });
    }

    [Fact]
    public void RandomSimpleObject()
    {
        Repeat(() =>
        {
            var obj = _sut.Model<SimpleObject>();
            obj.Should().NotBeNull();
            obj.Id.Should().NotBe(0);
            obj.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void RandomComplexObject()
    {
        Repeat(() =>
        {
            var obj = _sut.Model<NestedObject>();
            obj.Should().NotBeNull();
            obj.Child2.Should().NotBeNull();
        });
    }

    [Fact]
    public void RandomInfiniteObject_DoesNotThrow()
    {
        _sut.Model<InfiniteObject>();
        // Assertion is that the function can run without throwing a stack overflow exception
    }

    [Fact]
    public void RandomInfiniteObject_MaxDepthZero()
    {
        var obj = _sut.Model<InfiniteObject>(conf => conf.MaxDepth(0));
        obj.Should().NotBeNull();
        obj.Child.Should().BeNull();
    }

    [Fact]
    public void RandomInfiniteObject_MaxDepthOne()
    {
        var obj = _sut.Model<InfiniteObject>(conf => conf.MaxDepth(1));
        obj.Should().NotBeNull();
        obj.Child.Should().NotBeNull();
        obj.Child.Child.Should().BeNull();
    }

    [Fact]
    public void WithIgnoreConfiguration()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Ignore(o => o.Name));
        obj.Should().NotBeNull();
        obj.Name.Should().BeNull();
    }

    [Fact]
    public void WithFixedValueConfiguration()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Set(o => o.Id, -5));
        obj.Should().NotBeNull();
        obj.Id.Should().Be(-5);
    }

    [Fact]
    public void WithCountConfiguration()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Count(o => o.Ids, 5));
        obj.Should().NotBeNull();
        obj.Ids.Should().HaveCount(5);
    }

    [Fact]
    public void WithOverrideConfiguration()
    {
        var obj = _sut.Model<SimpleObject>(conf => conf.Set(o => o.Name, "First name").Set(o => o.Name, "Second name"));
        obj.Should().NotBeNull();
        obj.Name.Should().Be("Second name");
    }

    [Fact]
    public void WithoutEmptyConstructor()
    {
        Repeat(() =>
        {
            var obj = _sut.Model<NoEmptyConstructorObject>();
            obj.Should().NotBeNull();
            obj.Id.Should().NotBe(0);
            obj.Child.Should().NotBeNull();
            obj.Child.Id.Should().NotBe(0);
            obj.Child.Name.Should().NotBeNull();
            obj.Name.Should().NotBeNull();
        });
    }

    [Fact]
    public void WithoutEmptyConstructorAndConfigurationOnConstructorParameterProperty()
    {
        Repeat(() =>
        {
            var obj = _sut.Model<NoEmptyConstructorObject>(conf => conf.Ignore(p => p.Child.Name));
            obj.Should().NotBeNull();
            obj.Child.Should().NotBeNull();
            obj.Child.Name.Should().BeNull();
        });
    }

    [Fact]
    public void WithReadOnlyPropertyAndBackingField()
    {
        Repeat(() =>
        {
            var model = _sut.Model<NonWritablePropertyWithBackingFieldObject>();
            model.Name.Should().NotBeNull();
        });
    }

    [Fact]
    public void WithReadOnlyPropertyAndComplexBackingField()
    {
        var model = _sut.Model<NonWritablePropertyWithBackingFieldObject>();
        model.Child.Should().NotBeNull();
    }

    [Fact]
    public void WithReadOnlyPropertyAndComplexBackingFieldWithRule()
    {
        var child = new SimpleObject() { Id = 5 };
        var model = _sut.Model<NonWritablePropertyWithBackingFieldObject>(conf => conf.Set(o => o.Child, child));
        model.Child.Should().NotBeNull();
        model.Child.Should().Be(child);
    }

    private void Repeat(Action action)
    {
        for (int i = 0; i < RepeatCount; i++)
            action();
    }

    private void RepeatWithoutCollision<T>(Func<T> func)
    {
        var list = new List<T>();
        for (int i = 0; i < RepeatCount; i++)
            list.Add(func());
        list.Should().OnlyHaveUniqueItems();
    }
}
