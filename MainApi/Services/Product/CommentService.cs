namespace MainApi.Services.Product
{
    public class CommentService : ICommentService
    {
        private readonly MechanicContext _context;
        private readonly ILogger<CommentService> _logger;

        public CommentService(MechanicContext context, ILogger<CommentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Implement comment-related service methods here
    }
}