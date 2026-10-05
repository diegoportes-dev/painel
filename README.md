# 🚀 AspNetCore Minimal API - CRUD

Uma API Minimalista desenvolvida em ASP.NET Core para operações de CRUD (Create, Read, Update, Delete), utilizando SQLite como banco de dados local.

## 🛠️ Tecnologias Utilizadas

* **.NET 10.0** (SDK 10.0.300 / Runtime 10.0.8)
* **ASP.NET Core Minimal APIs**
* **Entity Framework Core**
* **SQLite** (Banco de dados local em arquivo)
* **Scalar** (Documentação interativa e execução das APIs)
* **MailKit** (Envio robusto e assíncrono de e-mails via SMTP)
* **BCrypt.Net-Next** (Criptografia segura de senhas com hashing)

## ☁️ Implantação no Render

O repositório inclui um `render.yaml` e um Dockerfile multi-stage para publicar a API como um **Web Service** no Render:

1. Envie o repositório para o GitHub e, no Render, selecione **New > Blueprint** e conecte esse repositório. O Render usará o `render.yaml` para criar o serviço.
2. Informe `ConnectionStrings__CentralConnection` quando solicitado. Use a string de conexão do seu servidor MySQL externo; o Render não fornece MySQL gerenciado.
3. O segredo `JwtSettings__ChaveSecreta` é gerado pelo Blueprint. Se configurar o serviço manualmente, crie essa variável com pelo menos 32 caracteres.
4. Faça o deploy. O container escuta na porta informada pelo Render (`PORT`, com fallback local para `10000`).

Configure também no painel do Render as variáveis necessárias para os recursos que utilizar:

* SMTP: `EmailSettings__SmtpServer`, `EmailSettings__Port`, `EmailSettings__SenderName`, `EmailSettings__SenderEmail`, `EmailSettings__Username` e `EmailSettings__Password`.
* Criação de bancos de tenants: `DatabaseConfig__ServidorMaster`, `DatabaseConfig__UsuarioMaster`, `DatabaseConfig__SenhaMaster`, `DatabaseConfig__ServidorTenant`, `DatabaseConfig__UsuarioTenant` e `DatabaseConfig__SenhaTenant`.

Use valores e credenciais do seu provedor de banco/e-mail e mantenha segredos nas variáveis de ambiente do Render, nunca no repositório. O serviço principal usa MySQL e precisa de um servidor MySQL acessível pela rede; o banco SQLite local não é armazenamento persistente apropriado para produção no Render.

## 📦 Como Executar o Projeto

Siga os passos abaixo para clonar e rodar o projeto localmente na sua máquina:

### 1. Clonar o Repositório
```bash
git clone https://github.com
cd AspNetCore
```

### 2. Restaurar as Dependências
```bash
dotnet restore
```

### 3. Iniciar o Banco de Dados e Construir as Tabelas (Migrations)
Para inicializar o banco de dados SQLite do zero e criar a estrutura das tabelas, execute os comandos do Entity Framework Core no terminal:
Obs.: É necessário executar as migrations para ambos os contextos;

```bash
# 1. Cria o histórico inicial da estrutura do banco
dotnet ef migrations add InitialCreate --context CrudContext
dotnet ef migrations add InitialCreate --context TenantDbContext

# 2. Executa a migração, gerando o arquivo do banco e construindo as tabelas
dotnet ef database update --context CrudContext
dotnet ef database update --context TenantDbContext
```

### 4. Configurar as Variáveis de Ambiente (`appsettings.json`)
Antes de rodar a aplicação, abra o arquivo `appsettings.json` na raiz do projeto e configure as credenciais para o envio do link de recuperação de senha.

#### 📧 Cenário A: Desenvolvimento Local (Mailtrap - Recomendado)
Para testar o fluxo sem enviar e-mails reais e evitar bloqueios de IP, crie uma conta gratuita no [Mailtrap](https://mailtrap.io), acesse sua *Inbox* e copie as credenciais numéricas de SMTP:

```json
"EmailSettings": {
  "SmtpServer": "sandbox.smtp.mailtrap.io",
  "Port": 2525,
  "SenderName": "Sistema de Teste",
  "SenderEmail": "suporte@seudominio.com",
  "Username": "SUO_CODIGO_NUMERICO", 
  "Password": "SUA_SENHA_NUMERICA" 
}
```

#### ✉️ Cenário B: Produção / Teste Real (Gmail)
Caso queira disparar e-mails reais usando os servidores do Google, use a porta seguro TLS (`587`) e gere uma **Senha de App de 16 dígitos** no painel de segurança da sua Conta Google (a senha comum de login do e-mail será rejeitada):

```json
"EmailSettings": {
  "SmtpServer": "://gmail.com",
  "Port": 587,
  "SenderName": "Seu App/Sistema",
  "SenderEmail": "seu_usuario@gmail.com",
  "Username": "seu_usuario@gmail.com", 
  "Password": "abcd efgh ijkl mnop" 
}
```

#### ⏱️ Ajustar Prazos de Expiração (`TimeOutSettings`)
Defina os tempos limites do sistema em segundos para a validação dos tokens gerados:
```json
"TimeOutSettings" : {
  "TokenResetSenha": 60,       // Tempo de vida do Token JWT gerado no login
  "TokenResetExpiracao": 75    // Janela de validade do link de redefinição de senha
}
```

### 5. Rodar a Aplicação
```bash
dotnet run
```

A API estará disponível no seguinte endereço local:
* **`http://localhost:5164/`**

## 🔐 Fluxo de Redefinição de Senha

O sistema implementa uma arquitetura segura de recuperação de credenciais dividida em duas rotas:

1. **`POST /login/esqueci-senha`**: O cliente envia o e-mail cadastrado. O sistema gera um Token criptográfico exclusivo (GUID) com prazo de expiração amarrado ao relógio do servidor, monta um e-mail com layout em HTML contendo um botão e dispara para o usuário através do serviço configurado.
2. **`POST /login/resetar-senha`**: O frontend lê os parâmetros contidos na URL do link clicado pelo usuário e envia o token de validação junto com a nova senha digitada. O backend valida os limites do `TimeOutSettings` e, se aprovado, aplica o Hash seguro via **BCrypt** na nova credencial antes de atualizar o banco de dados.

## 🔌 Documentação e Execução das APIs (Scalar)

O projeto utiliza o **Scalar** para gerar automaticamente uma interface de documentação moderna, bonita e interativa. Por ela, você pode visualizar todos os endpoints disponíveis e testar as requisições diretamente do navegador.

Para acessar a documentação com a aplicação rodando, abra o navegador e acesse:
* **`http://localhost:5164/scalar/v1`**

*(Nota: Você também pode testar os endpoints utilizando o arquivo `crud.http` integrado diretamente no VS Code se tiver a extensão REST Client instalada).*
