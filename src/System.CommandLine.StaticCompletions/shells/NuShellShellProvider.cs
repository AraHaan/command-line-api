// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.StaticCompletions.Resources;

namespace System.CommandLine.StaticCompletions.Shells;

public class NushellShellProvider : IShellProvider
{
    public string ArgumentName => ShellNames.Nushell;

    public string Extension => "nu";

    public string HelpDescription => Strings.NuShellShellProvider_HelpDescription;

    /// <summary>
    /// Controls how the generated script invokes the application to resolve dynamic completions.
    /// Defaults to the built-in <c>[suggest]</c> directive.
    /// </summary>
    public CompletionInvocation Invocation { get; init; } = CompletionInvocation.Directive();

    // override the ToString method to return the argument name so that CLI help is cleaner for 'default' values
    public override string ToString() => ArgumentName;

    public string GenerateCompletions(System.CommandLine.Command command)
    {
        var binary = command.Name;
        var callLine = Invocation switch
        {
            CompletionInvocation.DirectiveInvocation d =>
                $$"""{{binary}} $"[{{d.Name}}:($line | str length)]" $line | lines""",
            CompletionInvocation.SubcommandInvocation s =>
                $$"""{{binary}} {{s.Name}} --position ($line | str length) $line | lines""",
            _ => throw new NotSupportedException($"Unknown invocation kind: {Invocation.GetType().Name}")
        };
        return
            $$"""
            # Add the following content to your config.nu file:

            let external_completer = { |spans|
                {
                    "{{binary}}": { ||
                        let line = ($spans | skip 1 | str join " ")
                        {{callLine}}
                    }
                } | get $spans.0 | each { || do $in }
            }

            # And then in the config record, find the completions section and add the
            # external_completer that was defined earlier to external:

            $env.config = {
                # your options here
                completions: {
                    # your options here
                    external: {
                        # your options here
                        completer: $external_completer # add it here
                    }
                }
            }
            """;
    }
}
