# Deployment guide

> **_NOTE:_** For this implementation, we are providing a mechanism that you can use to deploy to your own subscription. These steps are not technically part of the reference implementation, but does represent the type of work that needs to be done. You would typically encapsulate this work in your continuous delivery pipeline (Azure DevOps, Jenkins, etc.) in a way that aligns with your operational practices.

## Environment

The instructions were written assuming the usage of bash in Azure Cloud Shell, in which no additional installations are necessary.

If you use bash from another location (WSL, Code Spaces, a Linux workstation, etc.), then please ensure you have the following installed in that environment. _Azure Cloud Shell already has these installed._

   - Azure CLI
   - slqcmd

You do not need to clone this repo to your shell.

## Deploy the Azure resources from Azure Cloud Shell

The provided `rundeployment.sh` will create all dependencies necessary for the web application and will deploy the web application infrastructure as well. Start by setting parameters for your deployment.

```bash
mkdir deployweb
cd deployweb

export DEPLOYMENT_WA=https://raw.githubusercontent.com/mspnp/reference-architectures/master/web-app/deployment/
SUFFIX=$((${RANDOM}*${RANDOM}))
export RGNAME_WA=rg-webapp-infra
export RGLOCATION_WA=eastus2
export SQLSERVERNAME_WA=webapp-sql-${SUFFIX}
export SQLSERVERDB_WA=web-db
export SQLADMINUSER_WA=webadmin
export DNSNAME_WA=webapp-${SUFFIX}
```

Ensure you're logged into the correct subscription in your shell.  You can use `az account show` to validate and use `az account set` to change your subscription if necessary.

