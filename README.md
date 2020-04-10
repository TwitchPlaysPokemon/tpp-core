
## requirements
- dotnet core SDK 3.1+, you can get one from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

## run tests
- `dotnet test` runs all tests
- `dotnet test --filter TestCategory!=IntegrationTest` skips tests that have preconditions or are slow
