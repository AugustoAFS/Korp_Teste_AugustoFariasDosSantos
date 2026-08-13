# Arquitetura — API Gateway

Ponto único de entrada · .NET 10 · YARP · PostgreSQL

## Estrutura — MVC, projeto único

Sem Clean Architecture aqui: o gateway não tem domínio próprio, só roteia, autentica e emite cookie.

Solution `API-Gateway.slnx` com um projeto.

```
API-Gateway/
  .dockerignore
  API-Gateway.slnx
  Gateway/
    Config/
      AuthConfig.cs           cookie + antiforgery
      ReverseProxyConfig.cs   YARP + transform do JWT interno
      ResilienceConfig.cs     Polly (circuit breaker)
      OpenApiConfig.cs        OpenAPI + Scalar
      VersioningConfig.cs
      RateLimitConfig.cs      global
      CorsConfig.cs           confg para informar quem podfe acessar 
      HealthCheckConfig.cs    /health
    Controllers/             
    Data/                     
    DependencyInjecion/       
    Dtos/
      Request/                 
      Response/                
    Middleware/
      ExceptionMiddleware.cs
    Models/                     
    Repositories/
      Interfaces/             
      UsuarioRepository.cs
    Security/                 
    Services/
      Interfaces/              
      AuthService.cs
    Properties/
    Dockerfile
    Program.cs
```

## Responsabilidades

- Autenticação local (Argon2id)
- Emissão do cookie de sessão
- Roteamento YARP para Estoque e Faturamento
- Injeção do JWT interno assinado
- Circuit breaker (Polly) com feedback tratado quando um serviço está fora

## Endpoints

```
POST   /api/v1/auth/login
POST   /api/v1/auth/logout
GET    /api/v1/auth/me
POST   /api/v1/usuarios
```

Circuito aberto → 503 com `ProblemDetails` tratado, não erro cru.

## Tratamento de erros

| Situação | Retorno |
|---|---|
| Credencial inválida | 401 (mensagem genérica, sem revelar se o e-mail existe) |
| Usuário bloqueado / inativo | 403 |
| Sem perfil para a rota | 403 |
| Serviço downstream fora | 503 + `ProblemDetails` |
| Não tratado | middleware → 500 + `ProblemDetails` |
