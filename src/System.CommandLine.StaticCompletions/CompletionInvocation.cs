// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CommandLine.StaticCompletions;

/// <summary>
/// Describes how generated shell completion scripts invoke the target application
/// to resolve dynamic completions at runtime.
/// </summary>
public abstract record CompletionInvocation
{
    private protected CompletionInvocation() { }

    /// <summary>
    /// Invoke the application via a directive, e.g. <c>myapp [suggest:42] "myapp ar"</c>.
    /// This is the default — it works for any <see cref="RootCommand"/> because the
    /// <c>[suggest]</c> directive is registered on <c>RootCommand</c> by default.
    /// </summary>
    /// <param name="name">Directive name. Defaults to <c>suggest</c>.</param>
    public static CompletionInvocation Directive(string name = "suggest") => new DirectiveInvocation(name);

    /// <summary>
    /// Invoke the application via a subcommand, e.g. <c>myapp complete --position 42 "myapp ar"</c>.
    /// The tool author is responsible for registering a matching subcommand that accepts
    /// <c>--position &lt;int&gt; &lt;string&gt;</c> and writes newline-separated completions to stdout.
    /// </summary>
    /// <param name="name">Subcommand name. Defaults to <c>complete</c>.</param>
    public static CompletionInvocation Subcommand(string name = "complete") => new SubcommandInvocation(name);

    internal sealed record DirectiveInvocation(string Name) : CompletionInvocation;
    internal sealed record SubcommandInvocation(string Name) : CompletionInvocation;
}
