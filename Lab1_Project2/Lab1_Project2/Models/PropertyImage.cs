using System;

namespace EstateHub.Catalog.Models
{
    public class PropertyImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PropertyId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }

        public Property? Property { get; set; }
    }
}