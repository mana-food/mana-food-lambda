terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# Build do .NET
resource "null_resource" "build" {
  provisioner "local-exec" {
    command = <<-EOT
      cd ../src/ManaFood.AuthLambda
      dotnet publish -c Release -r linux-x64 --self-contained false -o publish
      cd publish
      powershell Compress-Archive -Path * -DestinationPath ../../terraform/lambda.zip -Force
    EOT
  }
}

# IAM Role
resource "aws_iam_role" "lambda_role" {
  name = "${var.project_name}-lambda-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = { Service = "lambda.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_basic" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Lambda Function
resource "aws_lambda_function" "auth_function" {
  filename      = "lambda.zip"
  function_name = "${var.project_name}-auth"
  role         = aws_iam_role.lambda_role.arn
  handler      = "ManaFood.AuthLambda::ManaFood.AuthLambda.Function::FunctionHandler"
  runtime      = "dotnet8"
  timeout      = var.lambda_timeout
  memory_size  = var.lambda_memory

  environment {
    variables = {
      MYSQL_CONNECTION_STRING = var.mysql_connection_string
    }
  }

  depends_on = [null_resource.build]
}