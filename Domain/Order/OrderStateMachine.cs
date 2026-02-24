using Domain.Order.Exceptions;

namespace Domain.Order;

/// <summary>
/// State Machine رسمی برای چرخه حیات سفارش.
/// هر انتقال وضعیت توسط Guard‌های صریح محافظت می‌شود.
/// این کلاس تنها مرجع معتبر برای انتقال وضعیت است.
/// </summary>
public static class OrderStateMachine
{
    
    public static class States
    {
        public const string Created = "Created";    
        public const string Reserved = "Reserved";   
        public const string Pending = "Pending";    
        public const string Paid = "Paid";       
        public const string Processing = "Processing"; 
        public const string Shipped = "Shipped";    
        public const string Delivered = "Delivered";  
        public const string Cancelled = "Cancelled";  
        public const string Failed = "Failed";     
        public const string Expired = "Expired";    
        public const string Refunded = "Refunded";   
        public const string Returned = "Returned";   
    }

    
    public static class Triggers
    {
        public const string ReserveStock = "ReserveStock";
        public const string InitiatePayment = "InitiatePayment";
        public const string ConfirmPayment = "ConfirmPayment";
        public const string FailPayment = "FailPayment";
        public const string StartProcessing = "StartProcessing";
        public const string Ship = "Ship";
        public const string Deliver = "Deliver";
        public const string Cancel = "Cancel";
        public const string Expire = "Expire";
        public const string RequestRefund = "RequestRefund";
        public const string MarkReturned = "MarkReturned";
    }

    
    private static readonly Dictionary<string, List<TransitionRule>> _transitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [States.Created] =
            [
                new(Triggers.ReserveStock,    States.Reserved,   Guards.CanReserveStock),
                new(Triggers.Cancel,          States.Cancelled,  Guards.CanCancel),
                new(Triggers.Expire,          States.Expired,    Guards.CanExpire),
            ],
            [States.Reserved] =
            [
                new(Triggers.InitiatePayment, States.Pending,    Guards.CanInitiatePayment),
                new(Triggers.Cancel,          States.Cancelled,  Guards.CanCancel),
                new(Triggers.Expire,          States.Expired,    Guards.CanExpire),
            ],
            [States.Pending] =
            [
                new(Triggers.ConfirmPayment,  States.Paid,       Guards.CanConfirmPayment),
                new(Triggers.FailPayment,     States.Failed,     Guards.AlwaysAllow),
                new(Triggers.Cancel,          States.Cancelled,  Guards.CanCancel),
                new(Triggers.Expire,          States.Expired,    Guards.CanExpire),
            ],
            [States.Failed] =
            [
                new(Triggers.InitiatePayment, States.Pending,    Guards.CanRetryPayment),
                new(Triggers.Cancel,          States.Cancelled,  Guards.CanCancel),
                new(Triggers.Expire,          States.Expired,    Guards.CanExpire),
            ],
            [States.Paid] =
            [
                new(Triggers.StartProcessing, States.Processing, Guards.AlwaysAllow),
                new(Triggers.RequestRefund,   States.Refunded,   Guards.CanRefund),
            ],
            [States.Processing] =
            [
                new(Triggers.Ship,            States.Shipped,    Guards.AlwaysAllow),
                new(Triggers.Cancel,          States.Cancelled,  Guards.CanCancelAfterPaid),
            ],
            [States.Shipped] =
            [
                new(Triggers.Deliver,         States.Delivered,  Guards.AlwaysAllow),
                new(Triggers.MarkReturned,    States.Returned,   Guards.AlwaysAllow),
            ],
            [States.Delivered] =
            [
                new(Triggers.RequestRefund,   States.Refunded,   Guards.CanRefund),
                new(Triggers.MarkReturned,    States.Returned,   Guards.AlwaysAllow),
            ],
            [States.Returned] =
            [
                new(Triggers.RequestRefund,   States.Refunded,   Guards.AlwaysAllow),
            ],
        };

    

    /// <summary>
    /// بررسی امکان انتقال از وضعیت فعلی به وضعیت مقصد.
    /// </summary>
    public static bool CanTransition(string currentState, string trigger, Order order)
    {
        if (!_transitions.TryGetValue(currentState, out var rules))
            return false;

        var rule = rules.FirstOrDefault(r =>
            r.Trigger.Equals(trigger, StringComparison.OrdinalIgnoreCase));

        return rule != null && rule.Guard(order);
    }

    /// <summary>
    /// اجرای انتقال وضعیت. در صورت عدم امکان، استثنا پرتاب می‌کند.
    /// </summary>
    public static string Transition(string currentState, string trigger, Order order)
    {
        if (!_transitions.TryGetValue(currentState, out var rules))
            throw new InvalidOrderStateException(order.Id, currentState, trigger);

        var rule = rules.FirstOrDefault(r =>
            r.Trigger.Equals(trigger, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOrderStateException(order.Id, currentState, trigger);

        if (!rule.Guard(order))
            throw new InvalidOrderStateException(order.Id, currentState, trigger);

        return rule.TargetState;
    }

    /// <summary>
    /// لیست تریگرهای مجاز از وضعیت فعلی.
    /// </summary>
    public static IEnumerable<string> GetPermittedTriggers(string currentState, Order order)
    {
        if (!_transitions.TryGetValue(currentState, out var rules))
            return Enumerable.Empty<string>();

        return rules.Where(r => r.Guard(order)).Select(r => r.Trigger);
    }

    /// <summary>
    /// آیا وضعیت نهایی است (بدون انتقال بعدی)?
    /// </summary>
    public static bool IsFinalState(string state) =>
        state is States.Delivered or States.Cancelled or
                 States.Expired or States.Refunded;

    
    private static class Guards
    {
        public static readonly Func<Order, bool> AlwaysAllow = _ => true;

        public static readonly Func<Order, bool> CanReserveStock = order =>
            !order.IsDeleted && order.HasItems();

        public static readonly Func<Order, bool> CanInitiatePayment = order =>
            !order.IsDeleted && order.HasItems() && order.FinalAmount.Amount > 0;

        public static readonly Func<Order, bool> CanRetryPayment = order =>
            !order.IsDeleted && order.HasItems();

        public static readonly Func<Order, bool> CanConfirmPayment = order =>
            !order.IsDeleted && order.FinalAmount.Amount > 0;

        public static readonly Func<Order, bool> CanCancel = order =>
            !order.IsDeleted && !order.IsShipped && !order.IsDelivered;

        public static readonly Func<Order, bool> CanCancelAfterPaid = order =>
            !order.IsDeleted; 

        public static readonly Func<Order, bool> CanRefund = order =>
            !order.IsDeleted && (order.IsPaid || order.IsDelivered);

        public static readonly Func<Order, bool> CanExpire = order =>
            !order.IsDeleted && !order.IsPaid;
    }

    
    private record TransitionRule(
        string Trigger,
        string TargetState,
        Func<Order, bool> Guard);
}