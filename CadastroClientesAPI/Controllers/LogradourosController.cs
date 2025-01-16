using CadastroClientesAPI.Data;
using CadastroClientesAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CadastroClientesAPI.Controllers
{
    [Route("api/Logradouros")]
    [ApiController]
    public class LogradourosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LogradourosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Logradouros
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Logradouro>>> GetLogradouros()
        {
            return await _context.Logradouros.ToListAsync();
        }

        // GET: api/Logradouros/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Logradouro>> GetLogradouro(int id)
        {
            var logradouro = await _context.Logradouros.FindAsync(id);

            if (logradouro == null)
            {
                return NotFound();
            }

            return logradouro;
        }

        // POST: api/Logradouros
        [HttpPost]
        public async Task<ActionResult<Logradouro>> CreateLogradouro(Logradouro logradouro)
        {
            _context.Logradouros.Add(logradouro);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLogradouro), new { id = logradouro.Id }, logradouro);
        }

        // PUT: api/Logradouros/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLogradouro(int id, Logradouro logradouro)
        {
            if (id != logradouro.Id)
            {
                return BadRequest();
            }

            _context.Entry(logradouro).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LogradouroExists(id))
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

        // DELETE: api/Logradouros/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLogradouro(int id)
        {
            var logradouro = await _context.Logradouros.FindAsync(id);
            if (logradouro == null)
            {
                return NotFound();
            }

            _context.Logradouros.Remove(logradouro);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LogradouroExists(int id)
        {
            return _context.Logradouros.Any(e => e.Id == id);
        }
    }
}
