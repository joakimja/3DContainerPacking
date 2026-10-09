# CromulentBisgetti.ContainerPacking.Services

**CromulentBisgetti.ContainerPacking.Services** is a .NET 9 library for planning and optimizing the packing of containers, trucks, or other boxes.

It helps you compute how to fit items efficiently into 3D space, supporting logistics, shipping, and warehouse automation.

This library continues the development of [davidmchapman's original project](https://github.com/davidmchapman/3DContainerPacking) to keep it maintained and evolving.

---

## 📦 Installation

Install via NuGet.org:

```bash
dotnet add package CromulentBisgetti.ContainerPacking.Services
```

Or in your .csproj:

```xml
<PackageReference Include="CromulentBisgetti.ContainerPacking.Services" Version="*" />
```

---

## 🚀 Quick Example

```csharp
using CromulentBisgetti.ContainerPacking;
using CromulentBisgetti.ContainerPacking.Entities;
using CromulentBisgetti.ContainerPacking.Algorithms;

var items = new List<Item>
{
    new Item(1, 10, 20, 30, 1),
    new Item(2, 15, 15, 15, 1)
};

var container = new Container(1, 100, 100, 100);

var result = PackingService.Pack(
    new List<Container> { container }, items,
    new List<int> { (int)AlgorithmType.EB_AFIT });

Console.WriteLine($"Packed items: {result[0].AlgorithmPackingResults[0].PackedItems.Count}");
```

---

## Orientation restrictions

`Item.KeepUpright` keeps the original height vertical. `Item.KeepLengthwise`
keeps the original length along the physical container length. Both default
to `false`. When using these restrictions, construct items with
`Dim1 = length`, `Dim2 = width`, `Dim3 = height`. Dimensions must use the same
unit as the container.

The result uses physical **X = length, Y = height, Z = width**. The algorithm's
internal container rotations do not change this convention.

| KeepUpright | KeepLengthwise | Allowed physical (PackDimX, PackDimY, PackDimZ) |
|---|---|---|
| false | false | All existing permutations of length, width and height |
| true | false | (length, height, width) or (width, height, length) |
| false | true | (length, height, width) or (length, width, height) |
| true | true | (length, height, width) |

Apply global restrictions using the four-argument `PackingService.Pack`
overload. Global and item flags combine using OR: individual settings can add
restrictions but cannot remove global ones. A null `PackingOptions` applies
only individual flags. Existing three-argument calls remain supported.

```csharp
var packages = new List<Item>
{
    new Item(1, 4800, 1200, 1100, 8)
};
var trucks = new List<Container>
{
    new Container(1, 13600, 2500, 2700)
};
var options = new PackingOptions
{
    KeepUpright = true,
    KeepLengthwise = true
};
var results = PackingService.Pack(
    trucks, packages, new List<int> { (int)AlgorithmType.EB_AFIT }, options);
var packing = results[0].AlgorithmPackingResults[0];
// Every packed package has physical dimensions X=4800, Y=1100, Z=1200.
// Packages that cannot be placed in an allowed orientation are in UnpackedItems.
```

Individual restrictions can also be configured with an object initializer:

```csharp
var item = new Item(2, 4800, 1200, 1100, 3)
{
    KeepUpright = true,
    KeepLengthwise = true
};
```

Direct `EB_AFIT.Run` calls honor individual item flags. Restrictions apply to
all quantity copies during placement and layer selection. Unrestricted calls
retain the existing packing behavior. Packing is a geometric heuristic;
weight, load support and cargo securing are outside this model.

---

## 📚 Features

- 3D bin-packing algorithms
- Optimizes use of container space
- Supports multiple item sizes
- .NET 9 compatible
- Open Source (MIT License)

---

## 🛠️ Target Framework

- .NET 9

---

## 💻 Repository

Source code and issue tracking:  
[GitHub - 3DContainerPacking](https://github.com/joakimja/3DContainerPacking)

---

## 📜 License

This project is licensed under the MIT License. See the LICENSE file for details.
