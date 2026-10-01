# Desafio: Sistema de Gestão de Tarefas

## 1. Contexto

Criar uma aplicação web **backend-only** para gerenciamento de tarefas.

O objetivo é disponibilizar uma API RESTful que permita aos usuários:

- Criar tarefas.
- Consultar tarefas.
- Filtrar tarefas.
- Editar tarefas.
- Excluir tarefas.
- Buscar tarefas de forma organizada.

Não é necessário desenvolver frontend.

---

# 2. Objetivos do Projeto

A solução deve demonstrar:

- Utilizar DDD, desenvolvendo o projeto em camadas
- Desenvolvimento de uma API RESTful utilizando .NET 8+.
- Aplicação dos princípios SOLID.
- Separação clara de responsabilidades.
- Uso de Entity Framework Core.
- Persistência utilizando banco **InMemory**.
- Utilização de DTOs.
- Injeção de dependência.
- Validação de dados.
- Tratamento adequado de erros.
- Logging básico.
- Documentação através do Swagger/OpenAPI.
- Testes automatizados da camada de negócio.
- Código limpo, organizado e de fácil manutenção.
- Não utilizar minimal api

---

# 3. Requisitos Funcionais

## 3.1 Cadastro de Tarefa

A API deve permitir a criação de uma nova tarefa.

### Campos

| Campo | Obrigatório | Descrição |
|---|---|---|
| `title` | Sim | Título da tarefa |
| `description` | Não | Descrição da tarefa |
| `dueDate` | Não | Data de vencimento |
| `status` | Sim | Status atual da tarefa |

### Status permitidos

- `Pendente`
- `Em progresso`
- `Concluída`

Cada tarefa criada deve possuir um **código/identificador único**.

### Comportamento esperado

- Validar os dados de entrada.
- Criar a tarefa.
- Retornar HTTP `201 Created`.
- Retornar os dados da tarefa criada.
- Disponibilizar a localização do recurso criado quando aplicável.

---

# 4. Listagem de Tarefas

A API deve permitir consultar todas as tarefas cadastradas.

Deve ser possível filtrar por:

- Status.
- Data de vencimento.
- Status + data de vencimento.

### Exemplo conceitual

```http
GET /api/tasks
GET /api/tasks?status=Pendente
GET /api/tasks?dueDate=2026-10-01
GET /api/tasks?status=Pendente&dueDate=2026-10-01
```

A implementação pode adotar outros parâmetros ou formatos desde que mantenha uma API RESTful e documentada.

---

# 5. Busca de Tarefas

O sistema deve disponibilizar uma funcionalidade de busca de tarefas.

A busca deve permitir localizar tarefas de forma prática, preferencialmente utilizando o título e/ou descrição.

### Exemplo conceitual

```http
GET /api/tasks/search?term=relatório
```

A estratégia definitiva de busca fica a critério da arquitetura escolhida, desde que seja documentada.

---

# 6. Consulta de Tarefa por ID

A API deve permitir consultar uma tarefa específica através do seu identificador.

### Exemplo

```http
GET /api/tasks/{id}
```

Caso a tarefa não exista, retornar:

```http
404 Not Found
```

---

# 7. Edição de Tarefa

A API deve permitir editar uma tarefa existente.

Os seguintes campos devem poder ser atualizados:

- Título.
- Descrição.
- Status.
- Data de vencimento.

### Exemplo conceitual

```http
PUT /api/tasks/{id}
```

Caso a tarefa não exista:

```http
404 Not Found
```

Caso os dados sejam inválidos:

```http
400 Bad Request
```

---

# 8. Exclusão de Tarefa

A API deve permitir excluir uma tarefa.

### Exemplo

```http
DELETE /api/tasks/{id}
```

Após exclusão bem-sucedida:

```http
204 No Content
```

Caso a tarefa não exista:

```http
404 Not Found
```

---

# 9. Requisitos Técnicos

## 9.1 Framework

Utilizar:

- .NET 8 ou versão mais recente.
- ASP.NET Core Web API.

---

## 9.2 Arquitetura

A arquitetura fica a critério do desenvolvedor.

A solução deve, entretanto, possuir responsabilidades bem separadas e ser modular.

Uma sugestão de organização:

