using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("variant_types")]
[Index("TypeName", Name = "variant_types_type_name_key", IsUnique = true)]
public partial class VariantType
{
    [Key]
    [Column("variant_type_id")]
    public int VariantTypeId { get; set; }

    [Column("type_name")]
    [StringLength(100)]
    public string TypeName { get; set; } = null!;

    [InverseProperty("VariantType")]
    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
