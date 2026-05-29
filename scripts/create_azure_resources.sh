#!/usr/bin/env bash
set -euo pipefail

# Usage: LOCATION=southafricanorth ./scripts/create_azure_resources.sh
# Requires: Azure CLI installed and logged in to the correct tenant/subscription

RG_NAME=${RG_NAME:-HG_Travelling_Services}
LOCATION=${LOCATION:-southafricanorth}
ACR_NAME=${ACR_NAME:-hgtsacr$((RANDOM%10000))}
WORKSPACE_NAME=${WORKSPACE_NAME:-hgts-law-$((RANDOM%10000))}
ENV_NAME=${ENV_NAME:-hgts-ca-env}
CONTAINERAPP_NAME=${CONTAINERAPP_NAME:-hgts-api}
POSTGRES_NAME=${POSTGRES_NAME:-hgts-pg-$((RANDOM%10000))}
POSTGRES_ADMIN=${POSTGRES_ADMIN:-pgadmin}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-$(openssl rand -base64 16)}
KEYVAULT_NAME=${KEYVAULT_NAME:-hgts-kv-$((RANDOM%10000))}

echo "Using resource group: $RG_NAME, location: $LOCATION"

echo "Creating resource group..."
az group create -n "$RG_NAME" -l "$LOCATION"

echo "Creating Azure Container Registry ($ACR_NAME)..."
az acr create -n "$ACR_NAME" -g "$RG_NAME" --sku Standard --admin-enabled true

ACR_LOGIN_SERVER=$(az acr show -n "$ACR_NAME" -g "$RG_NAME" --query loginServer -o tsv)
echo "ACR login server: $ACR_LOGIN_SERVER"

echo "Creating Log Analytics workspace..."
az monitor log-analytics workspace create -g "$RG_NAME" -n "$WORKSPACE_NAME" -l "$LOCATION"

WORKSPACE_ID=$(az monitor log-analytics workspace show -g "$RG_NAME" -n "$WORKSPACE_NAME" --query customerId -o tsv)
echo "Log Analytics workspace id: $WORKSPACE_ID"

echo "Creating Container Apps environment..."
az containerapp env create -g "$RG_NAME" -n "$ENV_NAME" --logs-workspace-id "$WORKSPACE_ID"

echo "Creating Azure Database for PostgreSQL Flexible Server..."
az postgres flexible-server create -g "$RG_NAME" -n "$POSTGRES_NAME" -l "$LOCATION" \
  --admin-user "$POSTGRES_ADMIN" --admin-password "$POSTGRES_PASSWORD" \
  --sku-name Standard_B1ms --tier Burstable --version 13 --storage-size 32

POSTGRES_FQDN=$(az postgres flexible-server show -g "$RG_NAME" -n "$POSTGRES_NAME" --query fullyQualifiedDomainName -o tsv)
echo "Postgres FQDN: $POSTGRES_FQDN"

echo "Creating Key Vault to store secrets..."
az keyvault create -g "$RG_NAME" -n "$KEYVAULT_NAME" -l "$LOCATION"
az keyvault secret set --vault-name "$KEYVAULT_NAME" -n "Postgres--AdminPassword" --value "$POSTGRES_PASSWORD"

echo "Summary"
echo "Resource Group: $RG_NAME"
echo "ACR: $ACR_NAME ($ACR_LOGIN_SERVER)"
echo "Container Apps environment: $ENV_NAME"
echo "Container App name (to create later): $CONTAINERAPP_NAME"
echo "Postgres server: $POSTGRES_NAME ($POSTGRES_FQDN)"
echo "Key Vault: $KEYVAULT_NAME"

echo "Next steps:
 - Create a container image and push to the ACR.
 - Create the Container App using 'az containerapp create' and set env vars for DB connection.
 - Configure firewall rules on the Postgres server (allow app outbound IPs or VNet integration).
 - Add service principal or GitHub Actions secrets (see README_Azure.md)."

echo "Done."
