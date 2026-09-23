using Application.Order.Sagas.State;
using Domain.Order.Enums;
using Domain.Order.ValueObjects;

namespace Tests.Application.Order.Sagas.State;

public class OrderProcessStateTests
{
    [Fact]
    public void Create_InitializesCreatedInProgress()
    {
        var orderId = OrderId.NewId();

        var state = OrderProcessState.Create(orderId, "corr-1");

        state.Id.ShouldNotBe(Guid.Empty);
        state.OrderId.ShouldBe(orderId);
        state.CurrentStep.ShouldBe(ProcessStepEnum.Created);
        state.Status.ShouldBe(ProcessStatusEnum.InProgress);
        state.FailureReason.ShouldBeNull();
        state.RetryCount.ShouldBe(0);
        state.CorrelationId.ShouldBe("corr-1");
    }

    [Fact]
    public void Create_WithoutCorrelationId_LeavesItNull()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void TransitionTo_UpdatesStepOnly()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.TransitionTo(ProcessStepEnum.InventoryReserving);

        state.CurrentStep.ShouldBe(ProcessStepEnum.InventoryReserving);
        state.Status.ShouldBe(ProcessStatusEnum.InProgress);
    }

    [Fact]
    public void MarkCompleted_SetsCompletedStepAndStatus()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.MarkCompleted();

        state.CurrentStep.ShouldBe(ProcessStepEnum.Completed);
        state.Status.ShouldBe(ProcessStatusEnum.Completed);
    }

    [Fact]
    public void MarkFailed_SetsFailedWithReason()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.MarkFailed("boom");

        state.CurrentStep.ShouldBe(ProcessStepEnum.Failed);
        state.Status.ShouldBe(ProcessStatusEnum.Failed);
        state.FailureReason.ShouldBe("boom");
    }

    [Fact]
    public void MarkCompensating_SetsCompensating()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.MarkCompensating();

        state.CurrentStep.ShouldBe(ProcessStepEnum.Compensating);
        state.Status.ShouldBe(ProcessStatusEnum.Compensating);
    }

    [Fact]
    public void MarkCompensated_SetsCompensated()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.MarkCompensated();

        state.CurrentStep.ShouldBe(ProcessStepEnum.Compensated);
        state.Status.ShouldBe(ProcessStatusEnum.Compensated);
    }

    [Fact]
    public void MarkRefunded_SetsRefundedStepWithCompensatedStatus()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.MarkRefunded();

        state.CurrentStep.ShouldBe(ProcessStepEnum.Refunded);
        state.Status.ShouldBe(ProcessStatusEnum.Compensated);
    }

    [Fact]
    public void MarkRequiresManualReconciliation_SetsFailedWithReason()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.MarkRequiresManualReconciliation("manual");

        state.CurrentStep.ShouldBe(ProcessStepEnum.RequiresManualReconciliation);
        state.Status.ShouldBe(ProcessStatusEnum.Failed);
        state.FailureReason.ShouldBe("manual");
    }

    [Fact]
    public void IncrementRetry_IncrementsCount()
    {
        var state = OrderProcessState.Create(OrderId.NewId());

        state.IncrementRetry();
        state.IncrementRetry();

        state.RetryCount.ShouldBe(2);
    }
}
