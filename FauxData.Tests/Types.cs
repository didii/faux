using System.Reflection;

namespace FauxData.Tests;

public enum SomeEnum
{
    One = 5,
    Two = 7,
    Three = 38,
}

public class SimpleObject
{
    public static PropertyInfo IdProp => typeof(SimpleObject).GetProperty("Id")!;
    public static PropertyInfo NameProp => typeof(SimpleObject).GetProperty("Name")!;
    public static PropertyInfo ValueProp => typeof(SimpleObject).GetProperty("Value")!;

    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public SomeEnum Value { get; set; }
    public IList<int> Ids { get; set; } = null!;
}

public class NestedObject
{
    public static PropertyInfo IdProp => typeof(NestedObject).GetProperty("Id")!;
    public static PropertyInfo CodeProp => typeof(NestedObject).GetProperty("Code")!;
    public static PropertyInfo Child1Prop => typeof(NestedObject).GetProperty("Child1")!;
    public static PropertyInfo Child2Prop => typeof(NestedObject).GetProperty("Child2")!;

    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public SimpleObject Child1 { get; set; } = null!;
    public SimpleObject Child2 { get; set; } = null!;
    public IList<SimpleObject> Children { get; set; } = null!;
}

public class InfiniteObject
{
    public static PropertyInfo IdProp => typeof(InfiniteObject).GetProperty("Id")!;
    public static PropertyInfo ChildProp => typeof(InfiniteObject).GetProperty("Child")!;

    public int Id { get; set; }
    public InfiniteObject Child { get; set; } = null!;
}

public class NoEmptyConstructorObject(int id, SimpleObject child)
{
    public int Id { get; } = id;
    public string? Name { get; set; }
    public SimpleObject Child { get; } = child;
}

public class NonWritablePropertyWithBackingFieldObject
{
    public static PropertyInfo ChildProp => typeof(NonWritablePropertyWithBackingFieldObject).GetProperty("Child")!;

    // Only use is to test if reflection can access this field, so compiler will never catch this
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private string? _name;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    public string? Name => _name;

    private SimpleObject _child = null!;
    public SimpleObject Child => _child;
}

public class Organisation
{
    public IList<CollectionPoint> CollectionPoints { get; set; } = new List<CollectionPoint>();
    private IList<Department> _departments = new List<Department>();
    public IReadOnlyList<Department> Departments => _departments.AsReadOnly();

    public class Department
    {
        public int Id { get; set; }
        public CollectionPoint CollectionPoint { get; set; } = null!;
    }

    public class CollectionPoint
    {
        public int Id { get; set; }
    }
}
