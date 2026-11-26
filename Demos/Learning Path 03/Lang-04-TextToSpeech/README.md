# Lang-04-TextToSpeech (agent + Speech MCP setup)

Learn module 5: *Develop a speech agent with the Azure Speech MCP server*. The agent part of `Lang-03-SpeechToText` and
`Lang-04-TextToSpeech` uses the agent and the blob storage described below.

The program sends prompts to a Foundry agent that has the **Azure Speech MCP Server** tool attached.
The tool needs a blob container: synthesized audio is written there, and transcription can read from it.

## 1. Create the blob storage (Azure CLI, PowerShell)

Uses resource group `AI-103` and region `swedencentral`. The storage account name must be globally unique
(lowercase letters and digits, 3-24 characters).

```powershell
$rg        = "AI-103"
$location  = "swedencentral"
$account   = "psspeechmcp$(Get-Random -Maximum 9999)"   # globally unique
$container = "files"

# 1. Storage account (Standard LRS, no public blob access)
az storage account create `
  --name $account --resource-group $rg --location $location `
  --sku Standard_LRS --kind StorageV2 `
  --allow-blob-public-access false --min-tls-version TLS1_2

# 2. Give your own user data access to the blobs (required by the course)
$me    = az ad signed-in-user show --query id -o tsv
$scope = az storage account show --name $account --resource-group $rg --query id -o tsv
az role assignment create --assignee $me --role "Storage Blob Data Contributor" --scope $scope

# 3. Container (a new role assignment can take a minute or two; retry if this fails)
az storage container create --account-name $account --name $container --auth-mode login

# 4. SAS URL for the container: read, add, create, write, list; valid for 1 day
$expiry = (Get-Date).ToUniversalTime().AddDays(1).ToString("yyyy-MM-ddTHH:mmZ")
$sas = az storage container generate-sas `
  --account-name $account --name $container `
  --permissions racwl --expiry $expiry --https-only `
  --auth-mode login --as-user -o tsv

"https://$account.blob.core.windows.net/${container}?$sas"
```

The last line prints the SAS URL. 

Notes:
- Treat the SAS URL as a secret: anyone with it can read and write the container until it expires.
  A user-delegation SAS can be valid for at most 7 days.
- To remove everything afterwards: `az storage account delete --name $account --resource-group $rg`

## 2. Add the Azure Speech MCP Server to an agent (Foundry portal)

1. Foundry portal > your project > **Tools** > **Add a new tool** > **Catalog** > **Azure Speech MCP Server**.
2. Fill in:
   - **Foundry resource name**: `ps-mijneerste-resource` (the resource, *not* the agent name)
   - **Key** (`Ocp-Apim-Subscription-Key`): the key of the resource (Keys and endpoint)
   - **X-Blob-Container-Url**: the SAS URL printed in step 1
3. Create an agent that uses the tool, e.g. name `Speech-Agent`, instruction
   *"You are an AI agent that uses the Azure AI Speech tool to transcribe and generate speech."*

## 3. Run

In `Program.cs` set `AGENT_NAME` to the name of your agent, sign in with `az login`, then:

```
dotnet run
```

The signed-in account needs the **Azure AI User** role on the Foundry resource.
The agent's tool call may ask for approval; the program approves it automatically.
