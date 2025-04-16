using CromulentBisgetti.ContainerPacking.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CromulentBisgetti.ContainerPacking.Tests
{
    public class TestCase
    {
        public int TestId { get; set; }
        public string RefId { get; set; }
        public decimal TotalItems { get; set; }
        public decimal PackedItems { get; set; }
        public decimal PercentContainerVolumePacked { get; set; }
        public decimal PercentItemVolumePacked { get; set; }
        public string Flag { get; set; }
        public Container Container { get; set; }
        public List<Item> Items { get; set; } = new();
    }

    //public class Container
    //{
    //    public decimal Length { get; set; }
    //    public decimal Width { get; set; }
    //    public decimal Height { get; set; }

    //    public Container(decimal length, decimal width, decimal height)
    //    {
    //        Length = length;
    //        Width = width;
    //        Height = height;
    //    }
    //}

    //public class Item
    //{
    //    public int Id { get; set; }
    //    public decimal Length { get; set; }
    //    public decimal Width { get; set; }
    //    public decimal Height { get; set; }
    //    public int Quantity { get; set; }

    //    public Item(int id, decimal length, decimal width, decimal height, int quantity)
    //    {
    //        Id = id;
    //        Length = length;
    //        Width = width;
    //        Height = height;
    //        Quantity = quantity;
    //    }
    //}

    public class TestCaseReader
    {
        public static List<TestCase> ReadTestCases(string filePath)
        {
            var testCases = new List<TestCase>();

            using var reader = new StreamReader(filePath);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var header = line.Split(' ');
                int testId = int.Parse(header[0]);
                string refId = header[1];

                var resultLine = reader.ReadLine()?.Split(' ')!;
                decimal totalItems = decimal.Parse(resultLine[1]);
                decimal packedItems = decimal.Parse(resultLine[2]);
                decimal percentVolPacked = decimal.Parse(resultLine[3], CultureInfo.InvariantCulture);
                decimal percentItemVolPacked = decimal.Parse(resultLine[4], CultureInfo.InvariantCulture);
                string flag = resultLine[5];

                var containerDims = reader.ReadLine()?.Split(' ')!;
                var container = new Container(0,
                    decimal.Parse(containerDims[0]),
                    decimal.Parse(containerDims[1]),
                    decimal.Parse(containerDims[2])
                );

                int itemTypeCount = int.Parse(reader.ReadLine()!);
                var items = new List<Item>();

                for (int i = 0; i < itemTypeCount; i++)
                {
                    var itemData = reader.ReadLine()?.Split(' ')!;
                    var item = new Item(
                        id: int.Parse(itemData[0]),
                        dim1: decimal.Parse(itemData[1]),
                        dim2: decimal.Parse(itemData[3]),
                        dim3: decimal.Parse(itemData[5]),
                        quantity: int.Parse(itemData[7])
                    );
                    items.Add(item);
                }

                testCases.Add(new TestCase
                {
                    TestId = testId,
                    RefId = refId,
                    TotalItems = totalItems,
                    PackedItems = packedItems,
                    PercentContainerVolumePacked = percentVolPacked,
                    PercentItemVolumePacked = percentItemVolPacked,
                    Flag = flag,
                    Container = container,
                    Items = items
                });
            }

            return testCases;
        }
    }

}
