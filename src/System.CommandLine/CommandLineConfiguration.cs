﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.CommandLine.Invocation;

namespace System.CommandLine
{
    /// <summary>
    /// Represents the configuration used by the <see cref="CommandLineParser"/>.
    /// </summary>
    public class CommandLineConfiguration
    {
        private TextWriter? _output, _error;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineConfiguration"/> class.
        /// </summary>
        /// <param name="rootCommand">The root command for the parser.</param>
        public CommandLineConfiguration(Command rootCommand)
        {
            RootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
        }

        internal bool HasDirectives =>
            RootCommand switch
            {
                RootCommand root => root.Directives.Count > 0,
                _ => false
            };

        /// <summary>
        /// Enables the parser to recognize and expand POSIX-style bundled options.
        /// </summary>
        /// <param name="value"><see langword="true"/> to parse POSIX bundles; otherwise, <see langword="false"/>.</param>
        /// <remarks>
        /// POSIX conventions recommend that single-character options be allowed to be specified together after a single <c>-</c> prefix. When <see cref="EnablePosixBundling"/> is set to <see langword="true"/>, the following command lines are equivalent:
        /// 
        /// <code>
        ///     &gt; myapp -a -b -c
        ///     &gt; myapp -abc
        /// </code>
        /// 
        /// If an argument is provided after an option bundle, it applies to the last option in the bundle. When <see cref="EnablePosixBundling"/> is set to <see langword="true"/>, all of the following command lines are equivalent:
        /// <code>
        ///     &gt; myapp -a -b -c arg
        ///     &gt; myapp -abc arg
        ///     &gt; myapp -abcarg
        /// </code>
        ///
        /// </remarks>
        public bool EnablePosixBundling { get; set; } = true;

        /// <summary>
        /// Enables a default exception handler to catch any unhandled exceptions thrown during invocation. Enabled by default.
        /// </summary>
        public bool EnableDefaultExceptionHandler { get; set; } = true;

