# State Transition with EF Core Interceptors Example
This repository contains an example application that demonstrates how to handle state transitions in a domain-driven design using EF Core Interceptors. This repository is the companion to the Medium article: [Supercharge Your EFCore: The Entity Lifecycle Validation Technique You Can’t Afford to Miss](https://jczacharia.medium.com/supercharge-your-efcore-the-entity-lifecycle-validation-technique-you-cant-afford-to-miss-7d932ad75186).

## Project Overview
The application is a simple contest management system, which has different states a contest can be in, like 'Draft', 'Public', and 'Finalized'. Each state has certain rules about which states you can transition to. These rules are enforced using a state machine implemented with EF Core Interceptors.

## How to Use
Clone the repository:
```bash
git clone https://github.com/jzacharia/EntityLifecycleValidation.git
```
Navigate into the project directory:
```bash
cd EntityLifecycleValidation
```
Build and run the unit tests or the project (ensure you have .NET Core installed):
```bash
dotnet build
dotnet test
dotnet run
```
