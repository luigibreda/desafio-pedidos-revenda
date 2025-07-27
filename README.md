# Sistema de Distribuição de Bebidas - AMBEV

Sistema de gerenciamento de pedidos para revendas de bebidas, desenvolvido em .NET 8.0 seguindo princípios de Clean Architecture e Domain-Driven Design (DDD).

## 🎯 Sobre o Projeto

Solução completa para o desafio de implementação de um sistema de pedidos para revendas da AMBEV, com foco em:
- Recebimento de pedidos de clientes sem restrições de quantidade mínima
- Consolidação e envio de pedidos para a AMBEV com regra de quantidade mínima de 1000 unidades
- Garantia de entrega mesmo com falhas na API externa

### Destaques da Solução

1. **Arquitetura Escalável**: Separação clara de responsabilidades entre camadas
2. **Resiliência**: Tratamento robusto de falhas com retry e dead-letter queue
3. **Observabilidade**: Logs estruturados e métricas para monitoramento
4. **Documentação**: API documentada com Swagger/OpenAPI

## 🏗️ Arquitetura da Solução

### Visão Geral da Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        BEVERAGE DISTRIBUTOR API                         │
│                                                                         │
│  ┌─────────────────┐     ┌─────────────────────┐     ┌──────────────┐  │
│  │                 │     │                     │     │              │  │
│  │  API Controller │◄───►│  OrderOrchestrator  │◄───►│  Order       │  │
│  │                 │     │                     │     │  Processing  │  │
│  └────────┬────────┘     └──────────┬──────────┘     └──────┬───────┘  │
│           │                         │                       |          │
│  ┌────────▼────────┐      ┌────────▼──────────┐    ┌────────▼───────┐  │
│  │                 │      │                   │    │                │  │
│  │  Swagger/       │      │  RabbitMQ         │    │  External     │  │
│  │  Documentação   │      │  (Message Queue)  │    │  AMBEV API    │  │
│  │                 │      │                   │    │                │  │
│  └─────────────────┘      └───────────────────┘    └────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Fluxo de Processamento de Pedidos

1. Cliente envia pedido via API REST
2. `OrderOrchestrator` valida e persiste o pedido
3. Pedido é publicado na fila para processamento assíncrono
4. `OrderProcessingService` consome a mensagem e aplica regras de negócio
5. Pedido é enviado para API da AMBEV com validação de quantidade mínima
6. Em caso de falha, mensagem é movida para DLQ para análise posterior

### Estrutura do Projeto

```
├── BeverageDistributor.API/          # API Controllers e configuração
├── BeverageDistributor.Application/  # Casos de uso e DTOs
├── BeverageDistributor.Domain/       # Entidades e regras de negócio
├── BeverageDistributor.Infrastructure/ # Implementações concretas
│   ├── Services/                     # Serviços de infraestrutura
│   ├── Repositories/                 # Acesso a dados
│   └── MessageBroker/                # Integração com RabbitMQ
└── BeverageDistributor.Tests/        # Testes automatizados
```

## 🛠️ Tecnologias Utilizadas

### Core
- **.NET 8.0** com ASP.NET Core Web API
- **Entity Framework Core 8.0** para acesso a dados
- **FluentValidation** para validação de requisições

### Infraestrutura
- **PostgreSQL** como banco de dados principal
- **RabbitMQ** para processamento assíncrono de pedidos
- **Serilog** para logging estruturado
- **Polly** para políticas de resiliência

### Qualidade
- **xUnit** para testes unitários
- **Moq** para mocks em testes
- **Swagger/OpenAPI** para documentação da API

## 🚀 Como Executar

### Pré-requisitos

- .NET 8.0 SDK
- Docker e Docker Compose

### Variáveis de Ambiente

Crie um arquivo `.env` na raiz do projeto com as seguintes variáveis:

