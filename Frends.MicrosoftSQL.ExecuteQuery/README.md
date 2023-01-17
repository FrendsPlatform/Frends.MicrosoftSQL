# Frends.MicrosoftSQL.ExecuteQuery
Frends MicrosoftSQL Task to execute MSSQL statement.

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT) 
[![Build](https://github.com/FrendsPlatform/Frends.MicrosoftSQL/actions/workflows/ExecuteQuery_build_and_test_on_main.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.MicrosoftSQL/actions)
![MyGet](https://img.shields.io/myget/frends-tasks/v/Frends.MicrosoftSQL.ExecuteQuery)
![Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.MicrosoftSQL/Frends.MicrosoftSQL.ExecuteQuery|main)

# Installing

You can install the Task via Frends UI Task View or you can find the NuGet package from the following NuGet feed https://www.myget.org/F/frends-tasks/api/v2.

## Building


Rebuild the project

`dotnet build`

Run tests

 Create a simple SQL server to docker:
 `docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04`
 
`dotnet test`


Create a NuGet package

`dotnet pack --configuration Release`