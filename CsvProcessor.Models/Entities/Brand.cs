using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("brands")]
[Index("BrandName", Name = "brands_brand_name_key", IsUnique = true)]
public partial class Brand
{
    [Key]
    [Column("brand_id")]
    public int BrandId { get; set; }

    [Column("brand_name")]
    [StringLength(100)]
    public string BrandName { get; set; } = null!;

    [InverseProperty("Brand")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
