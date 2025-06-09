using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessor.DAL;

[Table("products")]
[Index("Sku", Name = "products_sku_key", IsUnique = true)]
public partial class Product
{
    [Key]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("sku")]
    [StringLength(100)]
    public string Sku { get; set; } = null!;

    [Column("name")]
    [StringLength(150)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("base_price")]
    [Precision(10, 2)]
    public decimal BasePrice { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Column("brand_id")]
    public int? BrandId { get; set; }

    [Column("supplier_sku")]
    [StringLength(100)]
    public string? SupplierSku { get; set; }

    [Column("weight_kg")]
    [Precision(6, 2)]
    public decimal? WeightKg { get; set; }

    [Column("dimensions_cm")]
    [StringLength(50)]
    public string? DimensionsCm { get; set; }

    [Column("shipping_class_id")]
    public int? ShippingClassId { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("BrandId")]
    [InverseProperty("Products")]
    public virtual Brand? Brand { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Products")]
    public virtual Category? Category { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    [InverseProperty("Product")]
    public virtual ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();

    [InverseProperty("Product")]
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    [InverseProperty("Product")]
    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

    [ForeignKey("ShippingClassId")]
    [InverseProperty("Products")]
    public virtual ShippingClass? ShippingClass { get; set; }
}
