# GitHub Copilot Instructions for Vsix-QuickClassMap

These guidelines tell GitHub Copilot how to propose code for this repository.

## General .NET and C# guidelines

- Follow standard C# coding style and the conventions already used in this repo.
- Prefer readability over cleverness; optimize for maintainable, self-documenting code.
- Keep methods short and focused on a single responsibility.
- Apply SOLID principles when designing new types and refactoring existing code.

## Repo-specific code conventions

- **Naming and casing**
  - Use PascalCase for classes, interfaces, enums, and public members (e.g., `GenerateClassMapCommand`, `ClassInfo`, `CreateDgmlDocumentWithContent`).
  - Use camelCase for local variables and method parameters.
  - Use a leading underscore for private readonly fields (e.g., `_serviceProvider`).
  - Use meaningful, descriptive names that reflect intent; avoid abbreviations unless they are standard (e.g., `dte`).

- **Namespaces and organization**
  - Place types under the `QuickClassMap` root namespace and its logical sub-namespaces (`Domain`, `VS`, `Helpers`, `Generators`, `Roslyn`, etc.).
  - Keep one primary class per file; name the file to match the class (e.g., `GenerateClassMapCommand.cs`, `DocumentCreationService.cs`).

- **Using directives**
  - Place `using` directives at the top of the file, outside the namespace, grouped by framework / third-party / project namespaces and separated by a blank line when appropriate.
  - Use fully qualified namespaces only when required to avoid ambiguity; otherwise prefer `using` statements.

- **Braces, spacing, and layout**
  - Use K&R-style braces on a new line for types, methods, and control blocks, matching the existing codebase.
  - Use four spaces for indentation; do not use tabs.
  - Place a single space after keywords like `if`, `for`, `while`, and around binary operators.
  - Use blank lines to separate logical blocks of code for readability, but avoid excessive vertical whitespace.

- **Access modifiers and encapsulation**
  - Always specify access modifiers explicitly (`public`, `internal`, `protected`, `private`).
  - Prefer the least permissive access level necessary.
  - Prefer read-only fields where possible (e.g., `private readonly IServiceProvider _serviceProvider`).

- **Exceptions and messaging**
  - Throw specific exception types with clear, user-friendly messages (`ArgumentNullException`, `InvalidOperationException`, etc.).
  - For user-facing messages in the VS extension, use the existing patterns (e.g., `InfoException` and `VsShellUtilities.ShowMessageBox`).

## Error handling and logging

- Never use empty `catch` blocks.
- Catch the most specific exception type possible; avoid catching `Exception` unless necessary.
- Always log exceptions or rethrow with additional context; do not silently swallow them.
- When rethrowing, use `throw;` to preserve the stack trace.
- Do not leak sensitive data (connection strings, access tokens, personal data, etc.) into logs, exception messages, or UI.
- Use guard clauses to fail fast on invalid input (e.g., `ArgumentNullException`, `ArgumentException`).

## Async and I/O

- Prefer async/await for all I/O-bound operations (file, network, database, VS services, etc.).
- Use asynchronous APIs when available and propagate `async` all the way up when it makes sense.
- Avoid `async void` except for event handlers.
- Use cancellation tokens where APIs support them and pass them through the call chain when practical.

## Dependency management and design

- Prefer dependency injection over static classes or singletons for services and collaborators.
- Design new services and components behind interfaces where it improves testability.
- Avoid hidden dependencies and global state.
- Keep public APIs small and cohesive.

## Input validation and null safety

- Validate all user input and external data before use.
- Use argument validation (`ArgumentNullException.ThrowIfNull`, `ArgumentException`, etc.) at public entry points.
- Guard against null references; use nullable reference types annotations if/when enabled in the project.
- Prefer early returns with guard clauses to keep code simple and reduce nesting.

## Data access

- When database access is required, use the project’s ORM (e.g., Entity Framework) or existing data access abstractions.
- Avoid raw SQL unless there is a clear, documented need.
- If raw SQL is required, use parameterized queries to prevent SQL injection and keep SQL in a dedicated data access layer.

## Testing

- For all new functionality, add unit tests (or appropriate automated tests) covering happy paths and key edge cases.
- Update or extend existing tests when changing behavior.
- Keep tests deterministic and isolated; avoid unnecessary external dependencies.
- Prefer testing behavior and public contracts over internal implementation details.

## Documentation and comments

- Add XML documentation comments for all public classes, interfaces, methods, and properties.
- Use summaries that explain the purpose and behavior, including important side effects, exceptions thrown, and thread-safety assumptions.
- Add inline comments sparingly to explain *why* something is done, not *what* the code does.

## Visual Studio extension specifics (this repo)

- When working with VS extensibility APIs, follow asynchronous best practices to keep the UI responsive.
- Use the existing helper and service abstractions under the `VS` and `Helpers` namespaces when extending functionality.
- Be mindful of threading and use the appropriate VS services for switching to the UI thread when necessary.

## Security and privacy

- Do not hard-code secrets, API keys, or credentials.
- Avoid logging or exposing personally identifiable information (PII) or other sensitive data.
- Use secure defaults; prefer secure protocols and APIs.

## Performance and maintainability

- Avoid premature optimization; measure before optimizing.
- Use efficient data structures and algorithms appropriate for the expected scale.
- Keep codebase consistent with existing patterns and abstractions in this project.

## When in doubt

- Prefer patterns and conventions already used in this repository.
- Favor clarity, testability, and adherence to .NET best practices over clever solutions.
