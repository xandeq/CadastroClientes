using CadastroClientesAPI.Data;
using CadastroClientesAPI.DTOs;
using CadastroClientesAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CadastroClientesAPI.Controllers
{
    [Route("api/Clientes")]
    [ApiController]
    // Todos os endpoints deste controller expõem PII (nome, e-mail, endereço).
    // [Authorize] no nível da classe evita que um endpoint novo nasça anônimo por esquecimento.
    [Authorize]
    public class ClientesController : ControllerBase
    {
        // Teto de página: sem isso, ?pageSize=2147483647 extrai a base inteira numa requisição.
        private const int MaxPageSize = 100;

        private readonly AppDbContext _context;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(AppDbContext context, ILogger<ClientesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDTO>>> GetClientes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var clientes = await _context.Clientes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClienteDTO
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Logradouros = c.Logradouros.Select(l => new LogradouroDTO
                    {
                        Endereco = l.Endereco,
                        Cidade = l.Cidade,
                        Estado = l.Estado,
                        CEP = l.CEP
                    }).ToList()
                })
                .ToListAsync();

            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteDTO>> GetCliente(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Logradouros)
                .Where(c => c.Id == id)
                .Select(c => new ClienteDTO
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Logradouros = c.Logradouros.Select(l => new LogradouroDTO
                    {
                        Endereco = l.Endereco,
                        Cidade = l.Cidade,
                        Estado = l.Estado,
                        CEP = l.CEP
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound(new { Message = "Cliente não encontrado." });
            }

            return Ok(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCliente([FromBody] ClienteDTO clienteDto)
        {
            try
            {
                if (_context.Clientes.Any(c => c.Email == clienteDto.Email))
                {
                    return Conflict(new { Message = "Já existe um cliente com este e-mail." });
                }

                var cliente = new Cliente
                {
                    Nome = clienteDto.Nome,
                    Email = clienteDto.Email,
                    Logradouros = clienteDto.Logradouros.Select(l => new Logradouro
                    {
                        Endereco = l.Endereco,
                        Cidade = l.Cidade,
                        Estado = l.Estado,
                        CEP = l.CEP
                    }).ToList()
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, new ClienteDTO
                {
                    Id = cliente.Id,
                    Nome = cliente.Nome,
                    Email = cliente.Email,
                    Logradouros = cliente.Logradouros.Select(l => new LogradouroDTO
                    {
                        Endereco = l.Endereco,
                        Cidade = l.Cidade,
                        Estado = l.Estado,
                        CEP = l.CEP
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                // Nunca devolver ex.Message ao cliente: vaza schema, caminhos e detalhes de infraestrutura.
                var correlationId = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro ao criar cliente. CorrelationId={CorrelationId}", correlationId);
                return StatusCode(500, new { Message = "Erro ao criar cliente.", CorrelationId = correlationId });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCliente(int id, [FromBody] ClienteDTO clienteDto)
        {
            if (id != clienteDto.Id)
            {
                return BadRequest(new { Message = "O ID fornecido não corresponde ao cliente." });
            }

            try
            {
                if (_context.Clientes.Any(c => c.Email == clienteDto.Email && c.Id != id))
                {
                    return Conflict(new { Message = "Outro cliente com o mesmo e-mail já existe." });
                }

                var cliente = await _context.Clientes.Include(c => c.Logradouros).FirstOrDefaultAsync(c => c.Id == id);
                if (cliente == null)
                {
                    return NotFound(new { Message = "Cliente não encontrado." });
                }

                cliente.Nome = clienteDto.Nome;
                cliente.Email = clienteDto.Email;
                cliente.Logradouros = clienteDto.Logradouros.Select(l => new Logradouro
                {
                    Endereco = l.Endereco,
                    Cidade = l.Cidade,
                    Estado = l.Estado,
                    CEP = l.CEP
                }).ToList();

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(id))
                {
                    return NotFound(new { Message = "Cliente não encontrado." });
                }

                return StatusCode(500, new { Message = "Erro de concorrência ao atualizar cliente." });
            }
            catch (Exception ex)
            {
                var correlationId = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro ao atualizar cliente {ClienteId}. CorrelationId={CorrelationId}", id, correlationId);
                return StatusCode(500, new { Message = "Erro ao atualizar cliente.", CorrelationId = correlationId });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            try
            {
                var cliente = await _context.Clientes.Include(c => c.Logradouros).FirstOrDefaultAsync(c => c.Id == id);
                if (cliente == null)
                {
                    return NotFound(new { Message = "Cliente não encontrado." });
                }

                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var correlationId = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro ao excluir cliente {ClienteId}. CorrelationId={CorrelationId}", id, correlationId);
                return StatusCode(500, new { Message = "Erro ao excluir cliente.", CorrelationId = correlationId });
            }
        }

        private bool ClienteExists(int id) => _context.Clientes.Any(e => e.Id == id);

    }
}
