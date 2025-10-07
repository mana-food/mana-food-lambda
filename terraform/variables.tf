variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Nome do projeto"
  type        = string
  default     = "mana-food"
}

variable "mysql_connection_string" {
  description = "String de conexão MySQL"
  type        = string
  sensitive   = true
}

variable "lambda_timeout" {
  description = "Timeout da Lambda em segundos"
  type        = number
  default     = 30
}

variable "lambda_memory" {
  description = "Memória da Lambda em MB"
  type        = number
  default     = 512
}