using CadastroClientesAPI.Data;
using CadastroClientesAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CadastroClientesAPI.Controllers
{
    [Route("api/Logradouros")]
    [ApiController]
    // Logradouro é PII (endereço residencial). Nenhum endereço sai daqui sem autenticação.
    [Authorize]
    public class LogradourosController : ControllerBase
    {
        // Teto de página: a listagem devolvia a tabela inteira de endereços numa única resposta.
        private const int MaxPageSize = 100;

        private readonly AppDbContext _context;

        public LogradourosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Logradouros?page=1&pageSize=50
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Logradouro>>> GetLogradouros([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            return await _context.Logradouros
                .OrderBy(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
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
