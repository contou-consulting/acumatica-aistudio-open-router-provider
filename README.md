# OpenRouter provider for Acumatica AI Studio (26R2)

An Acumatica customization package that adds **OpenRouter (Direct)** to the provider list on
*Agent LLM Connections* (ML201000). AI Automation agents can then run on any model
[OpenRouter](https://openrouter.ai) serves (OpenAI, Anthropic, Google, Meta, Mistral, DeepSeek and others),
billed to your own OpenRouter key, without going through Acumatica's AI Studio proxy.

Built against Acumatica **2026 R2, build 26.200.0334**.

## Why

Out of the box, 26R2 agents talk to LLMs through Acumatica's cloud proxy, which offers a fixed
list of providers. The platform also has a second, hidden path: **direct providers** that build
the chat client inside the Acumatica site. The only one shipped is *Azure (Direct)*. This package
registers another direct provider pointed at OpenRouter's OpenAI-compatible API, which gives you:

- one key and one bill for hundreds of models
- the ability to switch models per connection without a code change
- agents on sites that have no AI Studio proxy subscription configured

## Requirements

- Acumatica 26R2 (26.200.x) with **AI Studio → AI Automation** enabled on *Enable/Disable Features* (CS100000)
- An OpenRouter API key (<https://openrouter.ai/settings/keys>) with credit on the account
- Access to the site's `web.config` (see step 2)

## Install

### 1. Publish the package

1. Download `LS.AIStudio.OpenRouterProvider.zip` from the releases page (or build it, below).
2. *Customization Projects* (SM204505) → **Import** the zip → select it → **Publish**.

The package contains a single DLL. It adds no screens, tables, or DAC changes.

### 2. Turn on direct providers in web.config

Direct providers are hidden unless the `ai-studio` options section enables them. Add an
`ai-studio` element inside the existing `<px.core>` section of the site's `web.config`:

```xml
<px.core>
  <!-- ...existing elements... -->
  <ai-studio EnableDirectProviders="true" />
</px.core>
```

Saving `web.config` restarts the site. This switch also shows the stock *Azure (Direct)* provider.
A customization package cannot edit `web.config`, so this step is manual on every site.
On hosted sites, ask whoever manages the site's `web.config`.

### 3. Create the connection

1. *Agent LLM Connections* (ML201000) → **Refresh** the provider list. *OpenRouter (Direct)* appears.
2. Add a connection, pick **OpenRouter (Direct)**, and fill in:

   | Parameter | Value |
   |---|---|
   | API Key | your OpenRouter key (stored encrypted) |
   | Model | an OpenRouter model id, for example `openai/gpt-4o-mini` |

3. **Test Connection**. When the status is *Active*, the connection shows up in the *Connection*
   lookup on *Agents* (ML202000).

Create one connection per model you want to offer, for example a small, cheap model for
classification agents and a larger one for summaries.

## Choosing a model

AI Automation agents depend on two model capabilities:

- **Tool calling**: agents read and write the record through `get_current`, `patch_current`, and GI tools.
- **Structured outputs**: every run asks for a JSON-schema response (`IsSuccess`, `Message`).

On [openrouter.ai/models](https://openrouter.ai/models), filter by *Tools* and *Structured Outputs*.
A model without them can fail outright or finish runs without ever calling `patch_current`.
Use the agent's **Test Agent** and **Processing Log** tabs to compare models on real records
before you activate an agent.

## What stays the same

The provider swaps only the transport at the end of the pipeline. Everything above it is stock:

- system and business instructions, tool definitions, and the iteration and error limits
- **masked fields**: values are swapped for `${Field}` placeholders before anything leaves the site
- *AI Automation History* (ML400000) token counts, plus the Test Agent result diff and processing log
- triggers: More-menu button, business events, import scenarios, workflow

Two differences from proxy connections:

- Acumatica's 800-token default output cap is a proxy setting, so it does not apply here.
  OpenRouter uses the model's own default.
- Data goes from your site to OpenRouter and on to the model's upstream provider. Check
  OpenRouter's data and logging policies, and the per-provider routing settings on your OpenRouter
  account, against your own data-handling requirements.

## How it works

Everything that follows was read from decompiled 26R2 assemblies (`PX.AIStudio.dll`).

| Piece | Stock behavior | This package |
|---|---|---|
| Provider list | `ConnectionMetadataProvider` merges proxy providers with every Autofac `ConnectionMetadata` keyed `"direct"`, but only when `ai-studio:EnableDirectProviders` is true | `LSAIOpenRouterModule` registers OpenRouter's metadata under the `"direct"` key |
| Client creation | `ChatClientFactoryResolver` resolves `ConnectionMetadata.ChatClientFactoryType` from the container and calls `IChatClientFactory.CreateClient(ConnectionSettings)` | `LSAIOpenRouterChatClientFactory` builds the site's own `OpenAIClient` with its endpoint set to `https://openrouter.ai/api/v1`, then wraps it as a `Microsoft.Extensions.AI` `IChatClient` |
| Pipeline | Function invocation, logging, token tracking, and masking wrap whatever `IChatClient` the factory returns | unchanged |

The code is two small classes in [`src/LS.AIStudio.OpenRouterProvider`](src/LS.AIStudio.OpenRouterProvider).
Because OpenRouter speaks the OpenAI wire format, the same pattern works for any other
OpenAI-compatible endpoint (a self-hosted vLLM, LM Studio, Groq, Together). Copy the factory and
change the endpoint and provider id.

## Caveats

- `IChatClientFactory`, `ConnectionMetadata`, and `ConnectionSettings` are marked `[PXInternalUseOnly]`.
  Acumatica can change them in any build without notice, and the
  package is not a candidate for ISV certification. Re-test after every Acumatica upgrade.
- Pin the assemblies you compile against to the site you deploy to. The project references
  `OpenAI.dll`, `Microsoft.Extensions.AI*.dll`, and `System.ClientModel.dll` from the site's `Bin`
  and does not ship them.
- This is a community sample, not an Acumatica product. Use it at your own risk (see [LICENSE](LICENSE)).

## Build from source

You need the .NET SDK and a local 26R2 site. All Acumatica assemblies, PX.* included, come from
that site's `Bin`, so no private NuGet feed is needed.

```powershell
dotnet build src/LS.AIStudio.OpenRouterProvider/LS.AIStudio.OpenRouterProvider.csproj -c Release `
  -p:AcumaticaSiteDir=C:\AcumaticaSites\MySite\
```

To avoid passing the path each time, create a git-ignored `src/acumatica.user.props`:

```xml
<Project>
  <PropertyGroup>
    <AcumaticaSiteDir>C:\AcumaticaSites\MySite\</AcumaticaSiteDir>
  </PropertyGroup>
</Project>
```

| Configuration | Result |
|---|---|
| `Debug` | copies the DLL into the site's `Bin` (restarts the app domain) |
| `Release` | stages the DLL in `cust/LS.AIStudio.OpenRouterProvider/Bin` and writes `LS.AIStudio.OpenRouterProvider.zip` to the repo root with `PX.CommandLine.exe` |

## Repository layout

```
src/
  acumatica.targets                         site path, Debug copy, Release packaging
  LS.AIStudio.OpenRouterProvider/
    Autofac/LSAIOpenRouterModule.cs         registers the provider metadata and factory
    ChatClients/LSAIOpenRouterChatClientFactory.cs
cust/LS.AIStudio.OpenRouterProvider/_project/  customization project definition
```

## License

MIT © 2026 Contou Consulting
