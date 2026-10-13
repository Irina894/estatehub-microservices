
using System;
using System.Collections.Generic;

namespace EstateHub.Catalog.Models
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }

        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}