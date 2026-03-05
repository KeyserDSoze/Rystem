using System.CommandLine;
using RepositoryFramework.Tools.TypescriptGenerator.Cli;

// Build the root command
var rootCommand = new RootCommand("Rystem TypeScript Generator - Generate TypeScript types and services from C# Repository/CQRS models");

// Add the generate command
rootCommand.Subcommands.Add(GenerateCommand.Create());

// Execute
return rootCommand.Parse(args).Invoke();

