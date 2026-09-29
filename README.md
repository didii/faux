# FauxData

Generate fake data for your own .NET types.
`FauxData` fills every property of an object graph with random values: nested objects, collections, constructor parameters and read-only properties included.
Rules let you control exactly which values end up where.
They can target a single property, every property of a type, or a specific branch of the object graph, and they can be layered on top of each other.

Typical uses: unit and integration tests, seeding a development database.

## Getting started

```bash
dotnet add package FauxData
```

```csharp
using FauxData;

var faux = new Faux();
var order = faux.Model<Order>();
```

The examples below use this model:

```csharp
public class Order
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Customer Customer { get; set; }
    public Address ShippingAddress { get; set; }
    public List<OrderLine> Lines { get; set; }
    public decimal Total { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string CountryCode { get; set; }
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Product { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum OrderStatus { Pending, Shipped, Delivered }
```

## Generating data

### A single object

Every property of `Order` is filled, including `Customer`, `Customer.Address`, `ShippingAddress` and the `Lines`
collection.

```csharp
var order = new Faux().Model<Order>();
```

### A list of objects

```csharp
List<Order> orders = new Faux().Model<Order>(10);
```

### When the type is only known at runtime

```csharp
object order = new Faux().Model(typeof(Order));
```

### An "empty" object

`Pas<T>()` builds the same object graph, but gives every value its default instead of a random one: `0`, `null`,
`false`, the first enum value, and empty collections. Nested objects are still created. Configured rules are not
applied.

```csharp
var order = new Faux().Pas<Order>();
// order.Id == 0, order.Reference == null, order.Lines is empty, order.Customer is not null
```

### In a test

```csharp
public class OrderServiceTests
{
    private readonly Faux _faux = new Faux();

    [Fact]
    public void Ship_SetsStatusToShipped()
    {
        var order = _faux.Model<Order>(conf => conf.Set(o => o.Status, OrderStatus.Pending));

        new OrderService().Ship(order);

        Assert.Equal(OrderStatus.Shipped, order.Status);
    }
}
```

## Default values

With no rules configured, values are generated like this:

| Type                                   | Generated value                                                          |
|----------------------------------------|--------------------------------------------------------------------------|
| `int`, `long`                          | Increasing numbers, unique within the process (handy for IDs)             |
| `float`, `double`, `decimal`           | Between 1 and 100                                                        |
| `bool`                                 | `true` or `false`                                                        |
| `string`                               | 5 to 10 random characters from `a-z`, `A-Z`, `0-9`, `-` and `_`          |
| `Guid`                                 | `Guid.NewGuid()`                                                         |
| `DateTime`                             | Within one year before or after now, in UTC                              |
| `DateOnly`, `TimeOnly`, `TimeSpan`     | Random date / time of day                                                |
| `TimeZoneInfo`                         | A random system time zone that has an IANA ID                            |
| Enums                                  | A random defined value                                                   |
| Arrays, `List<T>`, `IEnumerable<T>`, … | 2 or 3 generated items                                                   |
| Nullable value types (`int?`, …)       | A value, never `null`                                                    |
| Other classes                          | A new instance with all of its properties generated (see below)          |

How objects are built:

- The **public constructor with the fewest parameters** is used. Constructor parameters are generated too. A parameter
  whose name and type match a property (`string name` matches `Name`) follows the rules configured for that property.
- Properties with a public setter are then filled.
- Read-only properties are filled through a backing field named after the property (`Name` uses `_name`), if there is
  one. Otherwise they are skipped.
