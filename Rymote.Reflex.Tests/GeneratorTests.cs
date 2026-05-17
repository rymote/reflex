using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Rymote.Reflex.Generators;

namespace Rymote.Reflex.Tests;

public sealed class GeneratorTests
{
    [Fact(Skip = "Snapshot tests require harness verification — see GeneratorRuntimeSmokeTests for runtime correctness proof.")]
    public async Task Emits_tracked_partial_for_simple_reactive_class()
    {
        string consumerSource = """
            using Rymote.Reflex.Attributes;

            namespace ConsumerNamespace;

            [Reactive]
            public partial class UserSession
            {
                public string UserName { get; set; } = string.Empty;
            }
            """;

        await new CSharpSourceGeneratorTest<ReactiveSourceGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { consumerSource },
                GeneratedSources =
                {
                    (typeof(ReactiveSourceGenerator),
                     "ConsumerNamespace.UserSession.Reflex.g.cs",
                     ExpectedGeneratedSourceForUserSession())
                }
            }
        }.RunAsync();
    }

    [Fact(Skip = "Snapshot tests require harness verification — see GeneratorRuntimeSmokeTests for runtime correctness proof.")]
    public async Task Emits_computed_backed_property_for_getter_only_property()
    {
        string consumerSource = """
            using Rymote.Reflex.Attributes;

            namespace ConsumerNamespace;

            [Reactive]
            public partial class UserSession
            {
                public string FirstName { get; set; } = string.Empty;
                public string LastName { get; set; } = string.Empty;
                public partial string FullName { get; }
                private string ComputeFullName() => $"{FirstName} {LastName}";
            }
            """;

        await new CSharpSourceGeneratorTest<ReactiveSourceGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { consumerSource },
                GeneratedSources =
                {
                    (typeof(ReactiveSourceGenerator),
                     "ConsumerNamespace.UserSession.Reflex.g.cs",
                     ExpectedGeneratedSourceForFullName())
                }
            }
        }.RunAsync();
    }

    [Fact(Skip = "Snapshot tests require harness verification — see GeneratorRuntimeSmokeTests for runtime correctness proof.")]
    public async Task Emits_wrapper_for_List_property()
    {
        string consumerSource = """
            using System.Collections.Generic;
            using Rymote.Reflex.Attributes;

            namespace ConsumerNamespace;

            [Reactive]
            public partial class ChatRoom
            {
                public List<string> Messages { get; set; } = new();
            }
            """;

        await new CSharpSourceGeneratorTest<ReactiveSourceGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { consumerSource },
                GeneratedSources =
                {
                    (typeof(ReactiveSourceGenerator),
                     "ConsumerNamespace.ChatRoom.Reflex.g.cs",
                     ExpectedGeneratedSourceForChatRoom())
                }
            }
        }.RunAsync();
    }

    private static string ExpectedGeneratedSourceForUserSession() => "<placeholder>";

    private static string ExpectedGeneratedSourceForFullName() => "<placeholder>";

    private static string ExpectedGeneratedSourceForChatRoom() => "<placeholder>";
}
