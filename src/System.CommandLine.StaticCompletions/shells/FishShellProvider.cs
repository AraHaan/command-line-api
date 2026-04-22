// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.StaticCompletions.Resources;

namespace System.CommandLine.StaticCompletions.Shells;

public class FishShellProvider : IShellProvider
{
    public string ArgumentName => ShellNames.Fish;

    public string Extension => "fish";

    public string HelpDescription => Strings.FishShellProvider_HelpDescription;

    // override the ToString method to return the argument name so that CLI help is cleaner for 'default' values
    public override string ToString() => ArgumentName;

    public string GenerateCompletions(System.CommandLine.Command command)
    {
        var binary = command.Name;
        var safeName = binary.Replace('-', '_').Replace('.', '_');
        return
            $$"""
            # fish parameter completion for {{binary}}
            # add the following to your config.fish to enable completions

            function __{{safeName}}_complete
                set -l pos (commandline -C)
                set -l line (commandline -cp)
                {{binary}} "[suggest:$pos]" $line 2>/dev/null
            end
            complete -f -c {{binary}} -a '(__{{safeName}}_complete)'
            """;
    }
}
