using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("product_variants")]
[Index("ProductId", "VariantTypeId", "VariantValue", Name = "product_variants_product_id_variant_type_id_variant_value_key", IsUnique = true)]
public partial class ProductVariant
{
    [Key]
    [Column("variant_id")]
    public int VariantId { get; set; }

    [Column("product_id")]
    public int? ProductId { get; set; }

    [Column("variant_type_id")]
    public int? VariantTypeId { get; set; }

    [Column("variant_value")]
    [StringLength(100)]
    public string VariantValue { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("ProductVariants")]
    public virtual Product? Product { get; set; }

    [ForeignKey("VariantTypeId")]
    [InverseProperty("ProductVariants")]
    public virtual VariantType? VariantType { get; set; }
}