```ini
# Banco de Dados PostgreSQL
POSTGRES_DB=beverage_distributor
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

# RabbitMQ
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest

# API Externa
EXTERNAL_API__BASEURL=https://api.external-distributor.com
EXTERNAL_API__ORDERENDPOINT=/api/orders
EXTERNAL_API__APIKEY=your-api-key-here
EXTERNAL_API__TIMEOUTSECONDS=30
EXTERNAL_API__RETRYCOUNT=3
EXTERNAL_API__CIRCUITBREAKERFAILURETHRESHOLD=0.5
EXTERNAL_API__CIRCUITBREAKERSAMPLINGDURATIONSECONDS=60
EXTERNAL_API__CIRCUITBREAKERMINIMUMTHROUGHPUT=10
EXTERNAL_API__CIRCUITBREAKERDURATIONOFSECONDS=60

# Processamento de Pedidos
ORDERPROCESSING__MINORDERQUANTITY=1000
ORDERPROCESSING__MAXRETRYATTEMPTS=3
ORDERPROCESSING__RETRYDELAYSECONDS=5
```

Ou configure diretamente no arquivo `appsettings.json` na pasta `BeverageDistributor.API`.

### Executando com Docker Compose

1. **Clone o repositório**
   ```bash
   git clone <url-do-repositorio>
   cd desafio-pedidos-revenda
   ```

2. **Inicie os containers**
   ```bash
   docker-compose up -d
   ```

3. **Acesse a aplicação**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - RabbitMQ Management: http://localhost:15672 (guest/guest)
   - PGAdmin: http://localhost:5050 (admin@admin.com/admin)

### Configuração

As variáveis de ambiente podem ser configuradas no arquivo `.env` ou diretamente no `docker-compose.yml`.

### Execução com Docker

```bash
# Suba toda a infraestrutura
docker-compose up -d

# A aplicação estará disponível em http://localhost:8080
```

## 📋 Funcionalidades Principais

### 1. Gestão de Pedidos

#### Recebimento de Pedidos
- Aceita pedidos de qualquer quantidade
- Validação de dados de entrada
- Resposta imediata com confirmação de recebimento

#### Processamento Assíncrono
- Fila de mensagens com RabbitMQ
- Validação de quantidade mínima (1000 unidades) apenas no envio para AMBEV
- Dead-letter queue para tratamento de erros

#### Endpoints Principais
```http
POST   /api/orders                    # Criar novo pedido
GET    /api/orders                    # Listar todos os pedidos
GET    /api/orders/{id}               # Buscar pedido por ID
GET    /api/orders/client/{clientId}  # Buscar pedidos por cliente
```

### 2. Observabilidade

#### Logs Estruturados
- Níveis de log configuráveis
- Formato JSON para fácil análise
- Rastreamento de erros com correlation ID

#### Métricas
- Health checks para monitoramento
- Métricas de performance
- Status da fila de mensagens

### 3. Resiliência

- Retry automático em falhas de rede
- Circuit breaker para evitar sobrecarga
- Timeout configurável para chamadas externas

## 🔧 Configuração

### Variáveis de Ambiente

```bash
# Banco de Dados
POSTGRES_DB=beverage_distributor
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

# RabbitMQ
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest

# Aplicação
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:80
```

### Exemplo de Requisição

```http
POST /api/orders
Content-Type: application/json

{
  "clientId": "cliente-123",
  "items": [
    {
      "productId": "prod-001",
      "productName": "Skol 350ml",
      "quantity": 50,
      "unitPrice": 2.50
    }
  ]
}
```

## 📸 Demonstração

### 1. Swagger UI
![Swagger UI](docs/endpoints.png)

### 2. Logs de Observabilidade
![Logs de Observabilidade](docs/observabilidade.png)

### 3. Schemas
![Schemas](docs/schemas.png)

## 🎯 Pontos importantes

### ✅ Implementado
- [x] Modelagem de domínio para pedidos e itens
- [x] Separação clara entre pedidos de clientes e envio para AMBEV
- [x] Tratamento adequado da regra de quantidade mínima (1000 unidades)
- [x] Código limpo e bem estruturado seguindo Clean Architecture
- [x] Testes automatizados básicos
- [x] Documentação básica da API
- [x] Logging básico com níveis apropriados
- [x] Health check básico para banco de dados
- [x] Circuit breaker para chamadas à API externa
- [x] Retry com backoff exponencial

### ⚠️ Parcialmente Implementado
- [~] Logs estruturados (formato texto, não JSON)
- [~] Rastreabilidade de erros (básica, sem correlação automática)