```text
src/
├── TaskManagement.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Extensions/
│   └── Program.cs
│
├── TaskManagement.Application/
│   ├── DTOs/
│   ├── Services/
│   ├── Interfaces/
│   └── Validators/
│
├── TaskManagement.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Interfaces/
│
└── TaskManagement.Infrastructure/
    ├── Data/
    ├── Repositories/
    └── Configurations/

tests/
└── TaskManagement.Tests/
```

Esta estrutura é apenas uma sugestão.

O desenvolvedor pode utilizar outra arquitetura, desde que explique no README:

1. Qual arquitetura foi escolhida.
2. Por que ela foi escolhida.
3. Quais são as responsabilidades de cada camada.
4. Quais são as vantagens e possíveis trade-offs da abordagem.

---

# 10. SOLID

A implementação deve aplicar os princípios SOLID de forma adequada.

Especial atenção para:

### Single Responsibility Principle

Cada classe deve possuir uma responsabilidade clara.

### Open/Closed Principle

Evitar alterações desnecessárias em componentes existentes para adicionar novos comportamentos.

### Liskov Substitution Principle

As abstrações devem poder ser substituídas pelas respectivas implementações.

### Interface Segregation Principle

Evitar interfaces grandes e genéricas.

### Dependency Inversion Principle

As camadas superiores devem depender de abstrações, e não diretamente das implementações concretas.

---

# 11. Entity Framework Core

Utilizar Entity Framework Core para persistência.

O banco de dados deve utilizar o provider:

```text
Microsoft.EntityFrameworkCore.InMemory
```

Não deve ser necessário configurar um banco externo para executar o projeto.

---

# 12. Modelo de Dados

Uma entidade `Task` deve conter, no mínimo:

```text
Id
Title
Description
DueDate
Status
```

### Identificador

O identificador deve ser único.

Pode ser utilizado:

- `Guid`

A escolha deve ser documentada.

---

# 13. DTOs

A API não deve expor diretamente as entidades de domínio.

Utilizar DTOs para comunicação entre as camadas.

Sugestão:

```text
CreateTaskRequest
UpdateTaskRequest
TaskResponse
TaskFilterRequest
```

Exemplo conceitual:

```json
{
  "title": "Implementar API",
  "description": "Criar endpoints de tarefas",
  "dueDate": "2026-10-01",
  "status": "Pendente"
}
```

---

# 14. Validação

A API deve validar adequadamente os dados recebidos.

Exemplos:

### Título

- Obrigatório.
- Não pode ser vazio.
- Deve possuir tamanho máximo definido.

### Status

Deve aceitar somente:

```text
Pendente
Em progresso
Concluída
```

### Data de vencimento

Quando informada, deve possuir formato válido.

A solução pode utilizar:

- Data Annotations.
- FluentValidation.
- Ou outra abordagem apropriada.

A estratégia escolhida deve ser documentada.

---

# 15. HTTP Status Codes

A API deve utilizar códigos HTTP apropriados.

| Situação | Status |
|---|---:|
| Consulta realizada com sucesso | `200 OK` |
| Criação realizada | `201 Created` |
| Exclusão realizada | `204 No Content` |
| Dados inválidos | `400 Bad Request` |
| Recurso não encontrado | `404 Not Found` |
| Erro inesperado | `500 Internal Server Error` |

Outros códigos podem ser utilizados quando fizerem sentido.

---

# 16. Tratamento de Erros

Implementar tratamento centralizado de erros.

Uma abordagem recomendada é utilizar:

- Middleware global.
- `ExceptionHandlerMiddleware`.
- Ou `IExceptionHandler` / Problem Details.

As respostas de erro devem possuir formato consistente.

Exemplo:

```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "The request contains invalid data."
}
```

Evitar retornar stack traces ou detalhes internos da aplicação para o consumidor da API.

---

# 17. Logging

Implementar logging básico utilizando as abstrações nativas do .NET:

```csharp
ILogger<T>
```

Registrar eventos relevantes, como:

- Criação de tarefa.
- Atualização de tarefa.
- Exclusão de tarefa.
- Erros inesperados.
- Falhas de validação quando relevante.

Não registrar informações sensíveis desnecessariamente.

---

