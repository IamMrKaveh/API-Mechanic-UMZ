namespace DataAccessLayer.Models.Order;

[Index(nameof(Name))]
public class TShippingMethod
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام روش ارسال الزامی است")]
    [StringLength(100, ErrorMessage = "نام روش ارسال نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(19,4)")]
    [Range(0, double.MaxValue, ErrorMessage = "هزینه ارسال نمی‌تواند منفی باشد")]
    public decimal Cost { get; set; }

    public virtual ICollection<TOrders>? Orders { get; set; }
}