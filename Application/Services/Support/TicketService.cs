using Application.Common.Interfaces.Persistence.Support;

namespace Application.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public TicketService(ITicketRepository repository, IUnitOfWork unitOfWork, IHtmlSanitizer htmlSanitizer)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<ServiceResult<List<TicketDto>>> GetUserTicketsAsync(int userId)
    {
        var tickets = await _repository.GetByUserIdAsync(userId);
        var dtos = tickets.Select(t => new TicketDto
        {
            Id = t.Id,
            Subject = t.Subject,
            Status = t.Status,
            Priority = t.Priority,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        return ServiceResult<List<TicketDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<TicketDetailDto>> GetTicketDetailsAsync(int userId, int ticketId)
    {
        var ticket = await _repository.GetByIdAsync(ticketId, userId);
        if (ticket == null) return ServiceResult<TicketDetailDto>.Fail("Ticket not found");

        var dto = new TicketDetailDto
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            Messages = ticket.Messages.OrderBy(m => m.CreatedAt).Select(m => new TicketMessageDto
            {
                Id = m.Id,
                Message = m.Message,
                IsAdminResponse = m.IsAdminResponse,
                CreatedAt = m.CreatedAt
            }).ToList()
        };

        return ServiceResult<TicketDetailDto>.Ok(dto);
    }

    public async Task<ServiceResult<TicketDto>> CreateTicketAsync(int userId, CreateTicketDto dto)
    {
        var ticket = new Domain.Support.Ticket
        {
            UserId = userId,
            Subject = _htmlSanitizer.Sanitize(dto.Subject),
            Priority = dto.Priority,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        var message = new Domain.Support.TicketMessage
        {
            SenderId = userId,
            Message = _htmlSanitizer.Sanitize(dto.Message),
            IsAdminResponse = false,
            CreatedAt = DateTime.UtcNow
        };

        ticket.Messages.Add(message);
        await _repository.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = new TicketDto
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedAt = ticket.CreatedAt
        };

        return ServiceResult<TicketDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> AddMessageAsync(int userId, int ticketId, AddTicketMessageDto dto)
    {
        var ticket = await _repository.GetByIdAsync(ticketId, userId);
        if (ticket == null) return ServiceResult.Fail("Ticket not found");

        var message = new Domain.Support.TicketMessage
        {
            TicketId = ticketId,
            SenderId = userId,
            Message = _htmlSanitizer.Sanitize(dto.Message),
            IsAdminResponse = false,
            CreatedAt = DateTime.UtcNow
        };

        ticket.Status = "UserReplied";
        ticket.UpdatedAt = DateTime.UtcNow;

        await _repository.AddMessageAsync(message);
        _repository.Update(ticket);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }
}