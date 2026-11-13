# Azure infrastructure

The app is hosted as an **Azure Static Web App** in the existing resource group **Tellurian.Trains**.
`azuredeploy.json` (ARM template) and `azuredeploy.parameters.json` describe it.

The resource group is in North Europe, but Static Web Apps can't be created there, so the app's
location is **West Europe**. The location only decides where the app's metadata is kept; the site
is served from Azure's global edge network.

## Deploy or update the resource

```bash
az deployment group what-if --resource-group Tellurian.Trains --template-file infra/azuredeploy.json --parameters infra/azuredeploy.parameters.json
az deployment group create  --resource-group Tellurian.Trains --template-file infra/azuredeploy.json --parameters infra/azuredeploy.parameters.json
```

## Connect GitHub Actions

The workflow deploys with the app's deployment token, stored as the repository secret
`AZURE_STATIC_WEB_APPS_API_TOKEN`. Set it (or rotate it) without printing it:

```bash
az staticwebapp secrets list --name tellurian-trains-drawing --resource-group Tellurian.Trains --query properties.apiKey -o tsv \
  | gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --repo tellurianinteractive/Tellurian.Trains.Drawing
```

Until the secret exists, the deploy steps are skipped with a warning.
