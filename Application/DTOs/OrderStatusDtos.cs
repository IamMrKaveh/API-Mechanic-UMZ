namespace Application.DTOs;

public class CreateOrderStatusDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
}