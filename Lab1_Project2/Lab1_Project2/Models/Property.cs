using System;
using System.Collections.Generic;

namespace EstateHub.Catalog.Models
{
    public class Property
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CategoryId { get; set; }
        public Guid OwnerId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string City { get; set; } = string.Empty;
        public string Status { get; set; } = "available";

        public byte[]? RowVersion { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

        public Category? Category { get; set; }
        public PropertyLocation? Location { get; set; }
        public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
        public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    }
}