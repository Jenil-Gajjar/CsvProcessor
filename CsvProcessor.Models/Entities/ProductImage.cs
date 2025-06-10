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
    public int ProductId { get; set; }

    [Column("image_path")]
    public string ImagePath { get; set; } = null!;

    [Column("is_primary")]
    public bool? IsPrimary { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductImages")]
    public virtual Product Product { get; set; } = null!;
}
