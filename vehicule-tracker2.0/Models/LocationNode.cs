namespace vehicule_tracker2._0.Models
{
    public class LocationNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int AlertRadius { get; set; } = 5;
        public int StopRadius { get; set; } = 8;
    }
}
