using System;
using System.Collections.Generic;

namespace EstateHub.Catalog.Models
{
    public class Amenity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}