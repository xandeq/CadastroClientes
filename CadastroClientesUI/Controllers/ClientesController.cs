using CadastroClientesUI.Models;
using CadastroClientesUI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CadastroClientesUI.Controllers
{
    public class ClientesController : Controller
    {
        // Upload de logotipo: teto de tamanho + allowlist de extensao + conferencia de
        // magic bytes. O diretorio wwwroot/uploads e servido por UseStaticFiles, entao um
        // .html/.svg/.js aceito aqui virava XSS armazenado na mesma origem da aplicacao.
        private const long MaxLogotipoBytes = 2 * 1024 * 1024; // 2 MB

        private static readonly Dictionary<string, byte[][]> ExtensoesDeImagemPermitidas =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
                [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
                [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
                [".gif"] = new[] { Encoding.ASCII.GetBytes("GIF87a"), Encoding.ASCII.GetBytes("GIF89a") },
            };

        private readonly ApiService _apiService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            ApiService apiService,
            IWebHostEnvironment environment,
            ILogger<ClientesController> logger)
        {
            _apiService = apiService;
            _environment = environment;
            _logger = logger;
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
        [ValidateAntiForgeryToken]
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
                    if (logotipo.Length > MaxLogotipoBytes)
                    {
                        ModelState.AddModelError("", "O logotipo excede o tamanho maximo de 2 MB.");
                        return View(clienteDto);
                    }

                    var extensao = Path.GetExtension(logotipo.FileName);
                    if (string.IsNullOrEmpty(extensao) ||
                        !ExtensoesDeImagemPermitidas.TryGetValue(extensao, out var assinaturas))
                    {
                        ModelState.AddModelError("", "Formato de logotipo nao permitido. Use JPG, PNG ou GIF.");
                        return View(clienteDto);
                    }

                    if (!await ConteudoBateComAssinaturaAsync(logotipo, assinaturas))
                    {
                        ModelState.AddModelError("", "O arquivo enviado nao e uma imagem valida.");
                        return View(clienteDto);
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logotipos");
                    Directory.CreateDirectory(uploadsFolder);

                    // Nome gerado 100% no servidor: nada do FileName do cliente e reaproveitado,
                    // o que tambem elimina path traversal via nome de arquivo.
                    var fileName = $"{Guid.NewGuid():N}{extensao.ToLowerInvariant()}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.CreateNew))
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
                // Detalhe da excecao fica so no log do servidor; o usuario recebe id de correlacao.
                var correlationId = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro ao cadastrar cliente. CorrelationId={CorrelationId}", correlationId);
                ModelState.AddModelError("", $"Erro ao enviar os dados. Codigo de referencia: {correlationId}");
                return View(clienteDto);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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
                var correlationId = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro ao excluir cliente {ClienteId}. CorrelationId={CorrelationId}", id, correlationId);
                TempData["ErrorMessage"] = $"Erro interno ao excluir o cliente. Codigo de referencia: {correlationId}";
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
                var correlationId = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro ao atualizar cliente {ClienteId}. CorrelationId={CorrelationId}", clienteDto.Id, correlationId);
                TempData["ErrorMessage"] = $"Erro ao atualizar cliente. Codigo de referencia: {correlationId}";
                return View(clienteDto);
            }
        }

        /// <summary>
        /// Confere os primeiros bytes do arquivo contra as assinaturas conhecidas do formato.
        /// Sem isso bastaria renomear um .html para .png para gravar HTML executavel em wwwroot.
        /// </summary>
        private static async Task<bool> ConteudoBateComAssinaturaAsync(IFormFile arquivo, byte[][] assinaturas)
        {
            var tamanhoMaximo = assinaturas.Max(a => a.Length);
            var cabecalho = new byte[tamanhoMaximo];

            using var stream = arquivo.OpenReadStream();
            var lidos = await stream.ReadAsync(cabecalho.AsMemory(0, tamanhoMaximo));

            return assinaturas.Any(assinatura =>
                assinatura.Length <= lidos &&
                cabecalho.Take(assinatura.Length).SequenceEqual(assinatura));
        }
    }
}