        /// <summary>
        /// Enables signaling and handling of process termination (Ctrl+C, SIGINT, SIGTERM) via a <see cref="CancellationToken"/> 
        /// that can be passed to a <see cref="CommandLineAction"/> during invocation.
        /// If not provided, a default timeout of 2 seconds is enforced.
        /// </summary>
        public TimeSpan? ProcessTerminationTimeout { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Response file token replacer, enabled by default.
        /// To disable response files support, this property needs to be set to null.
        /// </summary>
        /// <remarks>
        /// When enabled, any token prefixed with <code>@</code> can be replaced with zero or more other tokens. This is mostly commonly used to expand tokens from response files and interpolate them into a command line prior to parsing.
        /// </remarks>
        public TryReplaceToken? ResponseFileTokenReplacer { get; set; } = StringExtensions.TryReadResponseFile;

        /// <summary>
        /// Gets the root command.
        /// </summary>
        public Command RootCommand { get; }

        /// <summary>
        /// The standard output. Used by Help and other facilities that write non-error information.
        /// By default it's set to <see cref="Console.Out"/>.
        /// For testing purposes, it can be set to a new instance of <see cref="StringWriter"/>.
        /// If you want to disable the output, please set it to <see cref="TextWriter.Null"/>.
        /// </summary>
        public TextWriter Output
        { 
            get => _output ??= Console.Out;
            set => _output = value ?? throw new ArgumentNullException(nameof(value), "Use TextWriter.Null to disable the output");
        }

        /// <summary>
        /// The standard error. Used for printing error information like parse errors.
        /// By default it's set to <see cref="Console.Error"/>.
        /// For testing purposes, it can be set to a new instance of <see cref="StringWriter"/>.
        /// </summary>
        public TextWriter Error
        {
            get => _error ??= Console.Error;
            set => _error = value ?? throw new ArgumentNullException(nameof(value), "Use TextWriter.Null to disable the output");
        }

        /// <summary>
        /// Parses an array strings using the configured <see cref="RootCommand"/>.
        /// </summary>
        /// <param name="args">The string arguments to parse.</param>
        /// <returns>A parse result describing the outcome of the parse operation.</returns>
        public ParseResult Parse(IReadOnlyList<string> args)
            => CommandLineParser.Parse(RootCommand, args, this);

        /// <summary>
        /// Parses a command line string value using the configured <see cref="RootCommand"/>.
        /// </summary>
        /// <remarks>The command line string input will be split into tokens as if it had been passed on the command line.</remarks>
        /// <param name="commandLine">A command line string to parse, which can include spaces and quotes equivalent to what can be entered into a terminal.</param>
        /// <returns>A parse result describing the outcome of the parse operation.</returns>
        public ParseResult Parse(string commandLine)
            => CommandLineParser.Parse(RootCommand, commandLine, this);

        /// <summary>
        /// Parses a command line string value and invokes the handler for the indicated command.
        /// </summary>
        /// <returns>The exit code for the invocation.</returns>
        /// <remarks>The command line string input will be split into tokens as if it had been passed on the command line.</remarks>
        public int Invoke(string commandLine)
            => RootCommand.Parse(commandLine, this).Invoke();

        /// <summary>
        /// Parses a command line string array and invokes the handler for the indicated command.
        /// </summary>
        /// <returns>The exit code for the invocation.</returns>
        public int Invoke(string[] args)
            => RootCommand.Parse(args, this).Invoke();

        /// <summary>
        /// Parses a command line string value and invokes the handler for the indicated command.
        /// </summary>
        /// <returns>The exit code for the invocation.</returns>
        /// <remarks>The command line string input will be split into tokens as if it had been passed on the command line.</remarks>
        public Task<int> InvokeAsync(string commandLine, CancellationToken cancellationToken = default)
            => RootCommand.Parse(commandLine, this).InvokeAsync(cancellationToken);

        /// <summary>
        /// Parses a command line string array and invokes the handler for the indicated command.
        /// </summary>
        /// <returns>The exit code for the invocation.</returns>
        public Task<int> InvokeAsync(string[] args, CancellationToken cancellationToken = default)
            => RootCommand.Parse(args, this).InvokeAsync(cancellationToken);

        /// <summary>
        /// Throws an exception if the parser configuration is ambiguous or otherwise not valid.
        /// </summary>
        /// <remarks>Due to the performance cost of this method, it is recommended to be used in unit testing or in scenarios where the parser is configured dynamically at runtime.</remarks>
        /// <exception cref="CommandLineConfigurationException">Thrown if the configuration is found to be invalid.</exception>
        public void ThrowIfInvalid()
        {
            ThrowIfInvalid(RootCommand);

            static void ThrowIfInvalid(Command command)
            {
                if (command.Parents.FlattenBreadthFirst(c => c.Parents).Any(ancestor => ancestor == command))
                {
                    throw new CommandLineConfigurationException($"Cycle detected in command tree. Command '{command.Name}' is its own ancestor.");
                }

                int count = command.Subcommands.Count + command.Options.Count;
                for (var i = 0; i < count; i++)
                {
                    Symbol symbol1 = GetChild(i, command, out AliasSet? aliases1);
                    for (var j = i + 1; j < count; j++)
                    {
                        Symbol symbol2 = GetChild(j, command, out AliasSet? aliases2);

                        if (symbol1.Name.Equals(symbol2.Name, StringComparison.Ordinal)
                            || (aliases1 is not null && aliases1.Contains(symbol2.Name)))
                        {
                            throw new CommandLineConfigurationException($"Duplicate alias '{symbol2.Name}' found on command '{command.Name}'.");
                        }
                        else if (aliases2 is not null && aliases2.Contains(symbol1.Name))
                        {
                            throw new CommandLineConfigurationException($"Duplicate alias '{symbol1.Name}' found on command '{command.Name}'.");
                        }

                        if (aliases1 is not null && aliases2 is not null)
                        {
                            // take advantage of the fact that we are dealing with two hash sets
                            if (aliases1.Overlaps(aliases2))
                            {
                                foreach (string symbol2Alias in aliases2)
                                {
                                    if (aliases1.Contains(symbol2Alias))
                                    {
                                        throw new CommandLineConfigurationException($"Duplicate alias '{symbol2Alias}' found on command '{command.Name}'.");
                                    }
                                }
                            }
                        }
                    }

                    if (symbol1 is Command childCommand)
                    {
                        ThrowIfInvalid(childCommand);
                    }
                }
            }

            static Symbol GetChild(int index, Command command, out AliasSet? aliases)
            {
                if (index < command.Subcommands.Count)
                {
                    aliases = command.Subcommands[index]._aliases;
                    return command.Subcommands[index];
                }

                aliases = command.Options[index - command.Subcommands.Count]._aliases;
                return command.Options[index - command.Subcommands.Count];
            }
        }
    }
}