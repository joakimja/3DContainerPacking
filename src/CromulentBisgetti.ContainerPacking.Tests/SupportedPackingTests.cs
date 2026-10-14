using CromulentBisgetti.ContainerPacking.Algorithms;
using CromulentBisgetti.ContainerPacking.Entities;

namespace CromulentBisgetti.ContainerPacking.Tests;

public class SupportedPackingTests
{
    private static AlgorithmPackingResult Pack(Container container, List<Item> items) =>
        PackingService.Pack([container], items, [(int)AlgorithmType.EB_AFIT],
            new PackingOptions { StablePacking = true }).Single().AlgorithmPackingResults.Single();

    [Fact]
    public void FullSupport_CanBeEnabledAndDisabledWithoutChangingInputs()
    {
        var container = new Container(1, 13, 7, 8);
        List<Item> items = [new(1, 5, 6, 5, 3), new(2, 5, 7, 7, 3), new(3, 7, 2, 3, 3), new(4, 3, 7, 3, 3)];
        var defaults = PackingService.Pack([container], items, [(int)AlgorithmType.EB_AFIT]).Single().AlgorithmPackingResults.Single();
        var disabled = PackingService.Pack([container], items, [(int)AlgorithmType.EB_AFIT],
            new PackingOptions { StablePacking = false }).Single().AlgorithmPackingResults.Single();
        var enabled = Pack(container, items);
        Assert.Equal(7, disabled.PackedItems.Count);
        Assert.ThrowsAny<Exception>(() => AssertSupported(disabled.PackedItems));
        Assert.Equal(defaults.PackedItems.Select(Signature), disabled.PackedItems.Select(Signature));
        Assert.NotEmpty(enabled.PackedItems);
        AssertSupported(enabled.PackedItems);
        Assert.All(items, i => Assert.False(i.IsPacked));
    }

    [Fact]
    public void DirectAlgorithm_FullSupportCanBeSwitchedBetweenRuns()
    {
        var algorithm = new EB_AFIT { StablePacking = true };
        var container = new Container(1, 13, 7, 8);
        List<Item> Items() => [new(1, 5, 6, 5, 3), new(2, 5, 7, 7, 3), new(3, 7, 2, 3, 3), new(4, 3, 7, 3, 3)];
        AssertSupported(algorithm.Run(container, Items()).PackedItems);
        algorithm.StablePacking = false;
        Assert.Equal(7, algorithm.Run(container, Items()).PackedItems.Count);
        algorithm.StablePacking = true;
        AssertSupported(algorithm.Run(container, Items()).PackedItems);
    }

    private static string Signature(Item item) => $"{item.ID}:{item.CoordX},{item.CoordY},{item.CoordZ}:{item.PackDimX},{item.PackDimY},{item.PackDimZ}";

    [Fact]
    public async Task ParallelSettings_AreIsolatedAndCombineWithOrientationRestrictions()
    {
        var container = new Container(1, 13, 7, 8);
        List<Item> items = [new(1, 5, 6, 5, 3), new(2, 5, 7, 7, 3), new(3, 7, 2, 3, 3), new(4, 3, 7, 3, 3)];
        await Task.WhenAll(Enumerable.Range(0, 12).Select(index => Task.Run(() =>
        {
            bool enabled = index % 2 == 0;
            var result = PackingService.Pack([container], items, [(int)AlgorithmType.EB_AFIT], new PackingOptions
            {
                StablePacking = enabled, KeepUpright = enabled, KeepLengthwise = enabled
            }).Single().AlgorithmPackingResults.Single();
            Assert.Equal(12, result.PackedItems.Count + result.UnpackedItems.Count);
            if (enabled)
            {
                Assert.NotEmpty(result.PackedItems);
                AssertSupported(result.PackedItems);
                Assert.All(result.PackedItems, item =>
                {
                    Assert.Equal(item.Dim3, item.PackDimY);
                    Assert.Equal(item.Dim1, item.PackDimX);
                });
            }
            else Assert.Equal(7, result.PackedItems.Count);
        })));
        Assert.All(items, item => Assert.False(item.IsPacked || item.KeepUpright || item.KeepLengthwise));
    }

    [Fact]
    public void AvailableFloor_IsUsedBeforeStacking()
    {
        var result = Pack(new Container(1, 12, 4, 12),
            [new Item(1, 4, 4, 2, 3) { KeepUpright = true, KeepLengthwise = true }]);
        Assert.Equal(3, result.PackedItems.Count);
        Assert.All(result.PackedItems, item => Assert.Equal(0m, item.CoordY));
    }

    [Fact]
    public void StackedPackage_CanSpanAdjacentSupports()
    {
        var result = Pack(new Container(1, 6, 4, 3),
            [new Item(1, 3, 4, 2, 2) { KeepUpright = true, KeepLengthwise = true },
             new Item(2, 6, 4, 1, 1) { KeepUpright = true, KeepLengthwise = true }]);
        Assert.Equal(3, result.PackedItems.Count);
        Assert.True(result.IsCompletePack);
        Assert.Equal(2m, result.PackedItems.Single(i => i.ID == 2).CoordY);
        Assert.All(result.PackedItems.Where(i => i.ID == 1), i => Assert.Equal(0m, i.CoordY));
        AssertSupported(result.PackedItems);
    }

