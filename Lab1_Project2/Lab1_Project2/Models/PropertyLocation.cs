using System;

namespace EstateHub.Catalog.Models
{
    public class PropertyLocation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PropertyId { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public bool IsGeocoded { get; set; }

        public Property? Property { get; set; }
    }
}