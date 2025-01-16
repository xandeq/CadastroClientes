namespace CadastroClientesAPI.Models
{
    public class Logradouro
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string? Endereco { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? CEP { get; set; }
        public Cliente? Cliente { get; set; }
    }

}
