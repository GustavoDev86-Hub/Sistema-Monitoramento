using System.Text;
using RabbitMQ.Client;

namespace Monitoramento.Infrastructure.Messaging;

public class RabbitMqService
{
    private readonly string _hostname = "localhost";
    private readonly string _queueName = "alertas_monitoramento";

    public async Task EnviarAlerta(string mensagem)
    {
        var factory = new ConnectionFactory() { HostName = _hostname };
        
        // Na versão 7, usamos CreateConnectionAsync
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(queue: _queueName,
                                        durable: false,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

        var body = Encoding.UTF8.GetBytes(mensagem);

        await channel.BasicPublishAsync(exchange: "",
                                        routingKey: _queueName,
                                        body: body);
    }
}