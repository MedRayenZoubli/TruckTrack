namespace vehicule_tracker2._0.Models
{

    public class Vehicle
    {
        public string Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime StatusChangedAt { get; set; }
        public double DistanceToNearestNode { get; set; }
        public string NearestNodeId { get; set; }
        public string NearestNodeName { get; set; }
    }

}
