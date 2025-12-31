### Run
#### VS Code
    \src\MfePortal.AppHost> dotnet run

#### Azure Resources
1. Container Registry
2. Container Apps Environment
3. Logs Analystics
4. User Managed Identity, and more

[Azure Developer CLI Sample browser](https://azure.github.io/awesome-azd/)

[Aspire Samples](https://learn.microsoft.com/en-us/samples/browse/?expanded=dotnet&terms=aspire)

### Environment variables
https://medium.com/@ravipatel.it/managing-configuration-and-environment-variables-in-net-b1c10d69d3d2
https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/manage-environment-variables#environment-specific-env-file

## Azure Developer CLI

azd up
* azd provision
* azd deploy
	dotnet publish
	
Notes:

* azd up keeps the infra provisioning code in-memory
* use ```azd infra synth``` to dump it to file
* azd infra synth - creates infra folder with bicep files
	
* azd env list
* azd env set
* ```azd env new local```: creates new envionment

azd up

azd init


[Hello Azd](https://github.com/Azure-Samples/hello-azd)


### Aspire with Dapr
* Dapr workflow does not work with Worker asp.net temple as it requires an Http endpoint. Use Web api template to write Dapr workflow.

Aspire with Dapr
https://github.com/SiddyHub/DaprWithAspire

### Dapr

## Workflow
* Go to Dapr side car (*-dapr-cli) in Aspire and click View details in overflow button
* Select Http Endpoint port number (i.e 49677)
### Start workflow
    Start Workflow: POST http://localhost:49677/v1.0-beta1/workflows/dapr/AsyncWorkflow/start

     

### Check workflow status
    Check Workflow Status: http://localhost:49677/v1.0-beta1/workflows/dapr/<instance-id>

### Azure Container Apps
https://azure.github.io/aca-dotnet-workshop/aca/00-workshop-intro/

### Routing
* Prefer Attribute-based routing over conventional routing
* In Attribute based routing, prefer [HttpGet] pattern over [Route]
* ```Route["api/[controller"]]``` is called token replacement.
* Route templates applied to an action that begin with ```/``` or ```~/``` don't get combined with the route templates applied to the controller.
* ```[HttpGet]``` GET /api/order
* ```[HttpGet("{id}")]``` GET /api/order/abc
* ```[HttpGet("status/{id}")]``` GET /api/order/status/abc

## Lean Architecture

Clean Architecture has this Application Layer.
- Infastructure --> Application (UseCases) --> Core(Domain)
- Infrastructure --> Core(Domain)

How abt if we remove Application layer and put everything in Core.?

UI --> Api --> Infrastructure --> Core --> Shared

Shared: Any utilities project that might be used to share across multiple repositoris

###

### References
[Dapr with Aspire](https://github.com/SiddyHub/DaprWithAspire)
[Dotnet Template](https://github.com/varianter/dotnet-template/tree/main)

### Errors:
* The gRPC server for Durable Task gRPC worker is unavailable. Will continue retrying. - Indicates Dapr side car is not running
* ERROR: environment already initialized to local - Remov .azure folder and run azd init again



### Deploy a cloud-native application automatically

The code in this repository supports a Learn module focused on automating CI/CD with .NET. The module shows you how to:

- Authenticate GitHub Actions to a container registry.
- Securely store sensitive information that GitHub Actions uses.
- Implement an action to build the container image for a microservice.
- Modify and commit the microservice code to trigger a build.
- Implement an action to deploy the updated container to an Azure Kubernetes Service (AKS) cluster.
- Modify and commit a Helm chart to trigger the deployment.
- Revert the microservice to the previous deployment.

Take the moudle on [Microsoft Learn Training](https://learn.microsoft.com/training/modules/microservices-devops-aspnet-core/)

### Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### Legal Notices

Microsoft and any contributors grant you a license to the Microsoft documentation and other content
in this repository under the [Creative Commons Attribution 4.0 International Public License](https://creativecommons.org/licenses/by/4.0/legalcode),
see the [LICENSE](LICENSE) file, and grant you a license to any code in the repository under the [MIT License](https://opensource.org/licenses/MIT), see the
[LICENSE-CODE](LICENSE-CODE) file.

Microsoft, Windows, Microsoft Azure and/or other Microsoft products and services referenced in the documentation
may be either trademarks or registered trademarks of Microsoft in the United States and/or other countries.
The licenses for this project do not grant you rights to use any Microsoft names, logos, or trademarks.
Microsoft's general trademark guidelines can be found at http://go.microsoft.com/fwlink/?LinkID=254653.

Privacy information can be found at https://privacy.microsoft.com/en-us/

Microsoft and any contributors reserve all other rights, whether under their respective copyrights, patents,
or trademarks, whether by implication, estoppel or otherwise.