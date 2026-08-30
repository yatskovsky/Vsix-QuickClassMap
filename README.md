# Vsix-QuickClassMap

This Visual Studio extension shows relationships between classes in your C# project, as well as in
individual folders or files. It also supports the selection of multiple folders or files.

Currently, the output is a DGML diagram.

![Screenshot](docs/screenshot1.png)

## Requirements

- Visual Studio 2022, 2026
- DGML component installed (Visual Studio Installer → Individual components → Code tools → DGML editor)

<img src="https://github.com/user-attachments/assets/e9efec26-0e66-4ad0-966a-851323efbe4e" alt="dgml" width="250"/>

## Relationship types

The extension uses heuristics to distinguish between five types of relationships:

- Inheritance
- Implementation
- Composition
- Aggregation
- Uses
