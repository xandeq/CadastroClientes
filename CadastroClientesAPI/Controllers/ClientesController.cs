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
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClienteDTO>>> GetClientes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
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
                return StatusCode(500, new { Message = "Erro ao criar cliente.", Details = ex.Message });
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
                return StatusCode(500, new { Message = "Erro ao atualizar cliente.", Details = ex.Message });
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
                return StatusCode(500, new { Message = "Erro ao excluir cliente.", Details = ex.Message });
            }
        }

        private bool ClienteExists(int id) => _context.Clientes.Any(e => e.Id == id);

    }
}
