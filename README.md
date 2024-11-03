# Vsix-QuickClassMap

This Visual Studio extension shows relationships between classes in your C# project, as well as in individual folders or files. It also supports the selection of multiple folders or files.

The output is a DGML diagram. (You need to have the DGML component installed in Visual Studio.)

The extension uses heuristics to distinguish between five types of relationships: 
- Inheritance
- Implementation
- Composition
- Aggregation
- Uses

![Screenshot](docs/screenshot1.png)
