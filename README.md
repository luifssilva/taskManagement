# Task Management API

API RESTful (backend-only) para gestão de tarefas, construída com **ASP.NET Core 8 (Controllers)**, **Entity Framework Core InMemory** e arquitetura em camadas inspirada em **DDD / Clean Architecture**.

Permite **criar, consultar, listar, filtrar, buscar, editar e excluir** tarefas.

---

## Sumário

- [Tecnologias](#tecnologias)
- [Pré-requisitos](#pré-requisitos)
- [Como executar](#como-executar)
- [Swagger](#swagger)
- [Como executar os testes](#como-executar-os-testes)
- [Endpoints](#endpoints)
- [Exemplos de utilização](#exemplos-de-utilização)
- [Arquitetura](#arquitetura)
- [Decisões arquiteturais](#decisões-arquiteturais)

---

## Tecnologias

| Tecnologia | Uso |
|---|---|
| .NET 8 (LTS) / ASP.NET Core Web API | API com Controllers (sem Minimal API) |
| Entity Framework Core 8 + provider **InMemory** | Persistência, sem banco externo |
| FluentValidation 11 | Validação dos DTOs de entrada |
| Swashbuckle (Swagger / OpenAPI) | Documentação e teste interativo |
| `ILogger<T>` | Logging nativo do .NET |
| `IExceptionHandler` + Problem Details (RFC 7807) | Tratamento centralizado de erros |
| xUnit + `Microsoft.AspNetCore.Mvc.Testing` | Testes automatizados |

## Pré-requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download) ou superior (o projeto tem como alvo `net8.0`; SDKs mais novos também compilam).

Nenhum banco de dados ou serviço externo é necessário.

## Como executar

Na raiz do repositório:

```bash
dotnet restore
dotnet build
dotnet run --project src/TaskManagement.Api
```

A API sobe em `http://localhost:5080` (perfil `http`). Para HTTPS: `dotnet run --project src/TaskManagement.Api --launch-profile https` (`https://localhost:7080`).

> Os dados ficam em memória: são perdidos ao reiniciar a aplicação.

## Swagger

Com a aplicação rodando, acesse:

- **UI:** http://localhost:5080/swagger
- **Documento OpenAPI:** http://localhost:5080/swagger/v1/swagger.json

O Swagger mostra todos os endpoints, parâmetros, corpos de requisição com exemplos, respostas possíveis (200/201/204/400/404/500) e os valores permitidos de `status`. É possível testar os endpoints diretamente pela interface ("Try it out").

## Como executar os testes

```bash
dotnet test
```

Com cobertura (coverlet):

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Os testes ficam em `tests/TaskManagement.Tests`:

| Pasta | O que cobre |
|---|---|
| `Application/` | **Regras de negócio** do `TaskService`: criação, consulta, filtros, busca, atualização, exclusão, validações e cenários de erro. Usa o repositório real sobre um banco InMemory isolado por teste e um relógio fixo (`TimeProvider`). |
| `Domain/` | Invariantes da entidade `TaskItem` e conversão de status. |
| `Api/` | Testes de integração do contrato HTTP (status codes, header `Location`, formato de erro, Swagger) via `WebApplicationFactory`. |

---

## Endpoints

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| `GET` | `/api/tasks` | Lista tarefas. Filtros opcionais: `status`, `dueDate` (combináveis) | 200, 400 |
| `GET` | `/api/tasks/search?term=...` | Busca por termo no título ou descrição | 200, 400 |
| `GET` | `/api/tasks/{id}` | Consulta tarefa por ID | 200, 404 |
| `POST` | `/api/tasks` | Cria tarefa | 201 (+ `Location`), 400 |
| `PUT` | `/api/tasks/{id}` | Atualiza título, descrição, status e data de vencimento | 200, 400, 404 |
| `DELETE` | `/api/tasks/{id}` | Exclui tarefa | 204, 404 |

Qualquer erro inesperado retorna `500` com Problem Details genérico (sem stack trace).

### Modelo

| Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|
| `id` | `Guid` | gerado | Identificador único gerado pela aplicação |
| `title` | string | sim | Não vazio, máx. 200 caracteres (espaços nas pontas são removidos) |
| `description` | string | não | Máx. 2000 caracteres |
| `dueDate` | data (`yyyy-MM-dd`) | não | Formato de data válido |
| `status` | string | sim | `Pendente`, `Em progresso` ou `Concluída` |
| `createdAt` / `updatedAt` | data/hora UTC | gerado | Auditoria simples |

O `status` é aceito sem diferenciar maiúsculas/minúsculas, acentos e espaços (`"concluida"`, `"EM PROGRESSO"` e `"EmProgresso"` são válidos) e é sempre **retornado** no formato canônico (`"Concluída"`, `"Em progresso"`). Valores numéricos não são aceitos.

---

## Exemplos de utilização

### Criar tarefa

```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Implementar API",
  "description": "Criar endpoints de tarefas",
  "dueDate": "2026-10-01",
  "status": "Pendente"
}
```

```http
HTTP/1.1 201 Created
Location: http://localhost:5080/api/tasks/03411814-c63d-41f7-adca-5490971f0448

{
  "id": "03411814-c63d-41f7-adca-5490971f0448",
  "title": "Implementar API",
  "description": "Criar endpoints de tarefas",
  "dueDate": "2026-10-01",
  "status": "Pendente",
  "createdAt": "2026-09-29T15:10:21.3157931+00:00",
  "updatedAt": null
}
```

### Listar e filtrar

```http
GET /api/tasks
GET /api/tasks?status=Pendente
GET /api/tasks?dueDate=2026-10-01
GET /api/tasks?status=Em%20progresso&dueDate=2026-10-01
```

```http
HTTP/1.1 200 OK

[
  {
    "id": "03411814-c63d-41f7-adca-5490971f0448",
    "title": "Implementar API",
    "description": "Criar endpoints de tarefas",
    "dueDate": "2026-10-01",
    "status": "Pendente",
    "createdAt": "2026-09-29T15:10:21.3157931+00:00",
    "updatedAt": null
  }
]
```

A ordenação é por data de vencimento (tarefas sem data por último) e, em seguida, por data de criação.

### Buscar

```http
GET /api/tasks/search?term=relatório
```

Retorna `200 OK` com a lista de tarefas cujo **título ou descrição contenha** o termo (sem diferenciar maiúsculas/minúsculas). Sem correspondências, retorna `[]`. Termo ausente/vazio retorna `400`.

### Consultar por ID

```http
GET /api/tasks/03411814-c63d-41f7-adca-5490971f0448
```

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Task '03411814-c63d-41f7-adca-5490971f0448' was not found.",
  "instance": "/api/tasks/03411814-c63d-41f7-adca-5490971f0448",
  "traceId": "0HNOU7ENKVVER:00000001"
}
```

### Atualizar

```http
PUT /api/tasks/03411814-c63d-41f7-adca-5490971f0448
Content-Type: application/json

{
  "title": "Implementar API",
  "description": "Criar endpoints de tarefas e documentação",
  "dueDate": "2026-10-15",
  "status": "Em progresso"
}
```

Retorna `200 OK` com a tarefa atualizada. O `PUT` tem semântica de **substituição**: campos opcionais omitidos (`description`, `dueDate`) são limpos.

### Excluir

```http
DELETE /api/tasks/03411814-c63d-41f7-adca-5490971f0448
```

Retorna `204 No Content`, ou `404 Not Found` se a tarefa não existir.

### Erro de validação

```http
POST /api/tasks
Content-Type: application/json

{ "title": "", "status": "Cancelada" }
```

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "The request contains invalid data.",
  "instance": "/api/tasks",
  "errors": {
    "title": ["Title is required."],
    "status": ["Status must be one of: 'Pendente', 'Em progresso', 'Concluída'."]
  },
  "traceId": "0HNOU7ENKVVEP:00000001"
}
```

Datas em formato inválido (`"dueDate": "31/12/2026"` ou `?dueDate=abc`) e JSON malformado retornam o mesmo formato de erro.

---

## Arquitetura

Arquitetura em camadas no estilo **DDD / Clean Architecture**, com as dependências apontando para o domínio:

```text
TaskManagement.Api ──────────► TaskManagement.Application ──► TaskManagement.Domain
        │                                                            ▲
        └──────► TaskManagement.Infrastructure ──────────────────────┘
                 (registrada apenas na composição, em Program.cs)
```

Fluxo de uma requisição:

```text
TasksController → ITaskService (TaskService) → ITaskReadRepository / ITaskWriteRepository (TaskRepository) → TaskDbContext
```

```text
src/
├── TaskManagement.Domain/            # Núcleo: sem dependências externas
│   ├── Entities/TaskItem.cs          # Entidade com invariantes (Create/Update)
│   ├── Enums/                        # TaskItemStatus + conversão de/para nomes públicos
│   ├── Exceptions/DomainException.cs
│   └── Interfaces/                   # ITaskReadRepository, ITaskWriteRepository
│
├── TaskManagement.Application/       # Casos de uso
│   ├── DTOs/                         # CreateTaskRequest, UpdateTaskRequest, TaskFilterRequest, TaskResponse
│   ├── Interfaces/ITaskService.cs
│   ├── Services/TaskService.cs       # Orquestra validação, domínio, persistência e logging
│   ├── Validators/                   # FluentValidation
│   ├── Mappings/                     # Entidade → DTO
│   ├── Exceptions/TaskNotFoundException.cs
│   └── DependencyInjection.cs        # AddApplication()
│
├── TaskManagement.Infrastructure/    # Detalhes técnicos
│   ├── Data/TaskDbContext.cs
│   ├── Configurations/               # Mapeamento EF Core (IEntityTypeConfiguration)
│   ├── Repositories/TaskRepository.cs
│   └── DependencyInjection.cs        # AddInfrastructure()
│
└── TaskManagement.Api/               # Entrada HTTP
    ├── Controllers/TasksController.cs
    ├── Middleware/GlobalExceptionHandler.cs
    ├── Extensions/                   # Registro de controllers, erros e Swagger
    ├── Swagger/                      # Filtros de documentação
    └── Program.cs                    # Composition root

tests/
└── TaskManagement.Tests/             # Application, Domain e Api
```

### Responsabilidades das camadas

| Camada | Responsabilidade | Depende de |
|---|---|---|
| **Domain** | Entidade `TaskItem`, enum de status e suas regras invariantes; contratos de repositório. Não conhece EF, HTTP nem DTOs. | nada |
| **Application** | Casos de uso (`TaskService`): valida entrada, aciona o domínio, persiste via abstrações, registra logs e converte para DTOs. | Domain |
| **Infrastructure** | Implementação da persistência com EF Core InMemory (`TaskDbContext`, `TaskRepository`, configurações). | Domain |
| **Api** | Recebe HTTP, delega ao serviço, devolve status codes adequados; tratamento global de erros; Swagger; composição da DI. | Application, Infrastructure |

---

## Decisões arquiteturais

### Por que camadas no estilo DDD / Clean Architecture

- **Motivos:** o desafio pede separação clara de responsabilidades, SOLID e testes da camada de negócio. Com o domínio e os casos de uso isolados de EF Core e HTTP, as regras são testáveis sem subir a API e a persistência pode ser trocada (ex.: SQL Server) alterando apenas a Infrastructure.
- **Vantagens:** baixo acoplamento, dependências explícitas via DI, cada projeto com um motivo único para mudar, testes focados.
- **Trade-offs:** mais projetos e arquivos do que um CRUD simples exigiria; um pouco de mapeamento manual (entidade ↔ DTO). Para um domínio pequeno isso é um custo consciente em troca de clareza e evolutividade. Não foram adotados CQRS/MediatR para não adicionar indireção sem necessidade real.

### SOLID na prática

- **SRP:** controller só traduz HTTP; `TaskService` orquestra casos de uso; validadores só validam; `TaskRepository` só persiste; `GlobalExceptionHandler` só converte exceções em respostas.
- **OCP:** novas regras de validação são adicionadas em validadores sem alterar o serviço; novos tipos de erro entram como um novo caso no handler; filtros de Swagger estendem a documentação sem mexer nos controllers.
- **LSP:** o serviço funciona com qualquer implementação de `ITaskReadRepository`/`ITaskWriteRepository` e de `TimeProvider` (nos testes, um relógio fixo).
- **ISP:** o repositório é dividido em **leitura** (`ITaskReadRepository`) e **escrita** (`ITaskWriteRepository`), em vez de uma interface genérica grande.
- **DIP:** Application depende de abstrações definidas no Domain; a Infrastructure implementa essas abstrações e só é conectada no `Program.cs`.

### Entidade de domínio

- Nome `TaskItem` para não colidir com `System.Threading.Tasks.Task`.
- Setters privados: o estado só muda por `TaskItem.Create(...)` e `task.Update(...)`, que garantem as invariantes (título obrigatório e com tamanho máximo, descrição com tamanho máximo, status válido) e lançam `DomainException` caso violadas. Assim a entidade nunca fica inválida, mesmo se usada fora do fluxo da API.
- `CreatedAt`/`UpdatedAt` preenchidos a partir de `TimeProvider` (injetável, facilita testes).

### Identificador

`Guid` gerado pela própria entidade (`Guid.NewGuid()`) no momento da criação: é único sem depender do banco, não é sequencial/adivinhável e o ID existe antes da persistência (útil para logs e para o header `Location`).

### Persistência e Repository Pattern

- **EF Core InMemory** (`Microsoft.EntityFrameworkCore.InMemory`), configurado em `AddInfrastructure()`. Não requer banco externo; os dados vivem enquanto o processo estiver ativo.
- O mapeamento fica em `TaskItemConfiguration` (`IEntityTypeConfiguration`), mantendo a entidade livre de atributos de persistência. O status é armazenado como texto.
- **Repository Pattern foi adotado** conscientemente: (1) o Domain/Application não dependem de EF Core, respeitando DIP; (2) consultas de filtro, busca e ordenação ficam concentradas em um só lugar; (3) permite trocar o provider sem tocar nas regras. O trade-off é uma camada extra sobre o `DbContext` (que já é um Unit of Work); por isso o repositório é fino e cada operação de escrita salva imediatamente, sem um Unit of Work adicional — suficiente para operações de agregado único como as desta API.
- Leituras usam `AsNoTracking()`; a escrita carrega a entidade rastreada (`FindForUpdateAsync`) e salva as alterações.
- Um único `TaskRepository` (scoped) atende às duas interfaces na mesma requisição.

### Estratégia de busca

`GET /api/tasks/search?term=...` faz **"contém", sem diferenciar maiúsculas/minúsculas**, no título **ou** na descrição (o termo é aparado). A consulta é escrita em LINQ traduzível (`ToLower().Contains(...)`), então funcionaria também em um provider relacional. Termo vazio é rejeitado com `400`. Para grandes volumes, a evolução natural seria full-text search do banco ou um motor dedicado (ex.: Elasticsearch), trocando apenas a implementação do repositório.

### Estratégia de validação

Validação em **duas camadas complementares**:

1. **Formato (model binding do ASP.NET Core):** JSON malformado e datas inválidas (`DateOnly`, formato `yyyy-MM-dd`) são rejeitados antes de chegar ao serviço. A resposta é customizada (`InvalidModelStateResponseFactory`) para seguir o mesmo formato de Problem Details, sem expor mensagens internas do serializador.
2. **Regras de entrada (FluentValidation, na Application):** título obrigatório/tamanho máximo, descrição com tamanho máximo, status permitido, filtro de status válido. O `TaskService` executa os validadores explicitamente e lança `ValidationException`, de modo que **as regras valem para qualquer consumidor do serviço** (não só a API) e são cobertas pelos testes de negócio.

Além disso, a entidade protege suas próprias invariantes (defesa em profundidade). O `status` é recebido como string nos DTOs para que valores inválidos gerem uma mensagem de validação clara (listando os valores permitidos), em vez de um erro genérico de desserialização.

Não há restrição de data de vencimento no passado: é comum registrar tarefas atrasadas, e o desafio não define essa regra.

### Estratégia de tratamento de erros

Centralizada em `GlobalExceptionHandler` (`IExceptionHandler`) + `AddProblemDetails()`, produzindo respostas **Problem Details (RFC 7807)** consistentes com `type`, `title`, `status`, `detail`, `instance` e `traceId`:

| Exceção | Status | Title |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | Validation Error (com `errors` por campo) |
| `DomainException` | 400 | Business Rule Violation |
| `TaskNotFoundException` | 404 | Resource Not Found |
| qualquer outra | 500 | Internal Server Error (mensagem genérica, sem stack trace) |

Os controllers não têm `try/catch`: o fluxo feliz fica limpo e o mapeamento erro → HTTP fica em um único lugar. Rotas inexistentes também retornam Problem Details (`UseStatusCodePages`). Rotas com `{id:guid}` retornam 404 para IDs que não são GUID.

### Logging

`ILogger<T>` nativo, com logs estruturados:

- `Information`: criação, atualização e exclusão de tarefas (apenas ID e status — sem título/descrição, evitando registrar conteúdo possivelmente sensível); respostas de erro 4xx tratadas.
- `Warning`: falhas de validação (apenas os nomes dos campos inválidos).
- `Error`: exceções inesperadas, com a exceção completa (somente no log, nunca na resposta).

### Swagger / OpenAPI

Swashbuckle com comentários XML dos controllers e DTOs (descrições, exemplos de request/response e status codes via `ProducesResponseType`). Filtros próprios documentam os valores permitidos de `status` como enum e exibem os parâmetros de query em camelCase. O Swagger fica habilitado em todos os ambientes para facilitar a avaliação; em produção real, o ideal seria restringi-lo.

### Versão do .NET

Alvo `net8.0` (LTS), atendendo ao requisito ".NET 8+" com a maior compatibilidade. Configurações comuns (target, nullable, implicit usings) ficam em `Directory.Build.props`.
