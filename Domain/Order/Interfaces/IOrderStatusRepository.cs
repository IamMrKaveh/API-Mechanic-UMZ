using Domain.Order.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Order.Interfaces;

public interface IOrderStatusRepository
{
    Task<OrderStatus?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<OrderStatus>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<OrderStatus>> GetActiveStatusesAsync(CancellationToken ct = default);

    Task AddAsync(OrderStatus orderStatus, CancellationToken ct = default);

    void Update(OrderStatus orderStatus);

    void Remove(OrderStatus orderStatus);
}