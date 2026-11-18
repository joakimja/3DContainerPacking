using System.Reflection;
using CromulentBisgetti.ContainerPacking.Algorithms;
using CromulentBisgetti.ContainerPacking.Entities;

namespace CromulentBisgetti.ContainerPacking.Tests;

public class SettlementTests
{
    [Fact]
    public void SettlesUnevenLayerAndItemsAboveIt()
    {
        Item Box(int id, decimal x, decimal y, decimal z, decimal dx, decimal dy, decimal dz) =>
            new Item(id, dx, dy, dz, 1) { CoordX = x, CoordY = y, CoordZ = z, PackDimX = dx, PackDimY = dy, PackDimZ = dz };

        var bottom = Box(1, 0, 0, 0, 5.4m, 0.944m, 1.35m);
        var adjacent = Box(3, 0, 0, 1.35m, 3.6m, 1.113m, 1m);
        var upper = Box(1, 0, 1.113m, 0, 5.4m, 0.944m, 1.35m);
        var top = Box(4, 0, 2.057m, 0, 1m, 1m, 1m);
        var items = new List<Item> { top, bottom, adjacent, upper };

        typeof(EB_AFIT).GetMethod("SettleItems", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [items]);

        Assert.Equal(0.944m, upper.CoordY);
        Assert.Equal(1.888m, top.CoordY);
        Assert.Equal(0m, adjacent.CoordY);
    }
}