- Types without a public constructor, such as abstract classes and interfaces other than collections, cannot be
  created and throw an `InvalidOperationException`. Use a concrete type, or register a
  [type factory](#custom-type-factories).
- Self-referencing types (`Node.Parent`, `Node.Children`, …) are safe. Generation stops at a
  [maximum depth](#maxdepth).

## Configuring rules

Rules can be configured in two places:

1. **On the `Faux` instance.** These apply to every model created with that instance, which suits shared test
   setup.
2. **Per call to `Model`.** These apply only to that call and to nothing created afterwards.

```csharp
var faux = new Faux();

// 1. Instance rule: every Customer created by this Faux gets this email
faux.ForType<Customer>().Set(c => c.Email, "test@example.com");

// 2. Model rule: only this order gets the reference "ORD-1"
var order = faux.Model<Order>(conf => conf.Set(o => o.Reference, "ORD-1"));
```

Every rule method returns the builder, so calls can be chained:

```csharp
var order = faux.Model<Order>(conf => conf
    .Set(o => o.Status, OrderStatus.Pending)
    .Ignore(o => o.Total)
    .Count(o => o.Lines, 5));
```

### Rules

#### `Set`: a fixed value

```csharp
faux.Model<Order>(conf => conf.Set(o => o.Status, OrderStatus.Shipped));
```

The expression can reach into nested properties:

```csharp
faux.Model<Order>(conf => conf.Set(o => o.ShippingAddress.CountryCode, "BE"));
```

`Set` hands the **same instance** to every match. For a new value each time, use `Factory`.

#### `Factory`: a value computed on each assignment

```csharp
faux.Model<Order>(conf => conf.Factory(o => o.Reference, () => $"ORD-{Random.Shared.Next(1000, 9999)}"));
```

#### `Ignore`: leave a property at its default

The property is not generated. It keeps whatever the constructor or property initializer assigned, usually `null` or
`0`.

```csharp
faux.Model<Order>(conf => conf.Ignore(o => o.Id));
```

#### `Count`: size of a collection or length of a string

```csharp
faux.Model<Order>(conf => conf
    .Count(o => o.Lines, 5)                  // exactly 5 order lines
    .Count(o => o.Reference, 12)             // string of exactly 12 characters
    .Count(o => o.Customer.Name, 3, 20));    // string of 3 to 20 characters (inclusive)
```

`Count(o => o.Lines, 0)` gives an empty collection.

#### `Assign`: a value derived from the finished object

`Assign` runs **after** the whole object has been generated. It receives the root object, so the value can depend on
other generated data.

```csharp
faux.Model<Order>(conf => conf
    .Assign(o => o.Total, o => o.Lines.Sum(l => l.Quantity * l.Price)));
```

The root is the type the rule was configured for: `T` inside `Model<T>(...)`, or `T` in `ForType<T>()`.

#### `MaxDepth`: limit nesting

Generation stops going deeper once the maximum depth is reached, which prevents endless recursion on
self-referencing types. The default is `10`. Every nested object or collection counts as one level. At the limit,
objects are still created but their properties are left empty, and collections are empty.

```csharp
faux.MaxDepth(3);                                   // for all models of this instance
faux.Model<Order>(conf => conf.MaxDepth(1));        // Order's own properties are filled; Customer is created but left empty
```

### Choosing what a rule applies to

#### `Model<T>(conf => ...)`: paths from the root

Inside `Model`, property expressions are **exact paths from the root object**. The rule below changes the street of
the order's shipping address only. `Customer.Address.Street` is still random.

```csharp
faux.Model<Order>(conf => conf.Set(o => o.ShippingAddress.Street, "Main street 1"));
```

#### `ForType<T>()`: every occurrence of a type

`ForType<T>()` targets a type **wherever it appears** in the object graph. This sets `CountryCode` on
`Order.ShippingAddress` and on `Order.Customer.Address`, and on any other `Address`:

```csharp
faux.ForType<Address>().Set(a => a.CountryCode, "BE");

// also available inside Model
faux.Model<Order>(conf => conf.ForType<Address>().Set(a => a.CountryCode, "BE"));
```

On a `Faux` instance, `ForType<T>()` is the way to configure properties of a specific type.

#### `ForRoot<T>()`: only when `T` is the object being created

Like `ForType<T>()`, except the rule applies only when `T` is the type passed to `Model<T>()`, and never when `T`
shows up nested inside another model. Use it for instance rules meant for "the object under test" only.

```csharp
faux.ForRoot<Customer>().Ignore(c => c.Id);

faux.Model<Customer>();   // Id is ignored
faux.Model<Order>();      // order.Customer.Id is still generated
```

#### `For(x => x.Property)`: step into a nested object

Rules that follow `For` are relative to that property. This is the same as writing the full path, but reads better
when several properties are set:

```csharp
faux.Model<Order>(conf => conf
    .For(o => o.ShippingAddress)
        .Set(a => a.Street, "Main street 1")
        .Set(a => a.City, "Brussels")
        .Set(a => a.CountryCode, "BE"));
```

`For` calls can be chained to go deeper, for example `.For(o => o.Customer).For(c => c.Address)`. Note that after a
`For`, the chain stays at that level. Start a new chain from `conf` to configure something else.

#### `ForAll(x => x.Collection)`: step into every item of a collection

```csharp
faux.Model<Order>(conf => conf
    .ForAll(o => o.Lines)
        .Set(l => l.Quantity, 1));
```

Together with `Assign`, this links children to their parent:

```csharp
faux.Model<Order>(conf => conf
    .ForAll(o => o.Lines)
        .Assign(l => l.OrderId, order => order.Id));   // "order" is the root Order
```

### Which rule wins

When several rules match the same property, **the rule configured last wins**, and **rules passed to `Model` always
win over rules on the `Faux` instance**.

```csharp
var faux = new Faux();

faux.ForType<Order>().Ignore(o => o.Id);
// Order.Id is ignored

faux.ForType<Order>().Set(o => o.Id, 5);
// Order.Id is 5, overriding the Ignore rule above

var order = faux.Model<Order>(conf => conf.Set(o => o.Id, 10));
// order.Id is 10: Model rules take priority over instance rules

var other = faux.Model<Order>();
// other.Id is 5 again: Model rules do not leak into later calls
```

"Last wins" is purely about order, not about how specific a rule is. A later `ForType<Address>()` rule overrides an
earlier `Set(o => o.ShippingAddress.City, ...)` on the same instance.

### Custom type factories

Change how **every** value of a type is generated, for example to get realistic data instead of random characters:

```csharp
var faux = new Faux();

faux.TypeFactory<DateTime>(() => new DateTime(2025, 1, 1).AddDays(Random.Shared.Next(365)));
faux.TypeFactory<decimal>(() => Math.Round((decimal)Random.Shared.NextDouble() * 1000, 2));

// Handle types based on a condition, e.g. an interface that has no public constructor
faux.TypeFactory(type => type == typeof(IClock), _ => new FakeClock());
```

User-configured factories override the built-in defaults.

### Advanced: rules with custom conditions

`Ignore` and `Set` on the `Faux` instance also take a condition that inspects every generated value. It receives:

- `type`: the type of the value being generated.
- `props`: the property path leading to it, **innermost property first**. For `order.Customer.Address.City` that is
  `[City, Address, Customer]`. Values in a collection share their collection's path.

```csharp
// Ignore every property whose name ends in "Hash", on any type
faux.Ignore((type, props) => props.Length > 0 && props[0].Name.EndsWith("Hash"));

// Set every string property called "Email"
faux.Set((type, props) => type == typeof(string) && props.Length > 0 && props[0].Name == "Email", "test@example.com");
```

### Built-in extensions

`FauxExtensions` has ready-made rules for common cases:

| Extension               | Effect                                                                                                   |
|-------------------------|----------------------------------------------------------------------------------------------------------|
| `IgnoreIdProps()`       | Ignores every `int` or `long` property named `Id`, so Entity Framework can generate the keys on insert |
| `IgnoreObsoleteProps()` | Ignores every property marked `[Obsolete]`                                                               |

```csharp
var faux = new Faux()
    .IgnoreIdProps()
    .IgnoreObsoleteProps();

dbContext.Orders.AddRange(faux.Model<Order>(50));
dbContext.SaveChanges();
```

### Starting from other base rules

By default a `Faux` starts from `FauxDefaults.Rules`. To start from a different base set, pass it to the constructor.
For example, begin with all defaults, like `Pas()`, and add rules on top:

```csharp
var faux = new Faux(FauxDefaults.PasRules);
faux.ForType<Order>().Set(o => o.Status, OrderStatus.Pending);
```

A rule list that has no factory for a primitive type (such as `int` or `string`) cannot generate that type. Build
on top of `FauxDefaults.Rules` or `FauxDefaults.PasRules` rather than starting from an empty list.
