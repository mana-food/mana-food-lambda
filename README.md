# mana-food-lambda
## 🛰️ ManaFood Lambda & API Gateway

Este repositório contém a camada serverless e de gateway do sistema ManaFood.
O serviço provê autenticação de clientes via CPF sem cadastro de senha, utilizando AWS Lambda e orquestrar as requisições externas através de um API Gateway .NET 9.

## Estrutura de Pastas

```
mana-food-lambda/
├── Gateway
│   └── ManaFood.ApiGateway
├── LICENSE
├── README.md
└── src
    ├── GetClientByCpf
