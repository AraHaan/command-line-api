// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.StaticCompletions.Resources;

namespace System.CommandLine.StaticCompletions.Shells;

public class NushellShellProvider : IShellProvider
{
    public string ArgumentName => ShellNames.Nushell;

    public string Extension => "nu";

    public string HelpDescription => Strings.NuShellShellProvider_HelpDescription;

    // override the ToString method to return the argument name so that CLI help is cleaner for 'default' values
    public override string ToString() => ArgumentName;

    public string GenerateCompletions(System.CommandLine.Command command)
    {
        var binary = command.Name;
        return
            $$"""
            # Add the following content to your config.nu file:

            let external_completer = { |spans|
                {
                    "{{binary}}": { ||
                        let line = ($spans | skip 1 | str join " ")
                        {{binary}} $"[suggest:($line | str length)]" $line | lines
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
