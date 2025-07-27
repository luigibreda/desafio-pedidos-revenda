using RabbitMQ.Client;
using System;

Console.WriteLine("Testando referência ao RabbitMQ.Client...");

// Verificar se o tipo IModel está disponível
try
{
    // Tentar criar uma referência ao tipo IModel
    var modelType = typeof(IModel);
    Console.WriteLine($"Tipo IModel encontrado: {modelType.FullName}");
    
    // Tentar criar uma conexão
    var factory = new ConnectionFactory() { HostName = "localhost" };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();
    
    Console.WriteLine("Conexão e canal criados com sucesso!");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao acessar o RabbitMQ.Client: {ex.Message}");
    Console.WriteLine($"Tipo da exceção: {ex.GetType().FullName}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("Pressione qualquer tecla para sair...");
Console.ReadKey();
