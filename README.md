<h1 align="center">Emissor de Nota Fiscal</h1>

<p align="center">
  Desafio técnico <strong>Korp</strong> · Augusto Farias dos Santos
</p>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white">
  <img alt="Angular 21" src="https://img.shields.io/badge/Angular-21-DD0031?style=flat-square&logo=angular&logoColor=white">
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-4169E1?style=flat-square&logo=postgresql&logoColor=white">
  <img alt="SQL Server" src="https://img.shields.io/badge/SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white">
  <img alt="RabbitMQ" src="https://img.shields.io/badge/RabbitMQ-FF6600?style=flat-square&logo=rabbitmq&logoColor=white">
  <img alt="Docker" src="https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white">
</p>

O projeto foi criado na arquitetura de microsserviços, e cada sistema tem a sua responsabilidade.

---

## Índice

| | Seção |
|---|---|
| **1** | [Como rodar o projeto com o Docker](#1--como-rodar-o-projeto-com-o-docker) |
| 1.1 | [Habilitar a funcionalidade com IA](#11--habilitar-a-funcionalidade-com-ia) |
| 1.2 | [Buildando as imagens Docker](#12--buildando-as-imagens-docker) |
| 1.3 | [Caso dê erro](#13--caso-dê-erro) |
| **2** | [Como rodar o projeto sem Docker](#2--como-rodar-o-projeto-sem-docker) |
| **3** | [Testes](#3--testes) |
| **4** | [Arquitetura](#4--arquitetura) |

---

## 1 · Como rodar o projeto com o Docker

> [!IMPORTANT]
> **Você só precisa ter o Docker instalado.** Não é preciso instalar .NET, Node nem banco de dados.

### 1.1 · Habilitar a funcionalidade com IA

O sistema tem um assistente que ajuda a cadastrar produtos na nota fiscal.

> [!TIP]
> Configure a IA **antes** de subir os contêineres (seção 1.2). O Docker copia o `API-Faturamento/Faturamento.Api/appsettings.json` durante o build — se colar a chave depois, será preciso buildar de novo.

#### Consiga uma chave

A mais simples é a do **Google (Gemini)**, gratuita: acesse [aistudio.google.com/apikey](https://aistudio.google.com/apikey), entre com uma conta Google e clique em *Create API key*.

#### Cole a chave no projeto

Abra o arquivo abaixo — é o do **Faturamento**, o único serviço que fala com a IA:

```
API-Faturamento/Faturamento.Api/appsettings.json
```

Localize a seção `Ai`. Ela já vem pronta para o Gemini, e só o campo `ApiKey` está vazio:

```json
"Ai": {
  "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/openai",
  "ApiKey": "COLE-SUA-CHAVE-AQUI",
  "Model": "gemini-3.6-flash",
  "MaxTokens": 1024,
  "TimeoutSeconds": 30
}
```

#### Prefere outra IA?

Caso não queira testar com a IA do Google, o sistema de faturamento está preparado para suportar **qualquer IA e qualquer modelo**. Basta substituir a seção `Ai` inteira por um dos blocos abaixo — nenhuma linha de código muda.

<details>
<summary><strong>OpenAI</strong> — chave paga</summary>

```json
"Ai": {
  "BaseUrl": "https://api.openai.com/v1",
  "ApiKey": "COLE-SUA-CHAVE-AQUI",
  "Model": "gpt-4o-mini",
  "MaxTokens": 1024,
  "TimeoutSeconds": 30
}
```

</details>

<details>
<summary><strong>Anthropic</strong> — chave paga</summary>

```json
"Ai": {
  "BaseUrl": "https://api.anthropic.com/v1",
  "ApiKey": "COLE-SUA-CHAVE-AQUI",
  "Model": "claude-haiku-4-5",
  "MaxTokens": 1024,
  "TimeoutSeconds": 30
}
```

</details>

<details>
<summary><strong>Ollama</strong> — roda na sua própria máquina, sem pagar nada e sem criar conta</summary>

```json
"Ai": {
  "BaseUrl": "http://host.docker.internal:11434/v1",
  "ApiKey": "ollama",
  "Model": "llama3.2",
  "MaxTokens": 1024,
  "TimeoutSeconds": 120
}
```

</details>

#### Se der errado

**O campo roxo não vai aparecer na tela.** Em caso de erro, o projeto continua funcionando normalmente — apenas a funcionalidade com IA fica desligada.

---

### 1.2 · Buildando as imagens Docker

Na pasta **`Korp_Teste_AugustoFariasDosSantos/`**, abra o terminal e rode:

```bash
docker compose --profile app up -d --build
```

Esse comando cria as imagens e sobe tudo de uma vez, sem precisar rodar cada projeto localmente.

Quando terminar, acesse:

| | |
|---|---|
| **Endereço** | <http://localhost:5000> |
| **Usuário** | `admin@admin.com` |
| **Senha** | `Admin123!` |

> [!NOTE]
> Na primeira vez o SQL Server demora cerca de um minuto para iniciar. Se alguma tela disser que não foi possível carregar, espere um pouco e tente novamente.

---

### 1.3 · Caso dê erro

Se o comando do Docker falhar, ou se algum dos sistemas der erro ao iniciar, derrube tudo:

```bash
docker compose --profile app down -v
```

E suba novamente:

```bash
docker compose --profile app up -d --build
```

---

## 2 · Como rodar o projeto sem Docker

Neste modo é preciso instalar o SDK do .NET e o Node.js — e ainda assim ter o Docker, para subir os bancos e o RabbitMQ.

| | Versão | Conferir com |
|---|---|---|
| **.NET SDK** | 10.0.400 ou maior | `dotnet --version` |
| **Node.js** | 20 ou maior | `node --version` |
| **Docker** | qualquer versão | `docker --version` |

**Primeiro**, suba só os bancos e a fila:

```bash
docker compose up -d
```

**Depois**, abra quatro terminais, um para cada parte — ou abra cada projeto na IDE e rode pelo F5 do Visual Studio, que também funciona:

```text
Primeiro cd API-Gateway     Segundo dotnet run --project Gateway          # :5000
Primeiro cd API-Estoque     Segundo dotnet run --project Estoque.Api      # :5247
Primeiro cd API-Faturamento Segundo dotnet run --project Faturamento.Api  # :5108
Primeiro cd NotaFlow        Segundo npm install Terceiro npm start              # :4200
```

Neste modo o endereço é **http://localhost:4200**, e não o :5000. O front chama `/api/v1` relativo e o `proxy.conf.json` encaminha `/api` para o gateway — é isso que mantém uma origem só, exigência do cookie de sessão `SameSite=Strict`.

Se o `--profile app` ainda estiver no ar, derrube antes com `docker compose --profile app down`: os contêineres publicam as mesmas portas 5000, 5247 e 5108 que o modo local usa.

---

## 3 · Testes

```text
Primeiro cd API-Estoque     Segundo dotnet test
Primeiro cd API-Faturamento Segundo dotnet test
Primeiro cd API-Gateway     Segundo dotnet test
Primeiro cd NotaFlow        Segundo npx ng test --watch=false
```

Só os unitários, sem Docker:

```bash
dotnet test Testes/TestesUnitarios
```

Só os de integração, com Docker:

```bash
dotnet test Testes/TestesIntegracao
```

---


## 4 · Arquitetura

![Arquitetura do sistema](Documentacao/Assets/System-Design.jpg)
