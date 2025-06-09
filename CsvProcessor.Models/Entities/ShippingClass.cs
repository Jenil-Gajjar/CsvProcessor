using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("shipping_classes")]
[Index("ClassName", Name = "shipping_classes_class_name_key", IsUnique = true)]
public partial class ShippingClass
{
    [Key]
    [Column("shipping_class_id")]
    public int ShippingClassId { get; set; }

    [Column("class_name")]
    [StringLength(100)]
    public string ClassName { get; set; } = null!;

    [InverseProperty("ShippingClass")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
