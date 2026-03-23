namespace Monitoramento.Domain.Entities;

public class Ativo 
{
    // O ID é necessário para o Entity Framework (EF) mapear no banco
    public int Id { get; set; }

    // O endereço que será monitorado (ex: https://google.com)
    public string Url { get; set; } = string.Empty;

    // Estado atual do servidor
    public bool EstaOnline { get; set; }

    // Registro de tempo para auditoria e relatórios
    public DateTime UltimaVerificacao { get; set; }

    // REGRA DE NEGÓCIO: Um método que não depende de banco, apenas da lógica
    public bool PrecisaDeAlerta() 
    {
        // Se estiver offline há mais de 5 minutos, podemos considerar crítico
        return !EstaOnline && (DateTime.UtcNow - UltimaVerificacao).TotalMinutes > 5;
    }
}