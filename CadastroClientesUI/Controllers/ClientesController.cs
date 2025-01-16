using CadastroClientesUI.Models;
using CadastroClientesUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CadastroClientesUI.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApiService _apiService;

        public ClientesController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var clientes = await _apiService.GetAsync<List<ClienteDTO>>("Clientes");
            return View(clientes);
        }

        public IActionResult Create()
        {
            return View(new ClienteDTO { Logradouros = new List<LogradouroDTO> { new LogradouroDTO() } });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ClienteDTO clienteDto, IFormFile logotipo)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Erro ao cadastrar o cliente. Verifique os dados e tente novamente.");
                return View(clienteDto);
            }

            try
            {
                if (logotipo != null && logotipo.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logotipos");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(logotipo.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await logotipo.CopyToAsync(stream);
                    }

                    clienteDto.Logotipo = Path.Combine("uploads", "logotipos", fileName).Replace("\\", "/");
                }

                await _apiService.PostAsync("Clientes", clienteDto);
                TempData["SuccessMessage"] = "Cliente cadastrado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao enviar os dados: {ex.Message}");
                return View(clienteDto);
            }
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID inválido.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _apiService.DeleteAsync($"clientes/{id}");
                if (result)
                {
                    TempData["SuccessMessage"] = "Cliente excluído com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir o cliente.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro interno: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _apiService.GetAsync<ClienteDTO>($"Clientes/{id}");
            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClienteDTO clienteDto)
        {
            if (!ModelState.IsValid)
            {
                return View(clienteDto);
            }
            try
            {
                var updatedCliente = await _apiService.PutAsync($"clientes/{clienteDto.Id}", clienteDto);
                TempData["SuccessMessage"] = "Cliente atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao atualizar cliente: {ex.Message}";
                return View(clienteDto);
            }
        }
    }
}
