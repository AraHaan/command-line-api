﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace System.CommandLine.Tests
{
    public class DirectiveTests
    {
        [Fact]
        public void Directives_should_be_considered_as_unmatched_tokens_when_they_are_not_matched()
        {
            Directive directive = new("parse");

            ParseResult result = Parse(new Option<bool>("-y"), directive, $"{RootCommand.ExecutableName} [nonExisting] -y");

            result.UnmatchedTokens.Should().ContainSingle("[nonExisting]");
        }

        [Fact]
        public void Tokens_still_hold_directives()
        {
            Directive directive = new ("parse");

            ParseResult result = Parse(new Option<bool>("-y"), directive, "[parse] -y");

            result.GetResult(directive).Should().NotBeNull();
            result.Tokens.Should().Contain(t => t.Value == "[parse]" && t.Type == TokenType.Directive);
        }

        [Fact]
        public void Directives_must_precede_other_symbols()
        {
            Directive directive = new("parse");

            ParseResult result = Parse(new Option<bool>("-y"), directive, "-y [parse]");

            result.GetResult(directive).Should().BeNull();
        }

        [Fact]
        public void Multiple_directives_are_allowed()
        {
            RootCommand root = new() { new Option<bool>("-y") };
            Directive parseDirective = new ("parse");
            Directive suggestDirective = new ("suggest");

            root.Add(parseDirective);
            root.Add(suggestDirective);

            var result = root.Parse("[parse] [suggest] -y");

            result.GetResult(parseDirective).Should().NotBeNull();
            result.GetResult(suggestDirective).Should().NotBeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Multiple_instances_of_the_same_directive_can_be_invoked(bool invokeAsync)
        {
            var commandActionWasCalled = false;
            var directiveCallCount = 0;

            Action<ParseResult> incrementCallCount = _ => directiveCallCount++;
            Action<ParseResult> verifyActionWasCalled = _ => commandActionWasCalled = true;

            var testDirective = new TestDirective("test")
            {
                Action = invokeAsync
                             ? new AsynchronousTestAction(incrementCallCount, terminating: false)
                             : new SynchronousTestAction(incrementCallCount, terminating: false)
            };

            var rootCommand = new RootCommand
            {
                Action = invokeAsync
                             ? new AsynchronousTestAction(verifyActionWasCalled, terminating: false)
                             : new SynchronousTestAction(verifyActionWasCalled, terminating: false),
                Directives = { testDirective }
            };

            if (invokeAsync)
            {
                await rootCommand.Parse("[test:1] [test:2]").InvokeAsync();
            }
            else
            {
                rootCommand.Parse("[test:1] [test:2]").Invoke();
            }

            using var _ = new AssertionScope();

            commandActionWasCalled.Should().BeTrue();
            directiveCallCount.Should().Be(2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Multiple_different_directives_can_be_invoked(bool invokeAsync)
        {
            bool commandActionWasCalled = false;
            bool directiveOneActionWasCalled = false;
            bool directiveTwoActionWasCalled = false;

            var directiveOne = new TestDirective("one")
            {
                Action = new SynchronousTestAction(_ => directiveOneActionWasCalled = true, terminating: false)
            };
            var directiveTwo = new TestDirective("two")
            {
                Action = new SynchronousTestAction(_ => directiveTwoActionWasCalled = true, terminating: false)
            };

            var rootCommand = new RootCommand
            {
                Action = new SynchronousTestAction(_ => commandActionWasCalled = true, terminating: false), Directives = { directiveOne, directiveTwo }
            };

            if (invokeAsync)
            {
                await rootCommand.Parse("[one] [two]").InvokeAsync();
            }
            else
            {
                rootCommand.Parse("[one] [two]").Invoke();
            }

            using var _ = new AssertionScope();

            commandActionWasCalled.Should().BeTrue();
            directiveOneActionWasCalled.Should().BeTrue();
            directiveTwoActionWasCalled.Should().BeTrue();
        }

        public class TestDirective : Directive
        {
            public TestDirective(string name) : base(name)
            {
            }
        }

        [Theory]
        [InlineData("[key:value]", "key", "value")]
        [InlineData("[key:value:more]", "key", "value:more")]
        [InlineData("[key:]", "key", "")]
        public void Directives_can_have_a_value_which_is_everything_after_the_first_colon(
            string wholeText,
            string key,
            string expectedValue)
        {
            Directive directive = new(key);

            ParseResult result = Parse(new Option<bool>("-y"), directive, $"{wholeText} -y");

            result.GetResult(directive).Values.Single().Should().Be(expectedValue);
        }

        [Fact]
        public void Directives_without_a_value_specified_have_no_values()
        {
            Directive directive = new("parse");

            ParseResult result = Parse(new Option<bool>("-y"), directive, "[parse] -y");

            result.GetResult(directive).Values.Should().BeEmpty();
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("[:value]")]
        public void Directives_must_have_a_non_empty_key(string directive)
        {
            Option<bool> option = new ("-a");
            RootCommand root = new () { option };

            var result = root.Parse($"{directive} -a");

            result.UnmatchedTokens.Should().Contain(directive);
        }

        [Theory]
        [InlineData("[par se]", "[par", "se]")]
        [InlineData("[ parse]", "[", "parse]")]
        [InlineData("[parse ]", "[parse", "]")]
        public void Directives_cannot_contain_spaces(string value, string firstUnmatchedToken, string secondUnmatchedToken)
        {
            Action create = () => new Directive(value);
            create.Should().Throw<ArgumentException>();

            Directive directive = new("parse");
            ParseResult result = Parse(new Option<bool>("-y"), directive, $"{value} -y");
            result.GetResult(directive).Should().BeNull();

            result.UnmatchedTokens.Should().BeEquivalentTo(firstUnmatchedToken, secondUnmatchedToken);
        }

        [Fact]
        public void When_a_directive_is_specified_more_than_once_then_its_values_are_aggregated()
        {
            Directive directive = new("directive");

            ParseResult result = Parse(new Option<bool>("-a"), directive, "[directive:one] [directive:two] -a");

            result.GetResult(directive).Values.Should().BeEquivalentTo("one", "two");
        }

        private static ParseResult Parse(Option option, Directive directive, string commandLine)
        {
            RootCommand root = new() { option };

            root.Directives.Add(directive);

            return root.Parse(commandLine);
        }
    }
}