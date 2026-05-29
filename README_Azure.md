Azure deploy notes for Hgts Shuttle System

Overview
- This project is set up to be hosted on Azure Container Apps with Azure Database for PostgreSQL Flexible Server.
- The repository includes a helper script and a GitHub Actions workflow to build and deploy the API.

Quickstart
1. From a machine with `az` CLI logged into the `hgtsshuttlesystem` tenant and the target subscription set, run the infra script. By default the script uses the existing resource group `HG_Travelling_Services` (you can override with `RG_NAME`):

```bash
RG_NAME=HG_Travelling_Services LOCATION=southafricanorth ./scripts/create_azure_resources.sh
```

The script will create or configure an Azure Container Registry, Log Analytics workspace, Container Apps environment, PostgreSQL flexible server, and a Key Vault for secrets inside the specified resource group.

2. Create a service principal for GitHub Actions and add it to repository secrets. Locally run:

```bash
az ad sp create-for-rbac --name "hgts-github-deploy" --role Contributor --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/HgtsShuttleSystem
```

Save the JSON output and add it as the repository secret `AZURE_CREDENTIALS`.

3. Add the following repository secrets (Repository Settings → Secrets):
- `AZURE_CREDENTIALS` : JSON from `az ad sp create-for-rbac`.
- `AZURE_SUBSCRIPTION_ID` : your subscription id (Subscription 1).
- `AZURE_RESOURCE_GROUP` : HgtsShuttleSystem
- `ACR_LOGIN_SERVER` : from the script output (e.g. hgtsacrXXXX.azurecr.io)
- `ACR_USERNAME` : admin username (you can get via `az acr credential show`)
- `ACR_PASSWORD` : admin password
- `CONTAINERAPPS_ENV` : the Container Apps environment name (default hgts-ca-env)
- `CONTAINERAPP_NAME` : the container app name to create (default hgts-api)
- `POSTGRES_HOST`, `POSTGRES_DB`, `POSTGRES_USER` : database connection values

The deployment workflow prefers the Key Vault secret named `Postgres--AdminPassword` when it can read it from the resource group, and falls back to `POSTGRES_PASSWORD` only if that secret is unavailable. That keeps the runtime connection aligned with the live database password without baking the secret into the source tree.

4. Push to `main` to trigger the GitHub Actions workflow and deploy.

Notes & Next steps
- You must configure Postgres firewall rules or VNet integration so the Container App can access the database.
- Consider using Managed Identity + Key Vault references for secrets retrieval instead of plain secrets in GitHub.
