# 🚀 AspNetCore Minimal API - CRUD

Uma API Minimalista desenvolvida em ASP.NET Core para operações de CRUD (Create, Read, Update, Delete), utilizando SQLite como banco de dados local.

## 🛠️ Tecnologias Utilizadas

* **.NET 10.0** (SDK 10.0.300 / Runtime 10.0.8)
* **ASP.NET Core Minimal APIs**
* **Entity Framework Core**
* **SQLite** (Banco de dados local em arquivo)
* **Scalar** (Documentação interativa e execução das APIs)

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

```bash
# 1. Cria o histórico inicial da estrutura do banco
dotnet ef migrations add InitialCreate

# 2. Executa a migração, gerando o arquivo do banco e construindo as tabelas
dotnet ef database update
```

### 4. Rodar a Aplicação
```bash
dotnet run
```

A API estará disponível no seguinte endereço local:
* **`http://localhost:5164/`**

## 🔌 Documentação e Execução das APIs (Scalar)

O projeto utiliza o **Scalar** para gerar automaticamente uma interface de documentação moderna, bonita e interativa. Por ela, você pode visualizar todos os endpoints disponíveis e testar as requisições diretamente do navegador.

Para acessar a documentação com a aplicação rodando, abra o navegador e acesse:
* **`http://localhost:5164/scalar/v1`**

*(Nota: Você também pode testar os endpoints utilizando o arquivo `crud.http` integrado diretamente no VS Code se tiver a extensão REST Client instalada).*
