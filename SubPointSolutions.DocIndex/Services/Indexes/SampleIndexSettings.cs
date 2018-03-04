namespace SubPointSolutions.DocIndex.Services.Indexes
{
    public class SampleIndexSettings
    {
        public SampleIndexSettings()
        {
            Resursive = true;
        }

        public string ContentSourceFolderPath { get; set; }
        public string ContentDestinationFolderPath { get; set; }

        public bool Resursive { get; set; }
    }
}
