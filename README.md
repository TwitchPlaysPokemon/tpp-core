
## requirements
- dotnet core SDK 5.0+, you can get one from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- If you use or run any components with persistence, MongoDB.
  See [Persistence.MongoDB](Persistence.MongoDB) for details.

## run
The executable project is located at [Core](Core). See its readme for further instructions.

## run tests
- `dotnet test` runs all tests
- `dotnet test --filter TestCategory!=IntegrationTest` skips tests that have preconditions or are slow
