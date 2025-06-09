using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("product_images")]
public partial class ProductImage
{
    [Key]
    [Column("image_id")]
    public int ImageId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("image_url")]
    public string ImageUrl { get; set; } = null!;

    [Column("is_primary")]
    public bool? IsPrimary { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductImages")]
    public virtual Product? Product { get; set; }
}
