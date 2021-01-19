# Azura
 source generation based serialization 

| Package                | Release |
|------------------------|---------|
| `Azura`           | [![NuGet](https://img.shields.io/nuget/v/Azura.svg)](https://www.nuget.org/packages/Azura/)|
| `Azura.Generator` | [![NuGet](https://img.shields.io/nuget/v/Azura.Generator.svg)](https://www.nuget.org/packages/Azura.Generator/) |


## Supported

* Fields / properties (even init-only) of supported types on structs, classes, and records
* Common BCL primitives (and string)
  - `byte` / `sbyte` / `ushort` / `short` / `uint` / `int` / `ulong` / `long`
  - `float` / `double`
  - `string`
* Arrays of supported types
* Nullable types

## Limitations

* Type must have parameter-less constructor
* Very basic builtin type support
  - Ideally support for most common collections e.g.
    `Dictionary<TKey,TValue>` / `List<T>` / `HashSet<T>` should be inbuilt
* Cannot (currently) serialize nested classes due to namespace
  - Potential future workaround: use root namespace `Azura`

## Usage

0. Add generator library (comes with base package as well)

```xml
<PackageReference Include="Azura.Generator" Version="ver" />
```

1. Define a data type and mark properties or fields

```csharp
[Azura]
public record Data
{
    [Azura] public int Property1 { get; init; }
    [Azura] public string? Property2 { get; init; }
    [Azura] public string?[]? Property3 { get; init; }
}
```

2. Serialize the data

```csharp
var data = new Data {Property2 = "Execute order", Property1 = 66};
data.Serialize(stream);
```

3. Retrieve the data
   - Helper types are generated as `<ClassName>Serialization`

```csharp
var data = DataSerialization.Deserialize(stream);
```

## Custom serialization

Custom serialization for existing library types can be added but requires
manual implementation of the below signatures in a class under the target
type's namespace named `<ClassName>Serialization`.

```csharp
public static T Deserialize(Stream stream);
public static void Serialize(this T self, Stream stream);
public static T[] DeserializeArray(Stream stream, int count);
public static void Serialize(this Span<T> self, Stream stream);
public static void Serialize(this ReadOnlySpan<T> self, Stream stream);
public static T?[] DeserializeArrayNullable(Stream stream, int count);
public static void SerializeNullable(this Span<T?> self, Stream stream);
public static void SerializeNullable(this ReadOnlySpan<T?> self, Stream stream);
```