    [Fact]
    public void MixedHeights_DoNotLeaveFloatingPackages()
    {
        var result = Pack(new Container(1, 8, 4, 8),
            [new Item(1, 4, 4, 3, 1) { KeepUpright = true, KeepLengthwise = true },
             new Item(2, 4, 4, 2, 5) { KeepUpright = true, KeepLengthwise = true }]);
        Assert.NotEmpty(result.PackedItems);
        Assert.Contains(result.PackedItems, i => i.CoordY > 0);
        Assert.Equal(6, result.PackedItems.Count + result.UnpackedItems.Count);
        AssertSupported(result.PackedItems);
    }

    [Fact]
    public void UnsupportedBridge_IsUnpackedAndExcludedFromVolume()
    {
        var container = new Container(1, 6, 4, 4);
        var result = Pack(container,
            [new Item(1, 6, 4, 1, 1) { KeepUpright = true, KeepLengthwise = true },
             new Item(2, 2, 4, 3, 2) { KeepUpright = true, KeepLengthwise = true },
             new Item(3, 6, 4, 1, 1) { KeepUpright = true, KeepLengthwise = true }]);
        AssertSupported(result.PackedItems);
        Assert.Equal(4, result.PackedItems.Count + result.UnpackedItems.Count);
        Assert.NotEmpty(result.UnpackedItems);
        Assert.False(result.IsCompletePack);
        Assert.Equal(Math.Round(result.PackedItems.Sum(i => i.Volume) / (6m * 4 * 4) * 100, 2),
            result.PercentContainerVolumePacked);
    }

    [Fact]
    public void VariedLoads_AlwaysHaveFullSupportAndPreserveCopies()
    {
        var random = new Random(1729);
        for (int scenario = 0; scenario < 40; scenario++)
        {
            var container = new Container(1, random.Next(6, 14), random.Next(6, 14), random.Next(6, 14));
            var items = Enumerable.Range(1, 4).Select(id => new Item(id,
                random.Next(2, 8), random.Next(2, 8), random.Next(2, 8), 3)).ToList();
            var result = Pack(container, items);
            AssertSupported(result.PackedItems);
            AssertFloorExhaustedBeforeStacking(container, result);
            foreach (var original in items)
                Assert.Equal(3, result.PackedItems.Concat(result.UnpackedItems).Count(i => i.ID == original.ID));
        }
    }

    private static void AssertFloorExhaustedBeforeStacking(Container container, AlgorithmPackingResult result)
    {
        int firstStack = result.PackedItems.FindIndex(i => i.CoordY > 0);
        if (firstStack < 0) return;
        var floor = result.PackedItems.Take(firstStack).ToList();
        var remaining = result.PackedItems.Skip(firstStack).Concat(result.UnpackedItems);
        foreach (var item in remaining)
        {
            decimal[] d = { item.Dim1, item.Dim2, item.Dim3 };
            for (int a = 0; a < 3; a++)
            for (int b = 0; b < 3; b++)
            {
                if (a == b) continue;
                int c = 3 - a - b;
                if (d[b] > container.Height) continue;
                // Test every integer floor coordinate, independently of the
                // production boundary-candidate search (these fixtures use integers).
                for (decimal x = 0; x + d[a] <= container.Length; x++)
                for (decimal z = 0; z + d[c] <= container.Width; z++)
                    Assert.True(floor.Any(s => x < s.CoordX + s.PackDimX && x + d[a] > s.CoordX &&
                        z < s.CoordZ + s.PackDimZ && z + d[c] > s.CoordZ), "Stacking began while a floor placement remained.");
            }
        }
    }

    internal static void AssertSupported(List<Item> packed)
    {
        Assert.All(packed, item =>
        {
            Assert.True(item.IsPacked);
            Assert.Equal(item.Volume, item.PackDimX * item.PackDimY * item.PackDimZ);
        });
        for (int i = 0; i < packed.Count; i++)
        for (int j = i + 1; j < packed.Count; j++)
        {
            var a = packed[i]; var b = packed[j];
            Assert.True(a.CoordX + a.PackDimX <= b.CoordX || b.CoordX + b.PackDimX <= a.CoordX ||
                a.CoordY + a.PackDimY <= b.CoordY || b.CoordY + b.PackDimY <= a.CoordY ||
                a.CoordZ + a.PackDimZ <= b.CoordZ || b.CoordZ + b.PackDimZ <= a.CoordZ);
        }
        foreach (var item in packed.Where(i => i.CoordY > 0))
        {
            var supports = packed.Where(s => s.CoordY + s.PackDimY == item.CoordY).ToList();
            // Independent coverage check: sweep strips and merge their support intervals.
            var xs = supports.SelectMany(s => new[] { s.CoordX, s.CoordX + s.PackDimX })
                .Append(item.CoordX).Append(item.CoordX + item.PackDimX)
                .Where(x => x >= item.CoordX && x <= item.CoordX + item.PackDimX).Distinct().Order().ToArray();
            for (int x = 0; x < xs.Length - 1; x++)
            {
                decimal coveredZ = item.CoordZ;
                var intervals = supports.Where(s => s.CoordX <= xs[x] && s.CoordX + s.PackDimX >= xs[x + 1] &&
                    s.CoordZ + s.PackDimZ > item.CoordZ && s.CoordZ < item.CoordZ + item.PackDimZ)
                    .OrderBy(s => s.CoordZ);
                foreach (var support in intervals)
                {
                    Assert.True(support.CoordZ <= coveredZ, $"Item {item.ID} has a support gap.");
                    coveredZ = Math.Max(coveredZ, support.CoordZ + support.PackDimZ);
                }
                Assert.True(coveredZ >= item.CoordZ + item.PackDimZ, $"Item {item.ID} lacks full support.");
            }
        }
    }
}
