
# Mana Food Lambda

Uma função AWS Lambda para autenticação de clientes via CPF, gerando tokens JWT para integração com a API Mana Food.

## 🏗️ Arquitetura

```
src/
├── ManaFood.AuthLambda/     # Função Lambda principal
│   ├── Function.cs          # Handler principal da Lambda
│   ├── Models/              # Modelos de request/response
│   ├── appsettings.json     # Configurações JWT
│   └── aws-lambda-tools-defaults.json
├── ManaFood.Data/           # Camada de dados e JWT
│   ├── ClientDao.cs         # Acesso a dados do cliente
│   └── JwtGenerator.cs      # Geração de tokens JWT
└── ManaFood.Domain/         # Entidades de domínio
    └── Client.cs            # Entidade Cliente
```

## 🚀 Funcionalidades

- ✅ **Autenticação por CPF** - Validação de clientes no banco MySQL
- ✅ **Geração de JWT** - Tokens compatíveis com a API Mana Food
- ✅ **Docker Support** - Execução local via container
- ✅ **Claims personalizados** - Suporte a roles (ADMIN, CUSTOMER, etc.)
- ✅ **AWS Lambda Ready** - Deploy direto na AWS

## 📋 Pré-requisitos

### Para Execução Local

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop/)
- **MySQL Server** - Local ou remoto
- **Visual Studio Code** (recomendado) com extensão C#

### Para Deploy na AWS

- **AWS CLI** - [Instalação](https://aws.amazon.com/cli/)
- **AWS SAM CLI** - [Instalação](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html)
- **Conta AWS** com permissões para Lambda
- **Amazon RDS MySQL** ou **Aurora MySQL**

```bash
# Instalar AWS CLI
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# Instalar SAM CLI
pip install aws-sam-cli

# Configurar credenciais AWS
aws configure
```

## ⚙️ Configuração

### 1. Configurações JWT (appsettings.json)

```json
{
  "Jwt": {
    "SecretKey": "rF8wzYp2L#eX1v9sKd3@qMTuN6JBgCmySecretKey123456",
    "ExpirationMinutes": 60,
    "Issuer": "ManaFoodIssuer",
    "Audience": "ManaFoodAudience"
  }
}
```

### 2. Variáveis de Ambiente

#### Local (Docker)
```bash
MYSQL_CONNECTION_STRING="server=####;port=3306;database=db_manafood;user=root;password=####;"
```

#### AWS Lambda
```bash
MYSQL_CONNECTION_STRING="server=mana-food-db.cluster-xxx.us-east-1.rds.amazonaws.com;port=3306;database=db_manafood;user=admin;password=SecurePassword123;"
```

## 🐳 Execução Local com Docker

### 1. Build da imagem

```bash
docker build -t mana-food-lambda .
```

### 2. Executar container

```bash
docker run -p 9000:8080 \
  -e MYSQL_CONNECTION_STRING="server=####;port=3306;database=db_manafood;user=root;password=####;" \
  --name mana-food-lambda \
  mana-food-lambda
```

### 3. Testar localmente

```bash
curl -X POST http://localhost:9000/2015-03-31/functions/function/invocations \
  -H "Content-Type: application/json" \
  -d '{"cpf": "###########"}'
```

## ☁️ Deploy na AWS
