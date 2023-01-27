# Frends.MicrosoftSQL.ExecuteQuery
Frends Task to execute Microsoft SQL Server query.

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT) 
[![Build](https://github.com/FrendsPlatform/Frends.MicrosoftSQL/actions/workflows/ExecuteQuery_build_and_test_on_main.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.MicrosoftSQL/actions)
![Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.MicrosoftSQL/Frends.MicrosoftSQL.ExecuteQuery|main)

# Installing

You can install the Task via Frends UI Task View.

## Building


Rebuild the project

`dotnet build`

Run tests

 Create a simple SQL server to docker:
 `docker-compose up`
 
`dotnet test`


Create a NuGet package

`dotnet pack --configuration Release`