SQL Database accounts (including the admin) have a minimum password size of eight characters ([amongst other requirements](https://docs.microsoft.com/sql/relational-databases/security/password-policy?view=azuresqldb-current)). Capture a suitable password into `SQLADMINPASSWORD_WA` using the command sequence below.

```bash
read -s SQLADMINPASSWORD_WA
export SQLADMINPASSWORD_WA
```

Download two files from this repo that you'll need to execute from your Azure Cloud Shell.

```bash
for f in saveenv.sh rundeployment.sh; do wget ${DEPLOYMENT_WA}${f} && chmod +x ./${f}; done
```

Execute `saveenv.sh` to save environment variables created above to a backup file called `webapp.env`.

```bash
./saveenv.sh
```

> If your terminal session gets reset for any reason past this point, you can source the file to reload the environment variables `source webapp.env`.

Execute `rundeployment.sh` to deploy all Azure resources. This will take about 25 minutes.

```bash
./rundeployment.sh
```

At this point you have all of the Azure resources in place: SQL Database, Cosmos DB, App Service, Application Insights, Azure Cache for Redis, Azure Front Door, Azure Service Bus, and Azure Storage. There are no records in Cosmos DB nor is the web application code itself yet deployed.

## Populate Cosmos DB starter content (optional)

The Cosmos DB server you deployed has a container named `cacheContainer` that is designed to hold advertisements for the website's footer. While they are not required for the reference implementation to function here is an example of content you could include. We provide a file called **Microsoft_Azure_logo_small.png** in this repo. You can reference that file in a fake ad.

Using the [Azure portal](https://portal.azure.com/#blade/HubsExtension/BrowseResource/resourceType/Microsoft.DocumentDb%2FdatabaseAccounts) or Azure Storage Explorer add this document to the `cacheContainer` container in the Cosmos DB Server created above.

```json
{"id": "1", "Message": "Powered by Azure", "MessageType": "AD", "Url": "https://raw.githubusercontent.com/mspnp/reference-architectures/master/web-app/deployment/Microsoft_Azure_logo_small.png"}
```

To do this from the Azure portal, in the resource group of deployment, click on **Azure Cosmos Db Account** then click **Data Explorer** and select **cacheDB** and then **cacheContainer**.  Click on **Items** and then **New Item**. Replace the whole json payload with above content and click **Save**.

## Publish the web apps

We'll publish the two web applications and Azure Function directly from Visual Studio. As with the resources above, this would normally be performed via your continuous delivery pipeline. To do this, you'll need to clone this repo to your personal workstation.

1. Clone the repo to your personal workstation, not Azure Cloud Shell, via `git clone https://github.com/mspnp/reference-architectures`
1. Open **Voting.sln** solution in Visual Studio.
   1. Deploy the **Voting API** web app.
      1. Right click on the **VotingData** project.
      1. Click on **Publish** and click **Add a publish profile**.
         1. Select _Azure_ and click **Next**
         1. The select _Azure App Service (Windows)_, and click **Next**.
         1. Sign in (if necessary)
         1. Select the Subscription and Resource group for the deployment (as set in `RGNAME_WA`).
         1. Select the app service deployment that starts with _web-votingapi_.
         1. Ensure _Deploy as ZIP package_ is selected and then click **Finish**.
      1. Click the _Publish_ button to perform the deployment.
   1. Deploy the **Voting website** web app.
      1. Right click on the **VotingWeb** project.
      1. Click on **Publish** and click **Add a publish profile**.
         1. Select _Azure_ and click **Next**.
         1. The select _Azure App Service (Windows)_, and click **Next**.
         1. Sign in (if necessary)
         1. Select the Subscription and Resource group for the deployment (as set in `RGNAME_WA`).
         1. Select the app service deployment that starts with _webapp_.
         1. Ensure _Deploy as ZIP package_ is selected and then click **Finish**.
      1. Click the **Publish** button to perform the deployment.
1. Open the **FunctionVoteCounter.sln** solution.
   1. Deploy the **Vote Counter** Function App.
      1. Right click on **VoteCounter** project and click on **Publish**.
      1. Select _Azure_ and click **Next**.
      1. Select _Azure Function App (Windows)_ and click **Next**.
      1. Sign in (if necessary)
      1. Select the Subscription and Resource group for the deployment (as set in `RGNAME_WA`).
      1. Select the app service deployment that starts with _func-votecounter_.
      1. Ensure _Run from package file_ is selected and then click **Finish**.
      1. Click the **Publish** button to perform the deployment.

## Solution components

Your website is fully deployed now. You can open the url <https://${DNSNAME\_WA}.azurewebsites.net> to view the _What's for Lunch?_ voting site.

### Data flow

As the web page loads, it asks for current vote data from the API. And as suggestions are added, it also invokes the API to persist the suggestion in SQL Database. See `Get` and `Add` in `VotesController.cs` in the **VotingWeb** project.

As suggestions are voted for in the web front end, it adds a message to the Service Bus Queue. See `Vote` in `VotesController.cs` in the **VotingWeb** project.

As votes are added to the Service Bus Queue (`sbq-voting`), the Azure Function is invoked asynchronously to update the vote count in SQL Database. See `VoteCounter.cs` in the **VoteCounter** project.

### Application Insights

All three web applications use Application Insights for logs and telemetry. That data can be view from the [Azure portal](https://ms.portal.azure.com/#blade/HubsExtension/BrowseResource/resourceType/microsoft.insights%2Fcomponents). Application Insights will detect SQL Database and Azure Cache for Redis calls as dependencies.

### Azure Cache for Redis and CosmosDB

At the bottom of the voting web page, is a simulated advertisement. If you followed the optional instructions above, a "Microsoft Azure" image is shown on the page. This image is attempted to be retrieved from Azure Cache for Redis, and if not available then will look to CosmosDB. Once loaded from CosmosDB, it is then cached using the [cache-aside pattern](https://docs.microsoft.com/azure/architecture/patterns/cache-aside) in Azure Cache for Redis for 10 minutes for future page loads. See `GetAdsAsync` in `AdRepository.cs` in the **VotingWeb** project.

## Clean Up Resources

All resources were created in the resource group you identified in `RGNAME_WA`. To delete everything deployed with these steps you can execute the following _destructive_ command. This will take about five minutes.

```bash
az group delete -n $RGNAME
```
