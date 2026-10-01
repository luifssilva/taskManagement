# Task Management API

API RESTful (backend-only) para gestão de tarefas, construída com **ASP.NET Core 8 (Controllers)**, **Entity Framework Core InMemory** e arquitetura em camadas inspirada em **DDD / Clean Architecture**.

Permite **criar, consultar, listar, filtrar, buscar, editar e excluir** tarefas. Os exemplos usam o contexto de uma operação logística (conferência de carga, agendamento de coleta, expedição). Por isso, além do CRUD, a API trata duas preocupações típicas desse contexto:

- **Prazo / SLA:** cada tarefa indica se está **vencida** (`isOverdue`), e a listagem filtra por isso (`?overdue=true`).
- **Rastreabilidade:** toda mudança de status fica registrada e pode ser consultada em `GET /api/tasks/{id}/history`.

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
| Docker / Docker Compose | Execução em container (opcional) |

## Pré-requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download) ou superior (o projeto tem como alvo `net8.0`; SDKs mais novos também compilam).
- **Ou**, para rodar em container: [Docker](https://docs.docker.com/get-docker/) com Docker Compose.

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

### Com Docker

```bash
docker compose up --build
```

A API fica disponível em `http://localhost:8080` (Swagger em `http://localhost:8080/swagger`). Para usar outra porta no host: `API_PORT=9000 docker compose up --build`. Para parar: `docker compose down`.

- O `Dockerfile` é multi-stage: a etapa de build restaura, **executa os testes** (a imagem só é gerada se todos passarem) e publica; a imagem final contém só o runtime do ASP.NET Core e roda com usuário não-root.
- O container atende apenas HTTP na porta 8080 e roda em `Development` para não aplicar o redirecionamento HTTPS. Em produção, o TLS normalmente termina em um proxy reverso ou load balancer à frente do container.
- O "hoje" da regra de tarefa vencida segue o fuso do container. O compose usa `America/Sao_Paulo` por padrão; para outro fuso: `TZ=America/Manaus docker compose up --build`.

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
| `Application/` | **Regras de negócio** do `TaskService`: criação, consulta, filtros (status, data, vencidas), busca, atualização, exclusão, histórico de status, validações e cenários de erro. Usa o repositório real sobre um banco InMemory isolado por teste e um relógio fixo (`TimeProvider`). |
| `Domain/` | Invariantes da entidade `TaskItem`, regra de vencimento, histórico e alinhamento entre o enum de status e a tabela de domínio. |
| `Api/` | Testes de integração do contrato HTTP (status codes, `Location`, status por nome/número e valores inválidos, histórico, filtro de vencidas, tabela de status, formato de erro, Swagger) via `WebApplicationFactory`. |

---

## Endpoints

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| `GET` | `/api/tasks` | Lista tarefas. Filtros opcionais e combináveis: `status`, `dueDate`, `overdue` | 200, 400 |
| `GET` | `/api/tasks/search?term=...` | Busca por termo no título ou descrição | 200, 400 |
| `GET` | `/api/tasks/{id}` | Consulta tarefa por ID | 200, 404 |
| `GET` | `/api/tasks/{id}/history` | Histórico de mudanças de status | 200, 404 |
| `POST` | `/api/tasks` | Cria tarefa | 201 (+ `Location`), 400 |
| `PUT` | `/api/tasks/{id}` | Atualiza título, descrição, status e data de vencimento | 200, 400, 404 |
| `DELETE` | `/api/tasks/{id}` | Exclui tarefa | 204, 404 |
| `GET` | `/api/task-statuses` | Lista a tabela de domínio de status (id, nome, descrição) | 200 |

Qualquer erro inesperado retorna `500` com Problem Details genérico (sem stack trace).

### Modelo

| Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|
| `id` | `Guid` | gerado | Identificador único gerado pela aplicação |
| `title` | string | sim | Não vazio, máx. 200 caracteres (espaços nas pontas são removidos) |
| `description` | string | não | Máx. 2000 caracteres |
| `dueDate` | data (`yyyy-MM-dd`) | não | Formato de data válido |
| `status` | enum `TaskItemStatus` | sim | `Pendente`, `EmProgresso` ou `Concluida` (ou o número: 1, 2, 3) |
| `isOverdue` | bool | calculado | `true` se `dueDate` for **anterior a hoje** e o status não for `Concluida` |
| `createdAt` / `updatedAt` | data/hora UTC | gerado | Auditoria simples |

O `status` trafega pelo **nome do enum** (`"EmProgresso"`, sem diferenciar maiúsculas/minúsculas) ou pelo seu **número**, que é o Id da tabela de domínio. As respostas sempre usam o nome. A descrição legível de cada status (`"Em progresso"`, `"Concluída"`) vem da tabela, em `GET /api/task-statuses`:

```json
[
  { "id": 1, "name": "Pendente",    "description": "Pendente" },
  { "id": 2, "name": "EmProgresso", "description": "Em progresso" },
  { "id": 3, "name": "Concluida",   "description": "Concluída" }
]
```

---

## Exemplos de utilização

### Criar tarefa

```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Conferir carga do pedido 4521",
  "description": "Validar volumes e lacres antes da expedição",
  "dueDate": "2026-10-01",
  "status": "Pendente"
}
```

```http
HTTP/1.1 201 Created
Location: http://localhost:5080/api/tasks/d0b377bf-818a-48d2-b242-cc23c3205266

{
  "id": "d0b377bf-818a-48d2-b242-cc23c3205266",
  "title": "Conferir carga do pedido 4521",
  "description": "Validar volumes e lacres antes da expedição",
  "dueDate": "2026-10-01",
  "status": "Pendente",
  "isOverdue": false,
  "createdAt": "2026-09-29T15:25:22.1508093+00:00",
  "updatedAt": null
}
```

### Listar e filtrar

```http
GET /api/tasks
GET /api/tasks?status=Pendente
GET /api/tasks?dueDate=2026-10-01
GET /api/tasks?status=EmProgresso&dueDate=2026-10-01
GET /api/tasks?status=2
GET /api/tasks?overdue=true
GET /api/tasks?overdue=true&status=Pendente
```

Retorna `200 OK` com a lista de tarefas (mesmo formato da criação). A ordenação é por data de vencimento (tarefas sem data por último) e, em seguida, por data de criação. `?overdue=true` responde "o que está atrasado agora?"; `?overdue=false` retorna o que está em dia.

### Buscar

```http
GET /api/tasks/search?term=coleta
```

Retorna `200 OK` com a lista de tarefas cujo **título ou descrição contenha** o termo (sem diferenciar maiúsculas/minúsculas). Sem correspondências, retorna `[]`. Termo ausente/vazio retorna `400`.

### Consultar por ID

```http
GET /api/tasks/d0b377bf-818a-48d2-b242-cc23c3205266
```

Retorna `200 OK` com a tarefa. Se não existir:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Task 'd0b377bf-818a-48d2-b242-cc23c3205266' was not found.",
  "instance": "/api/tasks/d0b377bf-818a-48d2-b242-cc23c3205266",
  "traceId": "0HNOU7ENKVVER:00000001"
}
```

### Atualizar

```http
PUT /api/tasks/d0b377bf-818a-48d2-b242-cc23c3205266
Content-Type: application/json

