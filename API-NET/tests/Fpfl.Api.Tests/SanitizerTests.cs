using Fpfl.Common.Exceptions;
using Fpfl.Common.Sanitization;

using Microsoft.Extensions.Logging.Abstractions;

namespace Fpfl.Api.Tests;

public sealed class SanitizerTests
{
    private readonly Sanitizer _sanitizer = new(NullLogger<Sanitizer>.Instance);

    private sealed record Sample(string Name, [property: LenientText] string Notes);

    private sealed record Skipped(string Raw) : INoSanitize;

    private sealed class Parent
    {
        public List<Sample> Children { get; set; } = [];
        public Parent? Self { get; set; }
    }

    [Fact]
    public void Strict_text_passes_unchanged()
    {
        Assert.Equal("Initial Amount", _sanitizer.SanitizeString("Initial Amount"));
    }

    [Fact]
    public void Normalizes_whitespace_and_invisible_characters()
    {
        Assert.Equal("Monthly Rent", _sanitizer.SanitizeString("﻿  Monthly​   Rent \t"));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("Rent; DROP TABLE items")]
    public void Strict_text_rejects_disallowed_characters(string input)
    {
        Assert.Throws<SanitizationException>(() => _sanitizer.SanitizeString(input));
    }

    [Fact]
    public void Records_are_sanitized_in_place_and_lenient_fields_allow_punctuation()
    {
        var sample = new Sample("  Car   Payment ", "Paid on the 1st & 15th!");

        _sanitizer.Sanitize(sample);

        Assert.Equal("Car Payment", sample.Name);
        Assert.Equal("Paid on the 1st & 15th!", sample.Notes);
    }

    [Fact]
    public void Nested_lists_are_walked_and_cycles_do_not_loop()
    {
        var parent = new Parent { Children = [new Sample("  Salary ", "ok")] };
        parent.Self = parent;

        _sanitizer.Sanitize(parent);

        Assert.Equal("Salary", parent.Children[0].Name);
    }

    [Fact]
    public void Uri_strings_use_the_uri_rules()
    {
        var sample = new Sample("/item-types/create/1", "x");

        _sanitizer.Sanitize(sample);

        Assert.Equal("/item-types/create/1", sample.Name);
    }

    [Fact]
    public void NoSanitize_types_are_skipped()
    {
        var skipped = new Skipped("<b>raw</b>");

        _sanitizer.Sanitize(skipped);

        Assert.Equal("<b>raw</b>", skipped.Raw);
    }
}
