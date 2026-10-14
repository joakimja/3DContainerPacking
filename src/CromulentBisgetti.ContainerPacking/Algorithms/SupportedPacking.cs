using System;
using System.Collections.Generic;
using System.Linq;
using CromulentBisgetti.ContainerPacking.Entities;

namespace CromulentBisgetti.ContainerPacking.Algorithms
{
    // Internal refinement of EB-AFIT's order and orientations. All geometry here
    // uses physical X=length, Y=height, Z=width and exact decimal contact.
    internal static class SupportedPacking
    {
        private readonly record struct Size(decimal X, decimal Y, decimal Z);
        private static readonly (int X, int Y, int Z)[] permutations =
        {
            (0, 1, 2), (0, 2, 1), (1, 0, 2),
            (1, 2, 0), (2, 0, 1), (2, 1, 0)
        };

        internal static List<Item> Pack(Container container, List<Item> preferred, List<Item> all)
        {
            var remaining = preferred.Concat(all.Where(i => !i.IsPacked)).ToList();
            var sizes = remaining.ToDictionary(i => i, Orientations);
            foreach (var item in all)
            {
                item.IsPacked = false;
                item.CoordX = item.CoordY = item.CoordZ = 0;
                item.PackDimX = item.PackDimY = item.PackDimZ = 0;
            }
            var packed = new List<Item>();
            var exhaustedHeights = new HashSet<decimal>();
            while (remaining.Count > 0)
            {
                bool placed = false;
                // Search every remaining item on the floor before any higher surface.
                var heights = packed.Select(i => i.CoordY + i.PackDimY)
                    .Append(0m).Distinct().Where(y => !exhaustedHeights.Contains(y)).OrderBy(y => y).ToList();
                foreach (decimal y in heights)
                {
                    var checkedTypes = new HashSet<(decimal, decimal, decimal, bool, bool)>();
                    var supports = packed.Where(i => i.CoordY + i.PackDimY == y).ToList();
                    for (int index = 0; index < remaining.Count && !placed; index++)
                    {
                        Item item = remaining[index];
                        if (!checkedTypes.Add((item.Dim1, item.Dim2, item.Dim3, item.KeepUpright, item.KeepLengthwise)))
                            continue;
                        foreach (Size size in sizes[item])
                        {
                            if (size.X > container.Length || y + size.Y > container.Height || size.Z > container.Width)
                                continue;
                            if (y > 0 && supports.Sum(s => s.PackDimX * s.PackDimZ) < size.X * size.Z)
                                continue;
                            var xs = Coordinates(packed, size.X, container.Length, true);
                            var zs = Coordinates(packed, size.Z, container.Width, false);
                            if (y > 0)
                            {
                                decimal minX = supports.Min(s => s.CoordX), maxX = supports.Max(s => s.CoordX + s.PackDimX);
                                decimal minZ = supports.Min(s => s.CoordZ), maxZ = supports.Max(s => s.CoordZ + s.PackDimZ);
                                xs.RemoveAll(x => x < minX || x + size.X > maxX);
                                zs.RemoveAll(z => z < minZ || z + size.Z > maxZ);
                            }
                            foreach (decimal z in zs)
                            {
                                var rowSupports = supports.Where(s => z < s.CoordZ + s.PackDimZ && z + size.Z > s.CoordZ).ToList();
                                if (y > 0 && rowSupports.Sum(s => s.PackDimX *
                                    (Math.Min(z + size.Z, s.CoordZ + s.PackDimZ) - Math.Max(z, s.CoordZ))) < size.X * size.Z)
                                    continue;
                                var obstacles = packed.Where(s => y < s.CoordY + s.PackDimY && y + size.Y > s.CoordY &&
                                    z < s.CoordZ + s.PackDimZ && z + size.Z > s.CoordZ).ToList();
                                foreach (decimal x in xs)
                                {
                                    if (!CanPlace(obstacles, rowSupports, x, y, z, size)) continue;
                                    item.CoordX = x; item.CoordY = y; item.CoordZ = z;
                                    item.PackDimX = size.X; item.PackDimY = size.Y; item.PackDimZ = size.Z;
                                    item.IsPacked = true;
                                    packed.Add(item);
                                    remaining.RemoveAt(index);
                                    placed = true;
                                    break;
                                }
                                if (placed) break;
                            }
                            if (placed) break;
                        }
                    }
                    if (placed) break;
                    // Later placements are above this exhausted height. They cannot
                    // create new support or free space here; remaining items only decrease.
                    exhaustedHeights.Add(y);
                }
                if (!placed) break;
            }
            return packed;
        }

        private static List<Size> Orientations(Item item)
        {
            var result = new List<Size>();
            if (item.IsPacked) result.Add(new Size(item.PackDimX, item.PackDimY, item.PackDimZ));
            decimal[] dims = { item.Dim1, item.Dim2, item.Dim3 };
            foreach (var p in permutations)
            {
                if (item.KeepLengthwise && p.X != 0 || item.KeepUpright && p.Y != 2) continue;
                var size = new Size(dims[p.X], dims[p.Y], dims[p.Z]);
                if (size.X > 0 && size.Y > 0 && size.Z > 0 && !result.Contains(size)) result.Add(size);
            }
            return result;
        }

        private static List<decimal> Coordinates(List<Item> packed, decimal size, decimal limit, bool alongX)
        {
            var coordinates = new HashSet<decimal> { 0, limit - size };
            foreach (Item item in packed)
            {
                decimal start = alongX ? item.CoordX : item.CoordZ;
                decimal end = start + (alongX ? item.PackDimX : item.PackDimZ);
                coordinates.Add(start); coordinates.Add(end);
                coordinates.Add(start - size); coordinates.Add(end - size);
            }
            return coordinates.Where(c => c >= 0 && c + size <= limit).OrderBy(c => c).ToList();
        }

        private static bool CanPlace(List<Item> obstacles, List<Item> supports, decimal x, decimal y, decimal z, Size size)
        {
            // Obstacles already intersect the candidate's vertical and Z intervals.
            foreach (Item other in obstacles)
                if (x < other.CoordX + other.PackDimX && x + size.X > other.CoordX) return false;
            if (y > 0)
            {
                decimal support = 0;
                foreach (Item other in supports)
                {
                    decimal overlapX = Math.Min(x + size.X, other.CoordX + other.PackDimX) - Math.Max(x, other.CoordX);
                    decimal overlapZ = Math.Min(z + size.Z, other.CoordZ + other.PackDimZ) - Math.Max(z, other.CoordZ);
                    // Accepted boxes never overlap, so these intersection areas are disjoint.
                    if (overlapX > 0 && overlapZ > 0) support += overlapX * overlapZ;
                }
                if (support != size.X * size.Z) return false;
            }
            return true;
        }
    }
}
