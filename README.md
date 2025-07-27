# Sistema de Distribuição de Bebidas - Desafio Desenvolvedor Sênior

Um sistema completo para gerenciamento de pedidos de revendas de bebidas, desenvolvido em .NET 8.0 com arquitetura limpa e princípios de Domain-Driven Design (DDD).

## 🎯 Sobre o Desafio

Este projeto simula o fluxo real de pedidos de bebidas que as revendas fazem diariamente para a empresa. O sistema gerencia desde o cadastro das revendas até o processamento de pedidos dos clientes finais e integração com a API da distribuidora.

### Cenário de Negócio Implementado

1. **Cadastro de Revendas**: Sistema para gerenciamento de pedidos de revendas e clientes 
2. **Recebimento de Pedidos**: API para que revendas recebam pedidos de seus clientes
3. **Integração com Distribuidora**: Processamento e envio de pedidos consolidados
4. **Resiliência**: Tratamento de falhas e garantia de que nenhum pedido seja perdido

## 🏗️ Arquitetura da Solução

### Estrutura do Projeto

```
├── BeverageDistributor.API/          # Camada de Apresentação (Web API)
├── BeverageDistributor.Application/  # Camada de Aplicação (Casos de Uso)
├── BeverageDistributor.Domain/       # Camada de Domínio (Entidades e Regras)
├── BeverageDistributor.Infrastructure/ # Camada de Infraestrutura
└── BeverageDistributor.Tests/        # Testes Unitários e de Integração
```

### Padrões e Princípios Aplicados

- **Clean Architecture**: Separação clara de responsabilidades e inversão de dependências
- **Domain-Driven Design (DDD)**: Modelagem rica do domínio com entidades, value objects e agregados
- **SOLID**: Aplicação rigorosa dos princípios de design orientado a objetos
- **Repository Pattern**: Abstração da camada de dados
- **Command Query Separation**: Separação entre operações de leitura e escrita
- **Event-Driven Architecture**: Comunicação assíncrona via eventos de domínio

## 🛠️ Tecnologias Utilizadas

### Core Framework
- **.NET 8.0**: Framework principal com as mais recentes funcionalidades
- **ASP.NET Core Web API**: Para criação da API RESTful
- **Entity Framework Core 8.0**: ORM com suporte completo ao PostgreSQL

### Banco de Dados e Mensageria
- **PostgreSQL 13+**: Banco de dados principal com suporte a JSON e funcionalidades avançadas
- **RabbitMQ**: Sistema de mensageria para processamento assíncrono

### Qualidade e Testes
- **FluentValidation**: Validações expressivas e reutilizáveis
- **xUnit**: Framework de testes unitários
- **Moq**: Biblioteca para mocking em testes
- **Testcontainers**: Testes de integração com containers

### Observabilidade e Resiliência
- **Serilog**: Logging estruturado com múltiplos sinks
- **Polly**: Políticas de resiliência (retry, circuit breaker, timeout)
- **Health Checks**: Monitoramento da saúde da aplicação
- **Swagger/OpenAPI**: Documentação interativa da API

## 🚀 Como Executar

### Pré-requisitos

- .NET 8.0 SDK
- PostgreSQL 13 ou superior
- Docker (para RabbitMQ e PostgreSQL opcional)

### Execução Local

1. **Clone o repositório**
   ```bash
   git clone <url-do-repositorio>
   cd beverage-distributor
   ```

2. **Configure o banco de dados**
   ```bash
   # Atualize a connection string em appsettings.json
   # Exemplo: "Host=localhost;Database=BeverageDistributor;Username=postgres;Password=password"
   ```

3. **Execute as migrações**
   ```bash
   cd BeverageDistributor.API
   dotnet ef database update
   ```

4. **Inicie os serviços de infraestrutura**
   ```bash
   docker-compose -f docker-compose.infrastructure.yml up -d
   ```

5. **Execute a aplicação**
   ```bash
   dotnet run --project BeverageDistributor.API
   ```

6. **Acesse a documentação**
   - Swagger UI: https://localhost:5001/swagger
   - Health Checks: https://localhost:5001/health

### Execução com Docker

```bash
# Suba toda a infraestrutura
docker-compose up -d

# A aplicação estará disponível em http://localhost:8080
```

## 📋 Funcionalidades Implementadas

### 1. Gestão de Revendas

#### Cadastro Completo com Validações
- **CNPJ**: Validação de formato e dígitos verificadores
- **Razão Social**: Obrigatório, validação de caracteres e tamanho
- **Nome Fantasia**: Obrigatório com validações específicas
- **Email**: Validação de formato RFC 5322
- **Telefones**: Múltiplos telefones com validação de formato brasileiro
- **Contatos**: Múltiplos contatos com definição de principal
- **Endereços de Entrega**: Múltiplos endereços com validação de CEP

#### Endpoints Disponíveis
```http
POST   /api/v1/revendas              # Cadastrar revenda
GET    /api/v1/revendas              # Listar revendas
GET    /api/v1/revendas/{id}         # Buscar revenda por ID
PUT    /api/v1/revendas/{id}         # Atualizar revenda
DELETE /api/v1/revendas/{id}         # Remover revenda
```

### 2. Sistema de Pedidos

