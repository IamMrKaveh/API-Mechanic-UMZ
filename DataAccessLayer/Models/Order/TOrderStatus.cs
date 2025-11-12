namespace DataAccessLayer.Models.Order;

[Index(nameof(Name))]
public class TOrderStatus
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام وضعیت الزامی است")]
    [MaxLength(100, ErrorMessage = "نام وضعیت نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "آیکون نمی‌تواند بیشتر از 500 کاراکتر باشد")]
    public string? Icon { get; set; }

    public virtual ICollection<TOrders>? Orders { get; set; }
}