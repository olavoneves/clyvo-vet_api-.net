# Clyvo-Vet.net — API REST de Clínica Veterinária

API REST em ASP.NET Core 8 para gerenciamento de uma clínica veterinária. Permite cadastrar tutores, pets e consultas, com relacionamentos entre eles.

## Rotas

### Tutores

| Método | Rota                    | Descrição                        | Resposta |
|--------|-------------------------|----------------------------------|----------|
| GET    | /tutores                | Lista todos os tutores           | 200      |
| GET    | /tutores/{id}           | Busca tutor por ID               | 200/404  |
| GET    | /tutores/{id}/pets      | Lista os pets de um tutor        | 200/404  |
| POST   | /tutores                | Cadastra novo tutor              | 201/400  |
| PUT    | /tutores/{id}           | Atualiza tutor                   | 204/400/404 |
| DELETE | /tutores/{id}           | Remove tutor                     | 204/404  |

### Pets

| Método | Rota                    | Descrição                        | Resposta |
|--------|-------------------------|----------------------------------|----------|
| GET    | /pets                   | Lista todos os pets              | 200      |
| GET    | /pets/{id}              | Busca pet por ID                 | 200/404  |
| GET    | /pets/{id}/consultas    | Lista consultas de um pet        | 200/404  |
| POST   | /pets                   | Cadastra novo pet                | 201/400  |
| PUT    | /pets/{id}              | Atualiza pet                     | 204/400/404 |
| DELETE | /pets/{id}              | Remove pet                       | 204/404  |

### Consultas

| Método | Rota                         | Descrição                              | Resposta |
|--------|------------------------------|----------------------------------------|----------|
| GET    | /consultas                   | Lista todas as consultas               | 200      |
| GET    | /consultas/{id}              | Busca consulta por ID                  | 200/404  |
| GET    | /consultas/tutor/{tutorId}   | Lista consultas de todos os pets de um tutor | 200/404 |
| POST   | /consultas                   | Cadastra nova consulta                 | 201/400  |
| PUT    | /consultas/{id}              | Atualiza consulta                      | 204/400/404 |
| DELETE | /consultas/{id}              | Remove consulta                        | 204/404  |

Swagger disponível em: `https://localhost:{porta}/swagger`

## Instalação

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Acesso à instância Oracle FIAP

### Configurar a connection string

Edite `server.net/appsettings.json` e substitua o `User Id` pelo seu RM e o `Password` pela sua senha:

```json
"OracleConnection": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)));User Id=RM000000;Password=000000;"
```

### Executar

```bash
cd server.net
dotnet restore
dotnet run
```

### Migrations (validação do mapeamento EF Core)

```bash
# Instalar ferramenta (apenas uma vez)
dotnet tool install --global dotnet-ef --version 8.0.0

cd server.net
dotnet ef migrations add InitialCreate
```