#### Recebimento de Pedidos dos Clientes
- Identificação única do cliente
- Lista de produtos com quantidades
- Sem limite mínimo para pedidos da revenda
- Resposta com ID do pedido e itens confirmados

#### Consolidação e Envio para Distribuidora
- **Regra de Negócio**: Mínimo de 1000 unidades por pedido
- **Agregação Inteligente**: Consolidação automática de produtos
- **Resiliência**: Retry com backoff exponencial
- **Garantia de Entrega**: Persistência local antes do envio

#### Endpoints de Pedidos
```http
POST /api/v1/pedidos/clientes        # Receber pedido de cliente
POST /api/v1/pedidos/processar       # Processar pedidos pendentes
GET  /api/v1/pedidos                 # Listar pedidos
GET  /api/v1/pedidos/{id}            # Buscar pedido específico
```

## 🔧 Configurações e Ambiente

### Variáveis de Ambiente

```bash
# Banco de Dados
ConnectionStrings__DefaultConnection="Host=localhost;Database=BeverageDistributor;Username=postgres;Password=password"

# RabbitMQ
MessageBroker__Host="localhost"
MessageBroker__Username="guest"
MessageBroker__Password="guest"

# API Externa (Distribuidora)
ExternalServices__DistributorApi__BaseUrl="https://api.distribuidor.com"
ExternalServices__DistributorApi__ApiKey="sua-api-key"
ExternalServices__DistributorApi__TimeoutSeconds=30

# Configurações de Resiliência
Resilience__RetryCount=3
Resilience__CircuitBreaker__HandledEventsAllowedBeforeBreaking=5
```

### Configuração de Logging

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/app-.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

## 🧪 Testes

### Execução dos Testes

```bash
# Todos os testes
dotnet test

# Apenas testes unitários
dotnet test --filter Category=Unit

# Apenas testes de integração
dotnet test --filter Category=Integration

# Com cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

### Cobertura de Testes

O projeto mantém alta cobertura de testes com foco em:

- **Testes Unitários**: Lógica de domínio e casos de uso
- **Testes de Integração**: APIs e persistência de dados
- **Testes de Contrato**: Validação de contratos de API
- **Testes de Resiliência**: Cenários de falha e recuperação

## 📊 Observabilidade

### Health Checks

A aplicação possui health checks configurados para:

- **Banco de Dados**: Conectividade com PostgreSQL
- **Message Broker**: Status do RabbitMQ
- **APIs Externas**: Disponibilidade da API da distribuidora
- **Dependências**: Status geral do sistema

Acesse: `GET /health` para verificação básica ou `GET /health/detailed` para informações completas.

### Logging Estruturado

Implementação completa de logging com:

- **Correlação de Requisições**: Tracking end-to-end
- **Contexto de Negócio**: Logs enriquecidos com dados relevantes
- **Níveis Apropriados**: Debug, Information, Warning, Error, Critical
- **Structured Logging**: Formato JSON para análise automatizada

### Métricas e Monitoramento

- **Request/Response Timing**: Tempo de resposta das APIs
- **Error Rates**: Taxa de erro por endpoint
- **Business Metrics**: Métricas específicas do negócio (pedidos processados, etc.)
- **Resource Usage**: Utilização de CPU, memória e conexões

## 🔄 Fluxo de Integração

### Tratamento de Indisponibilidade da API Externa

1. **Detecção de Falha**: Circuit breaker monitora falhas consecutivas
2. **Armazenamento Local**: Pedidos são persistidos localmente
3. **Retry com Backoff**: Tentativas com intervalos exponenciais
4. **Notificação**: Alertas automáticos para equipe de operações
5. **Recuperação**: Processamento automático quando serviço volta

### Políticas de Resiliência

```csharp
// Exemplo de configuração Polly
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            _logger.LogWarning("Tentativa {RetryCount} em {Delay}s", 
                retryCount, timespan.TotalSeconds);
        });
```

## 📈 Melhorias Futuras

- **CQRS**: Implementação completa com Event Sourcing
- **Microsserviços**: Decomposição em serviços especializados
- **Cache Distribuído**: Redis para performance
- **API Gateway**: Centralização de cross-cutting concerns
- **Kubernetes**: Orquestração e scaling automático

## 🤝 Considerações Técnicas

### Escolhas Arquiteturais

1. **DDD sobre CQRS**: Optei por DDD puro devido ao tempo limitado, mas a arquitetura permite evolução para CQRS facilmente
2. **Inglês no Código**: Mantive o código em inglês por preferência pessoal, mas posso adaptar ao padrão da empresa
3. **PostgreSQL**: Escolhido pela robustez e suporte a JSON para dados semi-estruturados
4. **RabbitMQ**: Garantia de entrega e durabilidade de mensagens

### Padrões de Código

- **Nomenclatura Consistente**: PascalCase para C#, camelCase para JSON
- **Documentação XML**: Documentação completa para IntelliSense
- **EditorConfig**: Padronização de estilo de código
- **Conventional Commits**: Padrão para mensagens de commit

## 📞 Suporte

Para dúvidas sobre a implementação ou decisões arquiteturais, consulte:

- **Documentação da API**: Swagger UI em `/swagger`
- **Logs da Aplicação**: Diretório `logs/`
- **Health Checks**: Endpoint `/health/detailed`

---

*Desenvolvido como parte do desafio técnico para Desenvolvedor Sênior*