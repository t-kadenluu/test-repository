# Terraform configuration for Azure infrastructure
terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "enterprise-app-terraform"
    storage_account_name = "enterpriseappterraform"
    container_name       = "tfstate"
    key                  = "enterprise-app.terraform.tfstate"
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Variables
variable "environment" {
  description = "Environment name"
  type        = string
  default     = "production"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "East US"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-enterprise-app"
}

variable "aks_cluster_name" {
  description = "Name of the AKS cluster"
  type        = string
  default     = "aks-enterprise-app"
}

variable "sql_server_name" {
  description = "Name of the SQL Server"
  type        = string
  default     = "sql-enterprise-app"
}

variable "redis_cache_name" {
  description = "Name of the Redis Cache"
  type        = string
  default     = "redis-enterprise-app"
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
    ManagedBy   = "Terraform"
  }
}

# Virtual Network
resource "azurerm_virtual_network" "main" {
  name                = "vnet-enterprise-app"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
  }
}

# Subnet for AKS
resource "azurerm_subnet" "aks" {
  name                 = "subnet-aks"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Subnet for Database
resource "azurerm_subnet" "database" {
  name                 = "subnet-database"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.2.0/24"]
  
  service_endpoints = ["Microsoft.Sql"]
}

# AKS Cluster
resource "azurerm_kubernetes_cluster" "main" {
  name                = var.aks_cluster_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  dns_prefix          = "aks-enterprise-app"

  default_node_pool {
    name           = "default"
    node_count     = 3
    vm_size        = "Standard_D4s_v3"
    vnet_subnet_id = azurerm_subnet.aks.id
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin = "azure"
    network_policy = "calico"
  }

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
  }
}

# SQL Server
resource "azurerm_mssql_server" "main" {
  name                         = var.sql_server_name
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = "YourStrong@Passw0rd" # In production, use Azure Key Vault

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
  }
}

# SQL Databases
resource "azurerm_mssql_database" "user_management" {
  name           = "UserManagementDb"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 20
  read_scale     = false
  sku_name       = "S2"
  zone_redundant = false

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
    Service     = "UserManagement"
  }
}

resource "azurerm_mssql_database" "order_processing" {
  name           = "OrderProcessingDb"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 50
  read_scale     = false
  sku_name       = "S3"
  zone_redundant = false

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
    Service     = "OrderProcessing"
  }
}

# Redis Cache
resource "azurerm_redis_cache" "main" {
  name                = var.redis_cache_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  capacity            = 2
  family              = "C"
  sku_name            = "Standard"
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"

  redis_configuration {
    maxmemory_reserved = 125
    maxmemory_delta    = 125
    maxmemory_policy   = "volatile-lru"
  }

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
  }
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "ai-enterprise-app"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"

  tags = {
    Environment = var.environment
    Project     = "EnterpriseApp"
  }
}

# Outputs
output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "aks_cluster_name" {
  value = azurerm_kubernetes_cluster.main.name
}

output "sql_server_fqdn" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "redis_hostname" {
  value = azurerm_redis_cache.main.hostname
}

output "application_insights_instrumentation_key" {
  value     = azurerm_application_insights.main.instrumentation_key
  sensitive = true
}
