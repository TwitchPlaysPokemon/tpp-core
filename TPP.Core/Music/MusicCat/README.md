The code in this directory is auto-generated from [MusicCat](https://github.com/TwitchPlaysPokemon/musiccat-cs)'s OpenAPI.
Re-generate it by following these steps:
- Place the OpenAPI-spec to generate the code from in this directory, e.g. with `curl` (also works in Powershell):
  ```shell
  curl -o api-spec.json https://raw.githubusercontent.com/TwitchPlaysPokemon/musiccat-cs/refs/heads/master/api-spec.json
  ```
- Install [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview):
  ```shell
  dotnet tool install -g Microsoft.OpenApi.Kiota
  ```
- Generate the API client code:
  ```shell
  kiota generate `
    --openapi .\api-spec.json `
    --language csharp `
    --exclude-backward-compatible `
    --namespace-name MusicCat `
    --output Client
  ```
- If the files were generated on Windows, let's fix the newlines:
  ```shell
  find Client/ -type f -exec dos2unix {} \;
  ```
- Unfortunately, [Kiota erroneously makes results nullable](https://github.com/microsoft/kiota/issues/3911).
  Let's fix some nullabilities manually:
  ```shell
  sed -i 's/<int?>/<int>/g' Client/MusicLibrary/Count/CountRequestBuilder.cs
  sed -i 's/<float?>/<float>/g' Client/Player/Position/PositionRequestBuilder.cs
  sed -i 's/<float?>/<float>/g' Client/Player/Volume/VolumeRequestBuilder.cs
  ```