# 18. Swagger / OpenAPI

A API deve possuir documentação através do Swagger/OpenAPI.

A documentação deve permitir:

- Visualizar os endpoints.
- Visualizar parâmetros.
- Visualizar request bodies.
- Visualizar responses.
- Testar os endpoints diretamente pela interface Swagger.

Sempre que possível, documentar:

- Status codes.
- Exemplos de request.
- Exemplos de response.
- Possíveis erros.

---

# 19. Testes Automatizados

Implementar testes automatizados utilizando:

- xUnit
- ou NUnit.

O foco principal deve ser a **camada de lógica de negócio**.

Devem existir testes para, no mínimo:

### Criação

- Criar tarefa com dados válidos.
- Rejeitar tarefa sem título.
- Rejeitar status inválido.

### Consulta

- Listar tarefas.
- Buscar tarefa existente.
- Retornar comportamento adequado quando a tarefa não existe.

### Filtros

- Filtrar por status.
- Filtrar por data.
- Filtrar por status e data.

### Busca

- Buscar por título.
- Buscar por descrição.
- Retornar lista vazia quando não houver correspondências.

### Atualização

- Atualizar tarefa existente.
- Tentar atualizar tarefa inexistente.
- Validar dados durante atualização.

### Exclusão

- Excluir tarefa existente.
- Tentar excluir tarefa inexistente.

---

# 20. Cobertura de Testes

A cobertura deve ser suficiente para demonstrar que as principais regras de negócio estão protegidas.

Não é necessário buscar cobertura de 100% artificialmente.

A prioridade deve ser testar:

- Regras de negócio.
- Validações.
- Cenários de sucesso.
- Cenários de erro.
- Casos de borda relevantes.

---

# 21. Injeção de Dependência

Utilizar o mecanismo nativo de Dependency Injection do ASP.NET Core.

Exemplo conceitual:

```text
Controller
    ↓
Service
    ↓
Repository
    ↓
DbContext
```

As dependências devem ser registradas no container de DI.

Evitar instanciação manual de serviços dentro dos controllers.

---

# 22. Repository Pattern

A utilização de Repository Pattern pode ser adotada para abstrair o acesso aos dados.

Exemplo:

```csharp
ITaskRepository
TaskRepository
```

A decisão de utilizar ou não esse padrão deve ser justificada no README.

O objetivo não é aplicar design patterns apenas por formalidade, mas demonstrar uma decisão arquitetural consciente.

---

# 23. Controller

Os controllers devem ser responsáveis principalmente por:

- Receber requisições HTTP.
- Validar o modelo de entrada.
- Encaminhar a operação para a camada apropriada.
- Retornar respostas HTTP adequadas.

Evitar colocar regras de negócio diretamente nos controllers.

Exemplo conceitual:

```text
TasksController
    ↓
ITaskService
    ↓
ITaskRepository
    ↓
TaskDbContext
```

---

# 24. RESTful API

Os endpoints devem seguir boas práticas REST.

Exemplo:

```http
GET    /api/tasks
GET    /api/tasks/{id}
POST   /api/tasks
PUT    /api/tasks/{id}
DELETE /api/tasks/{id}
```

Para busca:

```http
GET /api/tasks/search?term=...
```

Os nomes e verbos HTTP devem ser consistentes.

---

# 25. Organização do Projeto

A solução deve possuir uma organização clara.

Exemplo:

```text
TaskManagement.sln

src/
    TaskManagement.Api/
    TaskManagement.Application/
    TaskManagement.Domain/
    TaskManagement.Infrastructure/

tests/
    TaskManagement.Tests/

README.md
.gitignore
```

A estrutura pode ser adaptada de acordo com a arquitetura escolhida.

---

# 26. README.md

O projeto deve possuir um README.md completo.

O README deve explicar:

## Visão geral

Descrição do projeto e seu objetivo.

## Arquitetura

Explicação da arquitetura escolhida.

## Tecnologias

Listar as principais tecnologias utilizadas.

Exemplo:

```text
.NET 8+
ASP.NET Core
Entity Framework Core
EF Core InMemory
Swagger / OpenAPI
xUnit ou NUnit
```

## Pré-requisitos

Informar o que é necessário para executar o projeto.