### ❌ Não Implementado
- [ ] Métricas de performance detalhadas
- [ ] Monitoramento de recursos
- [ ] Health checks avançados (RabbitMQ, API externa)
- [ ] Painel de monitoramento

## 🧪 Testes

### Executando os Testes

```bash
dotnet test
```

## 📊 Observabilidade

### Health Checks

A aplicação possui health checks básicos configurados:

- ✅ **Banco de Dados**: Verificação de conectividade com PostgreSQL
- ❌ **Message Broker**: Status do RabbitMQ (não implementado)
- ❌ **APIs Externas**: Disponibilidade da API da distribuidora (não implementado)
- ❌ **Dependências**: Status geral do sistema (apenas banco de dados implementado)

Acesse: `GET /health` para verificação básica de saúde da aplicação.

### Logging Estruturado

A aplicação utiliza o sistema de logging integrado do ASP.NET Core com as seguintes características:

- ✅ **Níveis de Log**: Suporte a Debug, Information, Warning, Error, Critical
- ✅ **Contexto de Negócio**: Logs incluem informações relevantes do pedido
- ⚠️ **Correlação de Requisições**: Implementação básica via logs manuais
- ❌ **Formato JSON**: Logs em formato de texto simples (não estruturado em JSON)

Exemplo de log implementado:
```
[Information] Tentativa 1 de envio do pedido à API externa. Motivo: 500
[Warning] Circuito aberto por 60000ms devido a: 500 Internal Server Error
[Information] Circuito fechado, as requisições serão permitidas novamente
```

### Métricas e Monitoramento

A aplicação possui monitoramento básico com as seguintes limitações:

- ❌ **Request/Response Timing**: Não implementado
- ❌ **Error Rates**: Apenas contagem básica via logs
- ❌ **Business Metrics**: Não implementado
- ❌ **Resource Usage**: Não implementado

**Observação**: A implementação atual se baseia principalmente em logs para monitoramento, mas, basta aumentar a cobertura, fiz apenas um exemplo. 

### Tratamento de Indisponibilidade da API Externa

1. **Detecção de Falha**: Circuit breaker monitora falhas consecutivas
2. **Armazenamento Local**: Pedidos são persistidos localmente
3. **Retry com Backoff**: Tentativas com intervalos exponenciais
4. **Recuperação**: Processamento automático quando serviço volta

## 🚀 Melhorias Futuras

### Prioridade Alta

1. **Sistema de Notificações**
   - Alertas em tempo real para falhas críticas
   - Notificações para a equipe de operações
   - Dashboard de status do sistema

2. **Monitoramento Avançado**
   - Integração com Prometheus/Grafana
   - Métricas detalhadas de performance
   - Health checks abrangentes (RabbitMQ, API externa)

3. **Logs Estruturados**
   - Formato JSON para melhor análise
   - Correlação automática de requisições
   - Integração com ferramentas como ELK ou Seq

### Prioridade Média

4. **Processamento em Lote**
   - Agrupar pedidos por distribuidor
   - Otimizar chamadas à API externa
   - Processar em lotes baseado em tempo/quantidade

5. **Autenticação e Autorização**
   - Autenticação JWT
   - Controle de acesso baseado em roles
   - Rate limiting

### Prioridade Baixa

6. **Arquitetura Avançada**
   - Migração para CQRS/Event Sourcing
   - Decomposição em microsserviços
   - Cache distribuído com Redis
   - API Gateway
   - Orquestração com Kubernetes

## 🤝 Considerações Técnicas

### Escolhas Arquiteturais

1. **DDD sobre CQRS**: Optei por DDD puro devido ao tempo limitado, mas a arquitetura permite evolução para CQRS facilmente
## 🏆 Considerações Finais

### Decisões de Projeto

1. **Arquitetura em Camadas**
   - Separação clara de responsabilidades
   - Fácil manutenção e evolução
   - Testabilidade aprimorada

2. **Resiliência**
   - Tratamento robusto de falhas
   - Garantia de entrega das mensagens
   - Recuperação automática

3. **Escalabilidade**
   - Processamento assíncrono
   - Baixo acoplamento entre serviços
   - Fácil escalabilidade horizontal


---

*Desenvolvido como parte do desafio técnico para Desenvolvedor Sênior*