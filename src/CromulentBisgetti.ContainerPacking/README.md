﻿# CromulentBisgetti.ContainerPacking.Services

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
using CromulentBisgetti.ContainerPacking.Services;
using CromulentBisgetti.ContainerPacking.Entities;

var items = new List<Item>
{
    new Item(1, 10, 20, 30, 1),
    new Item(2, 15, 15, 15, 1)
};

var container = new Container(1, 100, 100, 100);

var service = new PackingService();
var result = service.Pack(container, items);

Console.WriteLine($"Packed items: {result.PackedItems.Count}");
```

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
