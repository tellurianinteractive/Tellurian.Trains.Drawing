# Azure infrastructure

Everything lives in the existing resource group **Tellurian.Trains** and is described by
`azuredeploy.json` (ARM template) with `azuredeploy.parameters.json`:

| Resource | Purpose |
|---|---|
| Static Web App `tellurian-trains-drawing` (Free) | Hosts the app. |
| Managed identity `id-trainsdrawing-deploy` | GitHub Actions logs in as this identity with **OpenID Connect**. There are no stored credentials. |
| Federated credentials `github-production`, `github-preview` | Trust the workflow jobs that run in the GitHub environments `production` and `preview` of `tellurianinteractive/Tellurian.Trains.Drawing`. |
| Role assignment: Contributor on the static web app only | Lets the identity read the deployment token at run time. |

The resource group is in North Europe, but Static Web Apps can't be created there, so the app's
location is **West Europe**. The location only decides where the app's metadata is kept; the site
is served from Azure's global edge network. The identity is in the resource group's location.

This follows the same pattern as the Module Registry (`id-moduleregistry-deploy`).
One difference: this repository is newer, and GitHub puts its **immutable owner and repository ids** in the
token subject (`repo:tellurianinteractive@57481455/Tellurian.Trains.Drawing@1456346388:environment:…`).
The template parameter `gitHubSubjectPrefix` holds that prefix; read it with
`gh api repos/tellurianinteractive/Tellurian.Trains.Drawing/actions/oidc/customization/sub`.

## Deploy or update the resources

```bash
az deployment group what-if --resource-group Tellurian.Trains --template-file infra/azuredeploy.json --parameters infra/azuredeploy.parameters.json
az deployment group create  --resource-group Tellurian.Trains --name tellurian-trains-drawing --template-file infra/azuredeploy.json --parameters infra/azuredeploy.parameters.json
```

What-if lists some server-managed properties of the static web app (e.g. `stableInboundIP`) as
"Delete". That is noise; they are not removed. The template sets `deploymentAuthPolicy` explicitly,
because a deployment would otherwise clear it.

## GitHub configuration

Both GitHub environments, `production` and `preview`, have three secrets. They are identifiers,
not credentials. The values come from the deployment outputs `deployClientId`, `tenantId` and
`subscriptionId`:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

The workflow logs in with `azure/login`, reads the static web app's deployment token with
`az staticwebapp secrets list`, masks it, and passes it to `Azure/static-web-apps-deploy`.
To invalidate the token, run `az staticwebapp secrets reset-api-key`; nothing in GitHub has to change.
