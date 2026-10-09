namespace CromulentBisgetti.ContainerPacking.Entities
{
    /// <summary>Orientation restrictions applied to every item in a packing call.</summary>
    public class PackingOptions
    {
        /// <summary>Keep the original item height (Dim3) on the physical Y axis.</summary>
        public bool KeepUpright { get; set; }

        /// <summary>Keep the original item length (Dim1) on the physical X axis.</summary>
        public bool KeepLengthwise { get; set; }
    }
}
