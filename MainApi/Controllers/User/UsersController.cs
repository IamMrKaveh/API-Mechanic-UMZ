namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly MechanicContext _context;

    public UsersController(MechanicContext context)
    {
        _context = context;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TUsers>>> GetTUsers()
    {
        return await _context.TUsers.ToListAsync();
    }

    // GET: api/Users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TUsers>> GetTUsers(int id)
    {
        var tUsers = await _context.TUsers.FindAsync(id);

        if (tUsers == null)
        {
            return NotFound();
        }

        return tUsers;
    }

    // PUT: api/Users/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTUsers(int id, TUsers tUsers)
    {
        if (id != tUsers.Id)
        {
            return BadRequest();
        }

        _context.Entry(tUsers).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TUsersExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Users
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TUsers>> PostTUsers(TUsers tUsers)
    {
        _context.TUsers.Add(tUsers);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTUsers", new { id = tUsers.Id }, tUsers);
    }

    // DELETE: api/Users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTUsers(int id)
    {
        var tUsers = await _context.TUsers.FindAsync(id);
        if (tUsers == null)
        {
            return NotFound();
        }

        _context.TUsers.Remove(tUsers);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TUsersExists(int id)
    {
        return _context.TUsers.Any(e => e.Id == id);
    }
}
