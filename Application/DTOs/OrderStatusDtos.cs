namespace Application.DTOs;

public class CreateOrderStatusDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(50)]
    public string? Icon { get; set; }
}

public class UpdateOrderStatusDto
{
    public string? Name { get; set; }

    public string? Icon { get; set; }

    public string? RowVersion { get; set; }
}