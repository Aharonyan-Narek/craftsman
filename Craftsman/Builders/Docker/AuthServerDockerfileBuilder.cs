namespace Craftsman.Builders.Docker;

using System.IO.Abstractions;
using Helpers;

public static class AuthServerDockerfileBuilder
{
    public static void CreateAuthServerDotNetDockerfile(string srcDirectory, string projectBaseName, IFileSystem fileSystem)
    {
        var classPath = ClassPathHelper.WebApiProjectRootClassPath(srcDirectory, $"Dockerfile", projectBaseName);
        var fileText = GetAuthServerDockerfileText(projectBaseName, true, false);
        Utilities.CreateFile(classPath, fileText, fileSystem);
    }

    private static string GetAuthServerDockerfileText(string projectBaseName, bool addNodeInstall, bool addSharedKernel)
    {
        var sharedKernelText = addSharedKernel 
            ? $@"
COPY [""SharedKernel/SharedKernel.csproj"", ""./SharedKernel/""]"
            : "";
        var nodeText = addNodeInstall 
            ? $@"

RUN curl -sL https://deb.nodesource.com/setup_16.x | bash -
RUN apt install -y nodejs"
            : "";
        return @$"FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app{nodeText}

# Copy csproj and restore as distinct layers
COPY [""{projectBaseName}/{projectBaseName}.csproj"", ""./{projectBaseName}/""]{sharedKernelText}
RUN dotnet restore ""./{projectBaseName}/{projectBaseName}.csproj""

# Copy everything else and build
COPY . ./
RUN dotnet build ""{projectBaseName}/{projectBaseName}.csproj"" -c Release -o /app/build

FROM build-env AS publish
RUN dotnet publish ""{projectBaseName}/{projectBaseName}.csproj"" -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=publish /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT [""dotnet"", ""/app/{projectBaseName}.dll""]
";
    }
}