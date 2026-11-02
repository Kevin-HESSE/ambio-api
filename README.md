# Ambio API

## Prerequisites

### .NET SDK

Be sure to have the LTS version of .NET SDK installed. You check the official [Microsoft documentation](https://dotnet.microsoft.com/en-us/download/dotnet) for more information. 

Or you can use the `dotnetup` tools. More information [here](https://learn.microsoft.com/dotnet/core/tools/dotnetup/)

### Docker

Be sure to have Docker installed. You can check the official [Docker documentation](https://docs.docker.com/get-docker/) for more information.

## Installation

First, restore all tools need for the application :

```shell
dotnet tool restore
```

Start the container, it will start the database and wait until it is ready :

```shell
docker compose up -d --wait database
```

Then execute the migrations :

```shell
dotnet ef database update --project src/Ambio.Api
```

Finally, start the API with :

```shell
dotnet run --project src/Ambio.Api
```
