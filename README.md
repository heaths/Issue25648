# Sample ASP.NET Web API

This sample is an attempt to reproduce [Azure/azure-sdk-for-net#25648](https://github.com/Azure/azure-sdk-for-net/issues/25648).

## Getting started

To deploy the necessary resources,

1. Create a resource group:

   ```bash
   az group create -l westus2 -n issue25648
   ```

2. (Optional) If you'll be running the sample locally, make sure you're logged in as your user principal (or some other service principal).
   If using a user principal, you'll need your object ID:

   ```bash
   az ad user show --id user@domain.com --query objectId --output tsv
   ```

3. Deploy the Bicep template, making sure you have Bicep installed:

   ```bash
   az bicep install # or upgrade
   az deployment group create -g issue25648 -f azuredeploy.bicep -p principalId='<OID from above if needed>' principalType='<User | ServicePrincipal (default)>'
   ```

## Running locally

If running locally and you successfully deployed resources with the principalId specified as shown above, you need to add connection details.

In Visual Studio with the project loaded,

1. Right-click on the project and select **Managed User Secrets**.

2. Using the output values from the deployment above, define the following secret variables using appropriate output values:

   ```json
   {
     "APPCONFIG_URI": "https://hiat37inbdo5sconfig.azconfig.io",
     "KEYVAULT_URI": "https://kvhiat37inbdo5s.vault.azure.net/"
   }
   ```

3. Press **F5** to run the project under the debugger.

## Deploy

To deploy this to the web site you created above, in Visual Studio with the project loaded:

1. Right-click on the project and select **Publish...**.

2. Select **Azure** and click **Next**.

3. Select **Azure App Service (Linux)** and click **Next**.

4. Find the App Service instance created above, select it, and click **Next**.

5. For "API Management", check **Skip this step** and click **Next**.

6. Select "Publish (generates pubxml file)" and click **Finish**.

7. After the publishing profile is created, click the **Publish** button.

After a moment, the site is deployed and built, and after a few more minutes the web site should be available. Since the site is built for production, there may be no Swagger UI so you'll need to type the `/secrets` or `/secrets/SampleSecret` path in the URI to see anything.
