# ASP.NET Core + PostgreSQL na Master da Web

Um app ASP.NET Core (.NET 10) com PostgreSQL pelo Entity Framework Core, as migrações a cada deploy, arquivos privados no Object Storage e as chaves de login guardadas no banco.

| Serviço | O que roda |
| --- | --- |
| `web` | o projeto `src/MwApp`; `./efbundle` (as migrações do EF Core) roda uma vez por deploy, antes de a nova versão receber tráfego |

O banco chega como `ConnectionStrings__DefaultConnection`, no formato do Npgsql, e o app o lê com `GetConnectionString("DefaultConnection")`: pelo pooler para o app, direto no servidor para as migrações. O bucket privado chega como `MW_STORAGE_*`; cada link de download é assinado e expira em 15 minutos.

`POST /api/files` (campo `file`) guarda um arquivo; `GET /api/files` lista com os links. A página inicial faz o mesmo pelo navegador, com o token antifalsificação do ASP.NET.

## Como a plataforma monta o app

Não há Dockerfile: o build da plataforma encontra a solução (`MwApp.sln`), escolhe o projeto web, publica-o self-contained e roda o app como usuário sem privilégios na porta que a plataforma informa (`$PORT`). Como o projeto referencia `Microsoft.EntityFrameworkCore.Design`, o build também gera o `efbundle` com as migrações.

- Uma solução com mais de um app web? Crie a variável `PROJECT_FILE` (por exemplo `src/Loja.Web/Loja.Web.csproj`) marcada como **Disponível no build**.
- `BUILD_CONFIGURATION` e `MSBUILD_VERBOSITY_LEVEL` também valem, como no Heroku.
- Prefere o seu próprio build? Um `Dockerfile` na raiz tem prioridade.

## Torne o código seu

1. No console, abra o projeto e siga **Seu código**: copie este template
   para o seu GitHub e conecte a cópia ao projeto.
2. Altere `src/MwApp` e faça push: cada push faz um deploy, sem tirar o app do ar.

## Rodar na sua máquina

```bash
dotnet run --project src/MwApp      # usa o PostgreSQL de appsettings.json
```

Uma migração nova:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add NomeDaMudanca --project src/MwApp
```

## Bom saber

- Login e formulários usam o Data Protection do ASP.NET, cujas chaves ficam na tabela `DataProtectionKeys`: todas as instâncias abrem o que qualquer outra cifrou, antes e depois de cada deploy. Sem isso, cada instância teria chaves próprias no disco do container e os logins cairiam a cada reinício.
- O app recebe o esquema e o IP do visitante pelos cabeçalhos `X-Forwarded-*` do proxy da plataforma (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`), então links e redirecionamentos saem em `https`.

*[English](README.en.md)*
