namespace CromulentBisgetti.ContainerPacking.Entities
{
    /// <summary>Global packing rules and orientation restrictions for a packing call.</summary>
    public class PackingOptions
    {
        /// <summary>Keep the original item height (Dim3) on the physical Y axis.</summary>
        public bool KeepUpright { get; set; }

        /// <summary>Keep the original item length (Dim1) on the physical X axis.</summary>
        public bool KeepLengthwise { get; set; }

        /// <summary>Pack floor first and require full support beneath every item. Default is false.</summary>
        public bool StablePacking { get; set; }
    }
}
