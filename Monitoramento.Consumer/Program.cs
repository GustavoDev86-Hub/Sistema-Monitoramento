// See https://aka.ms/new-console-template for more information
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

const string queueName = "alertas_monitoramento";

await channel.QueueDeclareAsync(queue: queueName,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

Console.WriteLine(" [*] Aguardando alertas... Pressione [enter] para sair.");

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($" [!] ALERTA RECEBIDO: {message} em {DateTime.Now}");
    Console.ResetColor();

    // para o WhatsApp no futuro!
    await Task.Yield(); 
};

await channel.BasicConsumeAsync(queue: queueName,
                                autoAck: true,
                                consumer: consumer);

Console.ReadLine();