{
  "title": "Conferir carga do pedido 4521",
  "description": "Volumes conferidos; aguardando coleta da transportadora",
  "dueDate": "2026-10-02",
  "status": "EmProgresso"
}
```

Retorna `200 OK` com a tarefa atualizada. O `PUT` tem semântica de **substituição**: campos opcionais omitidos (`description`, `dueDate`) são limpos.

### Histórico de status

```http
GET /api/tasks/d0b377bf-818a-48d2-b242-cc23c3205266/history
```

```http
HTTP/1.1 200 OK

[
  { "fromStatus": null,       "toStatus": "Pendente",    "changedAt": "2026-09-29T15:25:22.15+00:00" },
  { "fromStatus": "Pendente", "toStatus": "EmProgresso", "changedAt": "2026-09-29T15:40:08.80+00:00" }
]
```

### Excluir

```http
DELETE /api/tasks/d0b377bf-818a-48d2-b242-cc23c3205266
```

Retorna `204 No Content`, ou `404 Not Found` se a tarefa não existir.

### Erro de validação

```http
POST /api/tasks
Content-Type: application/json

{ "title": "", "status": 99 }
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
    "status": ["Status must be one of: Pendente, EmProgresso, Concluida."]
  },
  "traceId": "0HNOU7ENKVVEP:00000001"
}
```

Um nome de status desconhecido (`"status": "Cancelada"` ou `?status=Cancelada`), datas em formato inválido (`"dueDate": "31/12/2026"` ou `?dueDate=abc`) e JSON malformado retornam o mesmo formato de erro, apontando o campo inválido.

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
│   ├── Entities/                     # TaskItem (agregado), TaskStatusChange (value object do histórico),
│   │                                 # TaskItemStatusDefinition (tabela de domínio de status)
│   ├── Enums/TaskItemStatus.cs       # Valores = Ids da tabela de domínio
│   ├── Exceptions/DomainException.cs
│   ├── Queries/TaskListCriteria.cs   # Critérios de listagem
│   └── Interfaces/                   # ITaskReadRepository, ITaskWriteRepository, ITaskStatusReadRepository
│
├── TaskManagement.Application/       # Casos de uso
│   ├── DTOs/                         # Requests, TaskResponse, TaskStatusChangeResponse, TaskStatusResponse, TaskFilterRequest
│   ├── Interfaces/                   # ITaskService, ITaskStatusService
│   ├── Services/                     # TaskService (validação, domínio, persistência, logging), TaskStatusService
│   ├── Validators/                   # FluentValidation
│   ├── Mappings/                     # Entidade → DTO
│   ├── Exceptions/TaskNotFoundException.cs
│   └── DependencyInjection.cs        # AddApplication()
│
├── TaskManagement.Infrastructure/    # Detalhes técnicos
│   ├── Data/TaskDbContext.cs
│   ├── Configurations/               # Mapeamento EF Core + carga inicial da tabela de status
│   ├── Repositories/                 # TaskRepository, TaskStatusRepository
│   └── DependencyInjection.cs        # AddInfrastructure(), InitializeDatabaseAsync()
│
└── TaskManagement.Api/               # Entrada HTTP
    ├── Controllers/                  # TasksController, TaskStatusesController
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
| **Domain** | Agregado `TaskItem` e suas regras: invariantes, regra de vencimento, histórico de status. Contratos de repositório. Não conhece EF, HTTP nem DTOs. | nada |
| **Application** | Casos de uso (`TaskService`): valida entrada, aciona o domínio, persiste via abstrações, registra logs e converte para DTOs. | Domain |
| **Infrastructure** | Implementação da persistência com EF Core InMemory (`TaskDbContext`, repositórios, configurações e carga da tabela de status). | Domain |
| **Api** | Recebe HTTP, delega ao serviço e devolve status codes adequados; tratamento global de erros; Swagger; composição da DI. | Application, Infrastructure |

---

## Decisões arquiteturais

### Por que camadas no estilo DDD / Clean Architecture

- **Motivos:** o desafio pede separação clara de responsabilidades, SOLID e testes da camada de negócio. Com o domínio e os casos de uso isolados de EF Core e HTTP, as regras são testáveis sem subir a API e a persistência pode ser trocada (ex.: SQL Server) alterando apenas a Infrastructure.
- **Vantagens:** baixo acoplamento, dependências explícitas via DI, cada projeto com um motivo único para mudar, testes focados.
- **Trade-offs:** mais projetos e arquivos do que um CRUD simples exigiria; um pouco de mapeamento manual (entidade ↔ DTO). Para um domínio pequeno isso é um custo consciente em troca de clareza e evolutividade. Não foram adotados CQRS/MediatR para não adicionar indireção sem necessidade real.

### SOLID na prática

- **SRP:** o controller só traduz HTTP; `TaskService` orquestra casos de uso; os validadores só validam; `TaskRepository` só persiste; `GlobalExceptionHandler` só converte exceções em respostas.
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

### Regra de vencimento (SLA)

- Uma tarefa está **vencida** quando `dueDate < hoje` **e** o status não é `Concluida`. Uma tarefa que vence hoje ainda está no prazo; sem `dueDate`, nunca está vencida.
- A regra vive no domínio (`TaskItem.IsOverdue` e `TaskItem.OverdueAsOf`). A segunda é uma *expression* usada pelo repositório no filtro `?overdue=`, então a consulta é traduzível para SQL em um banco relacional. Um teste garante que as duas formas concordam.
- `isOverdue` é **calculado na leitura**, não armazenado: uma tarefa passa a ficar vencida pela simples passagem do tempo, sem precisar de job ou atualização.
- "Hoje" usa o fuso horário local do servidor (`TimeProvider.GetLocalNow()`). **Trade-off:** em uma operação com unidades em fusos diferentes, o ideal seria associar a tarefa a um fuso (ex.: o do centro de distribuição) ou receber o prazo como data/hora com offset.

### Histórico de status (rastreabilidade)

- Toda tarefa nasce com uma entrada de histórico (`fromStatus = null`), e cada **mudança** de status adiciona outra. Atualizações que não mudam o status não geram entrada.
- Todas as transições são permitidas (inclusive reabrir uma tarefa `Concluida`). Como toda mudança fica registrada, reabrir é algo rastreável, e não um problema. Se o negócio exigir restrições, elas entram no método `TaskItem.Update`, sem afetar as outras camadas.
- `TaskStatusChange` é um **value object** dentro do agregado `TaskItem` (mapeado com `OwnsMany`): é carregado, salvo e excluído junto com a tarefa, e só o agregado pode alterá-lo.

### Status: enum + tabela de domínio

- No código, o status é o enum `TaskItemStatus` (entidade, DTOs e validação), com checagem de tipo em tempo de compilação e sem comparações de texto espalhadas.
- No banco, existe a **tabela de domínio** `TaskItemStatusDefinition` (Id + descrição), e o `Status` da tarefa e os campos do histórico são **chaves estrangeiras** para ela. O valor numérico do enum é o próprio Id da tabela, então os dois não podem divergir. Um teste garante que existe exatamente uma linha por valor do enum.
- A tabela é populada pela configuração do EF Core (`HasData`) e criada na inicialização (`InitializeDatabaseAsync`). É exposta em `GET /api/task-statuses`, para que clientes (front-end, integrações) descubram os valores válidos e suas descrições sem que elas fiquem fixas no código deles.
- **Na API**, o status trafega pelo **nome do enum** (`JsonStringEnumConverter`): o JSON fica legível e não depende de números "mágicos"; o número também é aceito na entrada. Nomes desconhecidos são rejeitados na desserialização (400 apontando o campo `status`). Números fora do enum (ex.: `99`) passam pela desserialização e são barrados pelo FluentValidation (`IsInEnum`).
- **Trade-offs:** os nomes do enum não têm espaço nem acento (`EmProgresso`, `Concluida`), então diferem da grafia do enunciado; a grafia exibível fica na tabela. Adicionar um novo status exige mudar o enum **e** a carga da tabela, algo intencional para um conjunto pequeno e estável como este. Se os status precisassem ser configuráveis em tempo de execução, o enum daria lugar a uma entidade consultada no banco.

### Concorrência (não implementada, de propósito)

Se duas pessoas editam a mesma tarefa ao mesmo tempo, a **última gravação vence**. Para o escopo do desafio, isso é aceitável e mantém a API simples. Se fosse necessário, a evolução seria o controle otimista: um campo de versão na tarefa, configurado como *concurrency token* no EF Core e exposto via `ETag` / `If-Match`, respondendo `412 Precondition Failed` quando a versão enviada estiver desatualizada.

### Idempotência (não implementada, de propósito)

Em logística, WMS, TMS e transportadoras integram por API e **reenviam requisições** quando há timeout. Um `POST` reenviado criaria uma tarefa duplicada. A solução seria aceitar um header `Idempotency-Key` no `POST` e guardar a resposta da primeira execução por um período. Não implementei porque exige um armazenamento com expiração (ex.: Redis) para funcionar corretamente com várias instâncias, o que foge do escopo "sem dependências externas" do desafio. `PUT` e `DELETE` já são idempotentes por definição.

### Persistência e Repository Pattern

- **EF Core InMemory** (`Microsoft.EntityFrameworkCore.InMemory`), configurado em `AddInfrastructure()`. Não requer banco externo; os dados vivem enquanto o processo estiver ativo.
- O mapeamento fica em `TaskItemConfiguration` (`IEntityTypeConfiguration`), mantendo a entidade livre de atributos de persistência. O status é armazenado como inteiro (FK para a tabela de domínio de status).
- O provider InMemory não aplica chaves estrangeiras; elas documentam o modelo e passam a ser garantidas pelo banco se o provider for trocado por um relacional.
- **Repository Pattern foi adotado** conscientemente: (1) Domain e Application não dependem de EF Core, respeitando DIP; (2) consultas de filtro, busca e ordenação ficam concentradas em um só lugar; (3) permite trocar o provider sem tocar nas regras. O trade-off é uma camada extra sobre o `DbContext` (que já é um Unit of Work). Por isso o repositório é fino e cada operação de escrita salva imediatamente, sem um Unit of Work adicional — suficiente para operações de agregado único como as desta API.
- Leituras usam `AsNoTracking()`; a escrita carrega a entidade rastreada (`FindForUpdateAsync`) e salva as alterações.
- Um único `TaskRepository` (scoped) atende às duas interfaces na mesma requisição.

### Estratégia de busca

`GET /api/tasks/search?term=...` faz uma busca do tipo **"contém", sem diferenciar maiúsculas/minúsculas**, no título **ou** na descrição (o termo é aparado). A consulta é escrita em LINQ traduzível (`ToLower().Contains(...)`), então funcionaria também em um provider relacional. Termo vazio é rejeitado com `400`. Para grandes volumes, a evolução natural seria full-text search do banco ou um motor dedicado (ex.: Elasticsearch), trocando apenas a implementação do repositório.

### Estratégia de validação

Validação em **duas camadas complementares**:

1. **Formato (model binding do ASP.NET Core):** JSON malformado e valores de tipo inválido (nome de status desconhecido, datas fora de `yyyy-MM-dd`, `overdue` não booleano) são rejeitados antes de chegar ao serviço. A resposta é customizada (`InvalidModelStateResponseFactory`) para seguir o mesmo formato de Problem Details, sem expor mensagens internas do serializador.
2. **Regras de entrada (FluentValidation, na Application):** título obrigatório/tamanho máximo, descrição com tamanho máximo, status obrigatório e dentro do enum (inclusive no filtro). O `TaskService` executa os validadores explicitamente e lança `ValidationException`, de modo que **as regras valem para qualquer consumidor do serviço** (não só a API) e são cobertas pelos testes de negócio.

Além disso, a entidade protege suas próprias invariantes (defesa em profundidade).

Não há restrição de data de vencimento no passado: é comum registrar tarefas já atrasadas (que aparecem com `isOverdue: true`).

### Estratégia de tratamento de erros

Centralizada em `GlobalExceptionHandler` (`IExceptionHandler`) + `AddProblemDetails()`, produzindo respostas **Problem Details (RFC 7807)** consistentes, com `type`, `title`, `status`, `detail`, `instance` e `traceId`:

| Exceção | Status | Title |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | Validation Error (com `errors` por campo) |
| `DomainException` | 400 | Business Rule Violation |
| `TaskNotFoundException` | 404 | Resource Not Found |
| qualquer outra | 500 | Internal Server Error (mensagem genérica, sem stack trace) |

Os controllers não têm `try/catch`: o fluxo principal fica limpo e o mapeamento erro → HTTP fica em um único lugar. Rotas inexistentes também retornam Problem Details (`UseStatusCodePages`). Rotas com `{id:guid}` retornam 404 para IDs que não são GUID.

### Logging

`ILogger<T>` nativo, com logs estruturados:

- `Information`: criação, atualização (incluindo a transição de status, ex.: `Pendente → EmProgresso`) e exclusão de tarefas — apenas ID e status, sem título/descrição, evitando registrar conteúdo possivelmente sensível; respostas de erro 4xx tratadas.
- `Warning`: falhas de validação (apenas os nomes dos campos inválidos).
- `Error`: exceções inesperadas, com a exceção completa (somente no log, nunca na resposta).

### Swagger / OpenAPI

Swashbuckle com comentários XML dos controllers e DTOs (descrições, exemplos de request/response e status codes via `ProducesResponseType`). O `status` aparece como enum de nomes (via `JsonStringEnumConverter`), e um filtro próprio acrescenta a cada valor o Id e a descrição da tabela de domínio. Outro filtro exibe os parâmetros de query em camelCase. O Swagger fica habilitado em todos os ambientes para facilitar a avaliação; em produção real, o ideal seria restringi-lo.

### Versão do .NET

Alvo `net8.0` (LTS), atendendo ao requisito ".NET 8+" com a maior compatibilidade. Configurações comuns (target, nullable, implicit usings) ficam em `Directory.Build.props`.