Exemplo:

```text
.NET SDK 8+
```

## Como executar

Incluir os comandos necessários.

Exemplo:

```bash
dotnet restore
dotnet build
dotnet run
```

## Swagger

Informar a URL do Swagger após iniciar a aplicação.

## Como executar os testes

Exemplo:

```bash
dotnet test
```

## Exemplos de utilização

Documentar exemplos de requests e responses.

## Decisões arquiteturais

Explicar:

- Arquitetura escolhida.
- Motivos.
- Trade-offs.
- Organização das camadas.
- Decisões relacionadas a persistência.
- Estratégia de validação.
- Estratégia de tratamento de erros.

---

# 27. Git

O projeto deve ser disponibilizado em um repositório GitHub ou plataforma similar.

Recomenda-se utilizar commits organizados e mensagens claras.

Não versionar:

```text
bin/
obj/
.vs/
.env
*.user
```

Caso existam configurações sensíveis, utilizar variáveis de ambiente ou arquivos de configuração apropriados.

---

# 28. Critérios de Avaliação

A solução será avaliada considerando:

## Qualidade do Código

- Código limpo.
- Legibilidade.
- Organização.
- Nomenclatura adequada.
- Baixo acoplamento.

## Boas Práticas

- SOLID.
- Design Patterns quando fizerem sentido.
- Dependency Injection.
- Separação de responsabilidades.
- DTOs.
- Tratamento de erros.

## Funcionamento da API

Todos os requisitos funcionais devem estar implementados e funcionando.

## Testes

Avaliar:

- Quantidade de cenários cobertos.
- Qualidade dos testes.
- Cobertura das regras de negócio.
- Cenários de erro.

## Documentação

Avaliar:

- Swagger.
- README.
- Instruções para execução.
- Exemplos de utilização.
- Explicação da arquitetura.

## Estrutura do Projeto

Avaliar:

- Modularidade.
- Separação de responsabilidades.
- Organização das camadas.
- Facilidade de manutenção.

---

# 29. Entrega

A entrega deve conter:

- Repositório GitHub ou similar.
- Código-fonte completo.
- Testes automatizados.
- Swagger/OpenAPI.
- README.md.
- Explicação da arquitetura.
- Instruções para execução.
- Instruções para execução dos testes.

---

# 30. Restrições

- O projeto é exclusivamente backend.
- Não é necessário desenvolver frontend.
- Não é necessário utilizar banco de dados externo.
- O banco deve utilizar Entity Framework Core InMemory.
- A API deve ser executável localmente sem dependências externas obrigatórias.

---

# 31. Resultado Esperado

Ao final, deve existir uma API RESTful funcional capaz de:

```text
Criar tarefa
      ↓
Consultar tarefa
      ↓
Listar tarefas
      ↓
Filtrar tarefas
      ↓
Buscar tarefas
      ↓
Editar tarefa
      ↓
Excluir tarefa
```

A implementação deve demonstrar não apenas que os endpoints funcionam, mas também boas decisões de arquitetura, qualidade de código, testes, documentação e separação de responsabilidades.

---

# 32. Checklist Final

Antes da entrega, verificar:

- [ ] Projeto utiliza .NET 8+.
- [ ] API RESTful implementada.
- [ ] Cadastro de tarefas funcionando.
- [ ] Identificador único gerado.
- [ ] Listagem funcionando.
- [ ] Filtro por status funcionando.
- [ ] Filtro por data funcionando.
- [ ] Busca de tarefas funcionando.
- [ ] Consulta por ID funcionando.
- [ ] Edição funcionando.
- [ ] Exclusão funcionando.
- [ ] DTOs utilizados.
- [ ] Entity Framework Core utilizado.
- [ ] EF Core InMemory configurado.
- [ ] SOLID aplicado.
- [ ] Dependency Injection configurada.
- [ ] Validações implementadas.
- [ ] Tratamento global de erros implementado.
- [ ] Logging implementado.
- [ ] Swagger configurado.
- [ ] Testes automatizados implementados.
- [ ] README.md criado.
- [ ] Arquitetura documentada.
- [ ] Instruções de execução documentadas.
- [ ] Instruções de testes documentadas.
- [ ] Repositório Git organizado.
