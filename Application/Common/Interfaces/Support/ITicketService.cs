namespace Application.Common.Interfaces;

public interface ITicketService
{
    Task<ServiceResult<List<TicketDto>>> GetUserTicketsAsync(int userId);
    Task<ServiceResult<TicketDetailDto>> GetTicketDetailsAsync(int userId, int ticketId);
    Task<ServiceResult<TicketDto>> CreateTicketAsync(int userId, CreateTicketDto dto);
    Task<ServiceResult> AddMessageAsync(int userId, int ticketId, AddTicketMessageDto dto);
}