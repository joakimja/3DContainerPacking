using CromulentBisgetti.ContainerPacking.Algorithms;
using CromulentBisgetti.ContainerPacking.Entities;

namespace CromulentBisgetti.ContainerPacking.Tests;

public class OrientationRestrictionTests
{
    private static AlgorithmPackingResult Pack(Container container, List<Item> items, PackingOptions? options = null)
    {
        return PackingService.Pack([container], items, [(int)AlgorithmType.EB_AFIT], options)
            .Single().AlgorithmPackingResults.Single();
    }

    public static IEnumerable<object[]> ExactFitCases()
    {
        // Physical X/Y/Z permutations of L=7, B=5, H=3.
        int[][] dimensions = [[7, 3, 5], [5, 3, 7], [7, 5, 3], [3, 5, 7], [5, 7, 3], [3, 7, 5]];
        foreach (int[] dims in dimensions)
        for (int flags = 0; flags < 16; flags++)
            yield return [dims[0], dims[2], dims[1], (flags & 1) != 0, (flags & 2) != 0,
                (flags & 4) != 0, (flags & 8) != 0];
    }

    [Theory]
    [MemberData(nameof(ExactFitCases))]
    public void ExactFit_RespectsGlobalAndIndividualFlagsAcrossPhysicalAxes(
        int length, int width, int height, bool globalUpright, bool globalLengthwise,
        bool itemUpright, bool itemLengthwise)
    {
        var item = new Item(1, 7, 5, 3, 1) { KeepUpright = itemUpright, KeepLengthwise = itemLengthwise };
        var container = new Container(1, length, width, height);
        var result = Pack(container, [item], new PackingOptions
        {
            KeepUpright = globalUpright, KeepLengthwise = globalLengthwise
        });
        bool upright = globalUpright || itemUpright;
        bool lengthwise = globalLengthwise || itemLengthwise;
        bool fits = (!upright || height == 3) && (!lengthwise || length == 7);

        Assert.Equal(fits ? 1 : 0, result.PackedItems.Count);
        Assert.Equal(fits ? 0 : 1, result.UnpackedItems.Count);
        Assert.Equal(fits, result.IsCompletePack);
        if (fits)
        {
            Item packed = result.PackedItems.Single();
            Assert.Equal((length, height, width), ((int)packed.PackDimX, (int)packed.PackDimY, (int)packed.PackDimZ));
            AssertGeometry(container, result.PackedItems);
        }
        // Global restrictions must never mutate the input instance.
        Assert.Equal(itemUpright, item.KeepUpright);
        Assert.Equal(itemLengthwise, item.KeepLengthwise);
        Assert.False(item.IsPacked);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void MultipleCopies_InCubeAndRectangularContainers_KeepAllowedOrientations(bool upright, bool lengthwise)
    {
        foreach (var container in new[] { new Container(1, 20, 20, 20), new Container(2, 23, 11, 17), new Container(3, 11, 23, 17) })
        {
            var result = Pack(container, [new Item(1, 7, 5, 3, 9)
            {
                KeepUpright = upright, KeepLengthwise = lengthwise
            }]);
            Assert.NotEmpty(result.PackedItems);
            Assert.Equal(9, result.PackedItems.Count + result.UnpackedItems.Count);
            Assert.All(result.PackedItems, item => AssertOrientation(item, upright, lengthwise));
            AssertGeometry(container, result.PackedItems);
        }
    }

    [Theory]
    [InlineData(7, 7, 3)]
    [InlineData(7, 3, 7)]
    [InlineData(3, 7, 7)]
    [InlineData(7, 7, 7)]
    public void EqualDimensions_StillPermitValidFixedOrientation(int length, int width, int height)
    {
        var container = new Container(1, length, width, height);
        var result = Pack(container, [new Item(1, length, width, height, 1)
        {
            KeepUpright = true, KeepLengthwise = true
        }]);
        Item item = Assert.Single(result.PackedItems);
        Assert.Equal(((decimal)length, (decimal)height, (decimal)width), (item.PackDimX, item.PackDimY, item.PackDimZ));
        AssertGeometry(container, result.PackedItems);
    }

    [Fact]
    public void MixedTypes_PreserveRestrictionsForEveryQuantityCopy()
    {
        var items = new List<Item>
        {
            new(1, 7, 5, 3, 3) { KeepUpright = true },
            new(2, 6, 4, 2, 4) { KeepLengthwise = true },
            new(3, 8, 3, 2, 2) { KeepUpright = true, KeepLengthwise = true },
            new(4, 4, 3, 1, 5),
            new(5, 100, 2, 1, 2) { KeepLengthwise = true }
        };
        var container = new Container(1, 30, 20, 15);
        var result = Pack(container, items);
        Assert.NotEmpty(result.PackedItems);
        var all = result.PackedItems.Concat(result.UnpackedItems).ToList();
        foreach (Item original in items)
        {
            var copies = all.Where(item => item.ID == original.ID).ToList();
            Assert.Equal(original.Quantity, copies.Count);
            Assert.All(copies, item =>
            {
                Assert.Equal(1, item.Quantity);
                Assert.Equal(original.KeepUpright, item.KeepUpright);
                Assert.Equal(original.KeepLengthwise, item.KeepLengthwise);
            });
        }
        Assert.Equal(2, result.UnpackedItems.Count(item => item.ID == 5));
        Assert.All(result.PackedItems, item => AssertOrientation(item, item.KeepUpright, item.KeepLengthwise));
        AssertGeometry(container, result.PackedItems);
    }

    [Fact]
    public void NoAllowedPlacement_ReturnsEveryCopyAsUnpacked()
    {
        var result = Pack(new Container(1, 3, 5, 7), [new Item(1, 7, 5, 3, 4)],
            new PackingOptions { KeepUpright = true, KeepLengthwise = true });
        Assert.Empty(result.PackedItems);
        Assert.Equal(4, result.UnpackedItems.Count);
        Assert.False(result.IsCompletePack);
        Assert.Equal(0, result.PercentContainerVolumePacked);
        Assert.Equal(0, result.PercentItemVolumePacked);
    }

    [Fact]
    public void NullAndDisabledOptions_ProduceIdenticalLegacyPlacements()
    {
        var container = new Container(1, 19, 13, 11);
        List<Item> items = [new(1, 7, 5, 3, 5), new(2, 4, 2, 1, 7)];
        var legacy = PackingService.Pack([container], items, [(int)AlgorithmType.EB_AFIT]).Single().AlgorithmPackingResults.Single();
        var noOptions = Pack(container, items);
        var disabled = Pack(container, items, new PackingOptions { KeepUpright = false, KeepLengthwise = false });
        Assert.Equal(Signature(legacy), Signature(noOptions));
        Assert.Equal(Signature(legacy), Signature(disabled));
        Assert.Equal(legacy.PercentContainerVolumePacked, disabled.PercentContainerVolumePacked);
        Assert.Equal(legacy.PercentItemVolumePacked, disabled.PercentItemVolumePacked);
        Assert.False(items[0].KeepUpright);
        Assert.False(items[0].KeepLengthwise);
    }

    [Fact]
    public async Task ParallelCallsAndContainers_DoNotShareGlobalOptions()
    {
        List<Item> items = [new(1, 7, 5, 3, 2)];
        List<Container> containers = [new(1, 7, 10, 3), new(2, 3, 10, 7)];
        var calls = Enumerable.Range(0, 16).Select(i => Task.Run(() =>
        {
            bool restricted = i % 2 == 0;
            var results = PackingService.Pack(containers, items, [(int)AlgorithmType.EB_AFIT], new PackingOptions
            {
                KeepUpright = restricted, KeepLengthwise = restricted
            });
            foreach (var result in results)
            {
                var packed = result.AlgorithmPackingResults.Single();
                Assert.Equal(restricted && result.ContainerID == 2 ? 0 : 2, packed.PackedItems.Count);
                Assert.All(packed.PackedItems, item => AssertOrientation(item, restricted, restricted));
                AssertGeometry(containers.Single(c => c.ID == result.ContainerID), packed.PackedItems);
            }
        }));
        await Task.WhenAll(calls);
        Assert.False(items[0].KeepUpright);
        Assert.False(items[0].KeepLengthwise);
    }

    [Fact]
    public void DirectAlgorithmCalls_PreserveFlagsAndResetBetweenRuns()
    {
        var algorithm = new EB_AFIT();
        var container = new Container(1, 7, 5, 3);
        var fixedItem = new Item(1, 7, 5, 3, 1) { KeepUpright = true, KeepLengthwise = true };
        Assert.Single(algorithm.Run(container, [fixedItem]).PackedItems);
        var impossible = algorithm.Run(new Container(1, 3, 5, 7), [fixedItem]);
        Assert.Empty(impossible.PackedItems);
        Assert.Single(impossible.UnpackedItems);
        Assert.Single(algorithm.Run(new Container(1, 3, 5, 7), [new Item(1, 7, 5, 3, 1)]).PackedItems);
    }

    [Fact]
    public void Packages_MaintainFixedPhysicalDimensions()
    {
        var container = new Container(1, 13600, 2500, 2700);
        var result = Pack(container, [new Item(1, 4800, 1200, 1100, 8)], new PackingOptions
        {
            KeepUpright = true, KeepLengthwise = true
        });
        Assert.NotEmpty(result.PackedItems);
        Assert.Equal(8, result.PackedItems.Count + result.UnpackedItems.Count);
        Assert.All(result.PackedItems, item => Assert.Equal((4800m, 1100m, 1200m), (item.PackDimX, item.PackDimY, item.PackDimZ)));
        AssertGeometry(container, result.PackedItems);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void VariedMixedLoads_ReturnValidGeometryAndRespectEveryRestriction(bool globalUpright, bool globalLengthwise)
    {
        // A fixed seed exercises partial packs, repeated layer selection and
        // different internal container orientations without asserting heuristic choices.
        var random = new Random(8241);
        for (int scenario = 0; scenario < 40; scenario++)
        {
            var container = new Container(1, random.Next(6, 16), random.Next(6, 16), random.Next(6, 16));
            var items = Enumerable.Range(1, 5).Select(id => new Item(id,
                random.Next(2, 10), random.Next(2, 10), random.Next(2, 10), random.Next(1, 5))
            {
                KeepUpright = random.Next(2) == 1,
                KeepLengthwise = random.Next(2) == 1
            }).ToList();
            var result = Pack(container, items, new PackingOptions
            {
                KeepUpright = globalUpright, KeepLengthwise = globalLengthwise
            });
            foreach (Item original in items)
            {
                Assert.Equal(original.Quantity, result.PackedItems.Concat(result.UnpackedItems).Count(item => item.ID == original.ID));
                Assert.All(result.PackedItems.Where(item => item.ID == original.ID),
                    item => AssertOrientation(item, globalUpright || original.KeepUpright, globalLengthwise || original.KeepLengthwise));
            }
            AssertGeometry(container, result.PackedItems);
        }
    }

    [Theory]
    [InlineData(7, 7, 3, 3, 7, 7)]
    [InlineData(7, 3, 7, 3, 7, 7)]
    [InlineData(3, 7, 7, 7, 3, 7)]
    public void EqualDimensions_DoNotAllowForbiddenFixedAxisChanges(
        int length, int width, int height, int containerLength, int containerWidth, int containerHeight)
    {
        var container = new Container(1, containerLength, containerWidth, containerHeight);
        var item = new Item(1, length, width, height, 1);
        Assert.Single(Pack(container, [item]).PackedItems);
        var restricted = Pack(container, [item], new PackingOptions { KeepUpright = true, KeepLengthwise = true });
        Assert.Empty(restricted.PackedItems);
        Assert.Single(restricted.UnpackedItems);
    }

    private static IEnumerable<string> Signature(AlgorithmPackingResult result)
    {
        return result.PackedItems.Select(item => $"{item.ID}:{item.CoordX},{item.CoordY},{item.CoordZ}:{item.PackDimX},{item.PackDimY},{item.PackDimZ}")
            .Concat(result.UnpackedItems.Select(item => $"unpacked:{item.ID}"));
    }

    private static void AssertOrientation(Item item, bool upright, bool lengthwise)
    {
        if (upright) Assert.Equal(item.Dim3, item.PackDimY);
        if (lengthwise) Assert.Equal(item.Dim1, item.PackDimX);
        if (upright && lengthwise) Assert.Equal(item.Dim2, item.PackDimZ);
        Assert.Equal(item.Volume, item.PackDimX * item.PackDimY * item.PackDimZ);
    }

    private static void AssertGeometry(Container container, List<Item> packed)
    {
        foreach (Item item in packed)
        {
            Assert.InRange(item.CoordX, 0, container.Length - item.PackDimX);
            Assert.InRange(item.CoordY, 0, container.Height - item.PackDimY);
            Assert.InRange(item.CoordZ, 0, container.Width - item.PackDimZ);
        }
        for (int i = 0; i < packed.Count; i++)
        for (int j = i + 1; j < packed.Count; j++)
        {
            Item a = packed[i], b = packed[j];
            bool separated = a.CoordX + a.PackDimX <= b.CoordX || b.CoordX + b.PackDimX <= a.CoordX ||
                a.CoordY + a.PackDimY <= b.CoordY || b.CoordY + b.PackDimY <= a.CoordY ||
                a.CoordZ + a.PackDimZ <= b.CoordZ || b.CoordZ + b.PackDimZ <= a.CoordZ;
            Assert.True(separated, $"Items {i} and {j} overlap.");
        }
    }
}
